using System.Diagnostics;
using FileOrganizerNET.Contracts;
using FileOrganizerNET.Models.Config;
using FileOrganizerNET.Models.Result;
using FileOrganizerNET.Utils;

namespace FileOrganizerNET.Concrete;

public class FileOrganizer(IFileLogger logger, FileSystemActions fileSystemActions) : IFileOrganizer
{
    private const string ConfigFileName = "config.json";

    public OrganizationResult Organize(string targetPath, OrganizerConfig config,
        bool isRecursive,
        bool isDryRun,
        bool enableDuplicateCheck)
    {
        var stopwatch = Stopwatch.StartNew();
        var overallSuccess = true;

        var targetDir = new DirectoryInfo(targetPath);
        if (!targetDir.Exists)
        {
            stopwatch.Stop();
            logger.Log($"ERROR: Target directory not found: {targetPath}");
            return new OrganizationResult
            {
                Success = false,
                Message = $"ERROR: Target directory not found: {targetPath}",
                ElapsedTime = stopwatch.Elapsed
            };
        }

        if (isDryRun)
            logger.Log("--- DRY RUN MODE ENABLED: No files or folders will be moved, copied, or deleted. ---");

        var managedFolders = GetManagedFolderNames(config);

        logger.Log("\n--- Processing Files ---");
        var fileProcessingResult = ProcessFiles(targetDir, config, managedFolders, isRecursive, isDryRun);
        if (fileProcessingResult.Errors.Count != 0) overallSuccess = false; // Set to false if errors exist

        logger.Log("\n--- Processing Folders ---");
        var folderProcessingResult = ProcessFolders(targetDir, config, managedFolders, isDryRun);
        if (folderProcessingResult.Errors.Count != 0) overallSuccess = false; // Set to false if errors exist


        DuplicateCheckResult? duplicateCheckResult = null; // Initialize as nullable
        if (enableDuplicateCheck)
        {
            logger.Log("\n--- Checking for Duplicate Files ---");
            duplicateCheckResult = FindAndRemoveDuplicates(targetDir, managedFolders, isDryRun);
            if (duplicateCheckResult.Errors.Count != 0) overallSuccess = false; // Set to false if errors exist
        }

        stopwatch.Stop();
        var elapsedTime = stopwatch.Elapsed;
        var message = overallSuccess ? "Organization complete." : "Organization completed with errors.";

        return new OrganizationResult
        {
            Success = overallSuccess,
            Message = message,
            ElapsedTime = elapsedTime,
            FileProcessingOutcome = fileProcessingResult,
            FolderProcessingOutcome = folderProcessingResult,
            DuplicateCheckOutcome = duplicateCheckResult
        };
    }

    /// <summary>
    ///     Iterates through files in the target directory and applies the first matching rule.
    /// </summary>
    private FileProcessingResult ProcessFiles(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders, bool isRecursive, bool isDryRun)
    {
        var filesScanned = 0;
        var actions = new List<ProcessedFileAction>();
        var errors = new List<string>();

        var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in targetDir.GetFiles("*", searchOption))
        {
            if (file.Name.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase)) continue;

            if (isRecursive)
            {
                var parentFolderName = file.Directory?.Name ?? string.Empty;
                if (managedFolders.Contains(parentFolderName)) continue;
            }

            filesScanned++;

            var matchedRule = config.Rules.FirstOrDefault(rule => RuleMatcher.DoesFileMatchRule(file, rule.Conditions));

            ProcessedFileAction fileAction;

            if (isDryRun)
            {
                var actionVerb = (matchedRule?.Action ?? RuleAction.Move).ToString().ToUpper();
                string? destinationPath = null;
                if (matchedRule?.Action != RuleAction.Delete)
                    destinationPath = Path.Combine(targetDir.FullName, matchedRule?.DestinationFolder ?? config.OthersFolderName);
                var destinationInfo = destinationPath is null ? "" : $" -> \"{destinationPath}\"";
                var message = $"[DRY RUN] Would {actionVerb} file: \"{file.FullName}\"{destinationInfo}";

                logger.Log(message);
                fileAction = new ProcessedFileAction
                {
                    OriginalFilePath = file.FullName,
                    Action = matchedRule?.Action ?? RuleAction.Move,
                    DestinationPath = destinationPath,
                    IsSuccess = true, // Dry run simulations are always "successful"
                    ResultMessage = message
                };
            }
            else
            {
                if (matchedRule != null)
                    switch (matchedRule.Action)
                    {
                        case RuleAction.Move:
                            fileAction = fileSystemActions.MoveFile(file, Path.Combine(targetDir.FullName, matchedRule.DestinationFolder));
                            break;
                        case RuleAction.Copy:
                            fileAction = fileSystemActions.CopyFile(file, Path.Combine(targetDir.FullName, matchedRule.DestinationFolder));
                            break;
                        case RuleAction.Delete:
                            fileAction = fileSystemActions.DeleteFile(file);
                            break;
                        default:
                            var errorMsg = $"Unsupported RuleAction: {matchedRule.Action}";
                            logger.Log($"ERROR: {errorMsg}");
                            fileAction = new ProcessedFileAction
                                { OriginalFilePath = file.FullName, IsSuccess = false, ResultMessage = errorMsg, Action = matchedRule.Action };
                            break;
                    }
                else
                    fileAction = fileSystemActions.MoveFile(file, Path.Combine(targetDir.FullName, config.OthersFolderName));
            }

