using System.IO.Hashing;
using FileOrganizerNET.Contracts;
using FileOrganizerNET.Models.Config;
using FileOrganizerNET.Models.Result;

namespace FileOrganizerNET.Utils;

public class FileSystemActions(IFileLogger logger)
{
    /// <summary>
    ///     Moves a file to a specified destination folder, handling name collisions.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to move.</param>
    /// <param name="destFolderPath">The path to the destination folder.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    public ProcessedFileAction MoveFile(FileInfo file, string destFolderPath)
    {
        try
        {
            Directory.CreateDirectory(destFolderPath);
            var uniqueDestFilePath = GetUniqueFilePath(Path.Combine(destFolderPath, file.Name));

            file.MoveTo(uniqueDestFilePath);
            logger.Log($"Moving file: \"{file.Name}\" -> \"{Path.GetFileName(destFolderPath)}\"");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Move,
                DestinationPath = uniqueDestFilePath,
                IsSuccess = true,
                ResultMessage = $"Moved to {uniqueDestFilePath}"
            };
        }
        catch (Exception ex)
        {
            logger.Log($"WARNING: Could not move \"{file.Name}\". Reason: {ex.Message}");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Move,
                IsSuccess = false,
                ResultMessage = $"Failed to move: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     Copies a file to a specified destination folder, handling name collisions.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to copy.</param>
    /// <param name="destFolderPath">The path to the destination folder.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    public ProcessedFileAction CopyFile(FileInfo file, string destFolderPath)
    {
        try
        {
            Directory.CreateDirectory(destFolderPath);
            var uniqueDestFilePath = GetUniqueFilePath(Path.Combine(destFolderPath, file.Name));

            file.CopyTo(uniqueDestFilePath);
            logger.Log($"Copying file: \"{file.Name}\" -> \"{Path.GetFileName(destFolderPath)}\"");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Copy,
                DestinationPath = uniqueDestFilePath,
                IsSuccess = true,
                ResultMessage = $"Copied to {uniqueDestFilePath}"
            };
        }
        catch (Exception ex)
        {
            logger.Log($"WARNING: Could not copy \"{file.Name}\". Reason: {ex.Message}");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Copy,
                IsSuccess = false,
                ResultMessage = $"Failed to copy: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     Deletes a specified file.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to delete.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    public ProcessedFileAction DeleteFile(FileInfo file)
    {
        try
        {
            file.Delete();
            logger.Log($"Deleting file: \"{file.Name}\"");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Delete,
                IsSuccess = true,
                ResultMessage = "Deleted successfully"
            };
        }
        catch (Exception ex)
        {
            logger.Log($"WARNING: Could not delete \"{file.Name}\". Reason: {ex.Message}");
            return new ProcessedFileAction
            {
                OriginalFilePath = file.FullName,
                Action = RuleAction.Delete,
                IsSuccess = false,
                ResultMessage = $"Failed to delete: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     Finds a unique file path by appending (1), (2), etc., if a file already exists.
    /// </summary>
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

    /// <summary>
    ///     Calculates the XXHash128 checksum for a given file stream.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The hexadecimal string representation of the hash, or null if an error occurs.</returns>
    public string? GetXxHash128(string filePath)
    {
        try
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var xxHash = new XxHash128();
            xxHash.Append(fs);
            var hash = xxHash.GetCurrentHash();
            return Convert.ToHexString(hash);
        }
        catch (Exception ex)
        {
            logger.Log($"WARNING: Could not compute hash for \"{filePath}\". Reason: {ex.Message}");
            return null;
        }
    }
}