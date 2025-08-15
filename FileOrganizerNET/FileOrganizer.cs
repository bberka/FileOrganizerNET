namespace FileOrganizerNET;

public class FileOrganizer(IFileLogger logger)
{
    public void Organize(string targetPath, OrganizerConfig config, bool isRecursive, bool isDryRun)
    {
        var targetDir = new DirectoryInfo(targetPath);
        if (!targetDir.Exists)
        {
            logger.Log($"ERROR: Target directory not found: {targetPath}");
            return;
        }

        if (isDryRun)
            logger.Log("--- DRY RUN MODE ENABLED: No files or folders will be moved. ---");

        var managedFolders = GetManagedFolderNames(config);

        logger.Log("\n--- Processing Files ---");
        ProcessFiles(targetDir, config, managedFolders, isRecursive, isDryRun);

        logger.Log("\n--- Processing Folders ---");
        ProcessFolders(targetDir, config, managedFolders, isDryRun);
    }

    /// <summary>
    ///     Iterates through files in the target directory and applies the first matching rule.
    /// </summary>
    /// <param name="targetDir">The root directory to process.</param>
    /// <param name="config">The loaded organizer configuration.</param>
    /// <param name="managedFolders">A set of all possible destination folder names for quick lookups.</param>
    /// <param name="isRecursive">If true, processes files in all subdirectories.</param>
    /// <param name="isDryRun">If true, logs intended actions without modifying the filesystem.</param>
    private void ProcessFiles(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders, bool isRecursive, bool isDryRun)
    {
        var searchOption =
            isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in targetDir.GetFiles("*", searchOption))
        {
            // First, skip any files that should not be processed.
            // 1. Do not process the configuration file itself.
            if (file.Name.Equals(ConfigLoader.DefaultConfigName,
                    StringComparison.OrdinalIgnoreCase)) continue;

            // 2. In recursive mode, do not re-process files already in a managed folder.
            // This prevents the tool from infinitely processing its own output on subsequent runs.
            if (isRecursive)
            {
                var parentFolderName = file.Directory?.Name ?? string.Empty;
                if (managedFolders.Contains(parentFolderName)) continue;
            }

            // Find the first rule in the configuration that the file matches.
            var matchedRule =
                config.Rules.FirstOrDefault(rule => DoesFileMatchRule(file, rule.Conditions));

            // If this is a dry run, log the intended action and move to the next file.
            if (isDryRun)
            {
                if (matchedRule != null)
                {
                    var actionVerb = matchedRule.Action.ToString().ToUpper();
                    var destinationInfo = matchedRule.Action == RuleAction.Delete
                        ? ""
                        : $" -> \"{Path.Combine(targetDir.FullName, matchedRule.DestinationFolder)}\"";

                    logger.Log(
                        $"[DRY RUN] Would {actionVerb} file: \"{file.FullName}\"{destinationInfo}");
                }
                else
                {
                    // Log the default action for unmatched files in a dry run.
                    logger.Log(
                        $"[DRY RUN] Would MOVE file: \"{file.FullName}\" -> \"{Path.Combine(targetDir.FullName, config.OthersFolderName)}\"");
                }

                continue;
            }

            // If not a dry run, execute the appropriate action.
            try
            {
                if (matchedRule != null)
                    // A rule was matched, so perform the specified action.
                    switch (matchedRule.Action)
                    {
                        case RuleAction.Move:
                            MoveFile(file,
                                Path.Combine(targetDir.FullName, matchedRule.DestinationFolder));
                            break;
                        case RuleAction.Copy:
                            CopyFile(file,
                                Path.Combine(targetDir.FullName, matchedRule.DestinationFolder));
                            break;
                        case RuleAction.Delete:
                            DeleteFile(file);
                            break;
                    }
                else
                    // No rule was matched, so perform the default action: move to "Others".
                    MoveFile(file, Path.Combine(targetDir.FullName, config.OthersFolderName));
            }
            catch (Exception ex)
            {
                // Catch potential IO errors (e.g., file in use, permissions denied).
                var action = matchedRule?.Action.ToString() ?? "Move";
                logger.Log(
                    $"WARNING: Could not perform action '{action}' on \"{file.Name}\". Reason: {ex.Message}");
            }
        }
    }

    private static bool DoesFileMatchRule(FileInfo file, RuleConditions conditions)
    {
        if (conditions.Extensions?.Count > 0)
            if (!conditions.Extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                return false;

        if (conditions.FileNameContains?.Count > 0)
            if (!conditions.FileNameContains.Any(keyword =>
                    file.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return false;

        if (conditions.OlderThanDays.HasValue)
            if (file.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-conditions.OlderThanDays.Value))
                return false;

        if (conditions.MinSizeMb.HasValue)
        {
            var minSizeBytes = conditions.MinSizeMb.Value * 1024 * 1024;
            if (file.Length < minSizeBytes)
                return false;
        }

        return true;
    }

    private void ProcessFolders(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders, bool isDryRun)
    {
        // Folder processing is intentionally NOT recursive to avoid complex scenarios.
        // It cleans up the root of the target directory.
        var subfolderDestination = Path.Combine(targetDir.FullName, config.SubfoldersFolderName);

        foreach (var dir in targetDir.GetDirectories())
        {
            if (managedFolders.Contains(dir.Name)) continue;

            var destPath = Path.Combine(subfolderDestination, dir.Name);

            if (isDryRun)
            {
                logger.Log(
                    $"[DRY RUN] Would move folder: \"{dir.FullName}\" -> \"{subfolderDestination}\"");
                continue;
            }

            try
            {
                Directory.CreateDirectory(subfolderDestination);
                logger.Log($"Moving folder: \"{dir.Name}\" -> \"{config.SubfoldersFolderName}\"");
                dir.MoveTo(destPath);
            }
            catch (Exception ex)
            {
                logger.Log($"WARNING: Could not move \"{dir.Name}\". Reason: {ex.Message}");
            }
        }
    }

    private static string GetUniqueFilePath(string intendedPath)
    {
        if (!File.Exists(intendedPath)) return intendedPath;

        var directory = Path.GetDirectoryName(intendedPath);
        var originalFileName = Path.GetFileNameWithoutExtension(intendedPath);
        var extension = Path.GetExtension(intendedPath);
        var counter = 1;
        string newPath;

        do
        {
            var newFileName = $"{originalFileName} ({counter++}){extension}";
            newPath = Path.Combine(directory!, newFileName);
        } while (File.Exists(newPath));

        return newPath;
    }

    private static HashSet<string> GetManagedFolderNames(OrganizerConfig config)
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            config.OthersFolderName,
            config.SubfoldersFolderName
        };
        foreach (var folderName in config.Rules.Select(r => r.DestinationFolder).Distinct())
            folders.Add(folderName);

        return folders;
    }


    private void MoveFile(FileInfo file, string destFolderPath)
    {
        Directory.CreateDirectory(destFolderPath);
        var uniqueDestFilePath = GetUniqueFilePath(Path.Combine(destFolderPath, file.Name));
        logger.Log($"Moving file: \"{file.Name}\" -> \"{Path.GetFileName(destFolderPath)}\"");
        file.MoveTo(uniqueDestFilePath);
    }

    private void CopyFile(FileInfo file, string destFolderPath)
    {
        Directory.CreateDirectory(destFolderPath);
        var uniqueDestFilePath = GetUniqueFilePath(Path.Combine(destFolderPath, file.Name));
        logger.Log($"Copying file: \"{file.Name}\" -> \"{Path.GetFileName(destFolderPath)}\"");
        file.CopyTo(uniqueDestFilePath);
    }

    private void DeleteFile(FileInfo file)
    {
        logger.Log($"Deleting file: \"{file.Name}\"");
        file.Delete();
    }
}