            actions.Add(fileAction);
            if (!fileAction.IsSuccess) errors.Add(fileAction.ResultMessage);
        }

        return new FileProcessingResult
        {
            FilesScanned = filesScanned,
            ActionsTaken = actions,
            Errors = errors
        };
    }

    /// <summary>
    ///     Processes top-level folders in the target directory, moving non-managed ones to the 'Folders'
    ///     directory.
    /// </summary>
    private FolderProcessingResult ProcessFolders(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders, bool isDryRun)
    {
        var foldersScanned = 0;
        var foldersMoved = 0;
        var errors = new List<string>();

        var subfolderDestination = Path.Combine(targetDir.FullName, config.SubfoldersFolderName);

        foreach (var dir in targetDir.GetDirectories())
        {
            foldersScanned++;

            if (managedFolders.Contains(dir.Name)) continue;

            if (isDryRun)
            {
                logger.Log($"[DRY RUN] Would move folder: \"{dir.FullName}\" -> \"{subfolderDestination}\"");
                continue;
            }

            try
            {
                Directory.CreateDirectory(subfolderDestination);
                dir.MoveTo(Path.Combine(subfolderDestination, dir.Name));
                logger.Log($"Moving folder: \"{dir.Name}\" -> \"{config.SubfoldersFolderName}\"");
                foldersMoved++;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Could not move \"{dir.Name}\". Reason: {ex.Message}";
                logger.Log($"WARNING: {errorMsg}");
                errors.Add(errorMsg);
            }
        }

        return new FolderProcessingResult
        {
            FoldersScanned = foldersScanned,
            FoldersMoved = foldersMoved,
            Errors = errors
        };
    }

    /// <summary>
    ///     Scans managed destination folders for duplicate files using XXHash and deletes extra copies.
    /// </summary>
    private DuplicateCheckResult FindAndRemoveDuplicates(DirectoryInfo targetDir, HashSet<string> managedFolders, bool isDryRun)
    {
        var filesHashed = 0;
        var errors = new List<string>();
        var duplicateSetsFound = 0;
        var duplicateFilesDeleted = 0;

        var fileHashes = new Dictionary<string, List<string>>();

        foreach (var folderName in managedFolders)
        {
            var folderPath = Path.Combine(targetDir.FullName, folderName);
            if (!Directory.Exists(folderPath)) continue;

            foreach (var file in new DirectoryInfo(folderPath).GetFiles("*", SearchOption.AllDirectories))
            {
                filesHashed++;
                if (file.Length == 0) continue;

                var hashHex = fileSystemActions.GetXxHash128(file.FullName);
                if (hashHex is null)
                {
                    errors.Add($"Failed to hash file: {file.FullName}");
                    continue;
                }

                if (!fileHashes.TryGetValue(hashHex, out var value))
                {
                    value = [];
                    fileHashes[hashHex] = value;
                }

                value.Add(file.FullName);
            }
        }

        foreach (var entry in fileHashes)
        {
            if (entry.Value.Count <= 1) continue;

            duplicateSetsFound++;
            var originalFile = entry.Value[0];
            logger.Log($"Found duplicate set for hash: {entry.Key} (Original: \"{Path.GetFileName(originalFile)}\")");

            for (var i = 1; i < entry.Value.Count; i++)
            {
                var duplicateFile = entry.Value[i];
                if (isDryRun)
                    logger.Log($"[DRY RUN] Would DELETE duplicate: \"{duplicateFile}\"");
                else
                    try
                    {
                        File.Delete(duplicateFile);
                        logger.Log($"Deleted duplicate: \"{duplicateFile}\"");
                        duplicateFilesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Could not delete duplicate \"{duplicateFile}\". Reason: {ex.Message}";
                        logger.Log($"WARNING: {errorMsg}");
                        errors.Add(errorMsg);
                    }
            }
        }

        return new DuplicateCheckResult
        {
            FilesHashed = filesHashed,
            DuplicateSetsFound = duplicateSetsFound,
            DuplicateFilesDeleted = duplicateFilesDeleted,
            Errors = errors
        };
    }

    /// <summary>
    ///     Gathers all unique destination folder names (including Others and Folders)
    ///     from the config into a HashSet for efficient lookups.
    ///     Handles nested folder paths by extracting only the top-level folder name.
    /// </summary>
    private HashSet<string> GetManagedFolderNames(OrganizerConfig config)
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            config.OthersFolderName,
            config.SubfoldersFolderName
        };
        foreach (var rule in config.Rules)
            if (!string.IsNullOrWhiteSpace(rule.DestinationFolder))
                folders.Add(rule.DestinationFolder.Split(Path.DirectorySeparatorChar)[0]);

        return folders;
    }
}