namespace FileOrganizerNET;

public class FileOrganizer
{
    public void Organize(string targetPath, OrganizerConfig config)
    {
        var targetDir = new DirectoryInfo(targetPath);
        if (!targetDir.Exists)
        {
            Console.WriteLine($"ERROR: Target directory not found: {targetPath}");
            return;
        }

        // Get all potential managed folder names for exclusion later.
        var managedFolders = GetManagedFolderNames(config);

        Console.WriteLine("--- Processing Files ---");
        ProcessFiles(targetDir, config);
        Console.WriteLine();

        Console.WriteLine("--- Processing Folders ---");
        ProcessFolders(targetDir, config, managedFolders);
    }

    private void ProcessFiles(DirectoryInfo targetDir, OrganizerConfig config)
    {
        foreach (var file in targetDir.GetFiles())
        {
            // Skip the config file itself if it's in the target directory.
            if (file.Name.Equals("default-config.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var extension = file.Extension.ToLowerInvariant();
            var destFolderName =
                config.ExtensionMappings.GetValueOrDefault(extension, config.OthersFolderName);
            var destFolderPath = Path.Combine(targetDir.FullName, destFolderName);

            // **CHANGE**: Create the destination folder only when it's needed.
            // This is idempotent; it does nothing if the folder already exists.
            Directory.CreateDirectory(destFolderPath);

            var uniqueDestFilePath = GetUniqueFilePath(Path.Combine(destFolderPath, file.Name));

            try
            {
                Console.WriteLine($"Moving file: \"{file.Name}\" -> \"{destFolderName}\"");
                file.MoveTo(uniqueDestFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not move \"{file.Name}\". Reason: {ex.Message}");
            }
        }
    }

    private void ProcessFolders(DirectoryInfo targetDir, OrganizerConfig config,
        HashSet<string> managedFolders)
    {
        var subfolderDestination = Path.Combine(targetDir.FullName, config.SubfoldersFolderName);

        foreach (var dir in targetDir.GetDirectories())
        {
            // Skip any folder whose name matches one of our category folders.
            // This correctly handles multiple runs.
            if (managedFolders.Contains(dir.Name)) continue;

            // Ensure the main "Folders" directory exists before moving into it.
            Directory.CreateDirectory(subfolderDestination);
            var destPath = Path.Combine(subfolderDestination, dir.Name);

            try
            {
                Console.WriteLine(
                    $"Moving folder: \"{dir.Name}\" -> \"{config.SubfoldersFolderName}\"");
                dir.MoveTo(destPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not move \"{dir.Name}\". Reason: {ex.Message}");
            }
        }
    }

    private string GetUniqueFilePath(string intendedPath)
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

    private HashSet<string> GetManagedFolderNames(OrganizerConfig config)
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        folders.Add(config.OthersFolderName);
        folders.Add(config.SubfoldersFolderName);
        foreach (var folderName in config.ExtensionMappings.Values) folders.Add(folderName);

        return folders;
    }
}