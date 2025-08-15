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

    private void ProcessFiles(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders, bool isRecursive, bool isDryRun)
    {
        var searchOption =
            isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in targetDir.GetFiles("*", searchOption))
        {
            if (file.Name.Equals("config.json", StringComparison.OrdinalIgnoreCase)) continue;

            if (isRecursive && managedFolders.Contains(
                    file.DirectoryName?.Split(Path.DirectorySeparatorChar).Last() ?? string.Empty))
                continue;

            string? destFolderName = null;
            foreach (var rule in config.Rules)
                if (DoesFileMatchRule(file, rule.Conditions))
                {
                    destFolderName = rule.DestinationFolder;
                    break; // The first rule wins.
                }

            // If no rule matched, use the default "Others" folder.
            destFolderName ??= config.OthersFolderName;

            var destFolderPath = Path.Combine(targetDir.FullName, destFolderName);
            var uniqueDestFilePath = Path.Combine(destFolderPath, file.Name);

            if (isDryRun)
            {
                logger.Log(
                    $"[DRY RUN] Would move file: \"{file.FullName}\" -> \"{destFolderPath}\"");
                continue;
            }

            try
            {
                Directory.CreateDirectory(destFolderPath);
                uniqueDestFilePath = GetUniqueFilePath(uniqueDestFilePath);
                logger.Log($"Moving file: \"{file.Name}\" -> \"{destFolderName}\"");
                file.MoveTo(uniqueDestFilePath);
            }
            catch (Exception ex)
            {
                logger.Log($"WARNING: Could not move \"{file.Name}\". Reason: {ex.Message}");
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
}