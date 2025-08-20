using FileOrganizerNET.Models.Result;

namespace FileOrganizerNET.Contracts;

public interface IFileSystemActions
{
    /// <summary>
    ///     Moves a file to a specified destination folder, handling name collisions.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to move.</param>
    /// <param name="destFolderPath">The path to the destination folder.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    ProcessedFileAction MoveFile(FileInfo file, string destFolderPath);

    /// <summary>
    ///     Copies a file to a specified destination folder, handling name collisions.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to copy.</param>
    /// <param name="destFolderPath">The path to the destination folder.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    ProcessedFileAction CopyFile(FileInfo file, string destFolderPath);

    /// <summary>
    ///     Deletes a specified file.
    /// </summary>
    /// <param name="file">The FileInfo object of the file to delete.</param>
    /// <returns>A ProcessedFileAction detailing the outcome.</returns>
    ProcessedFileAction DeleteFile(FileInfo file);

    /// <summary>
    ///     Finds a unique file path by appending (1), (2), etc., if a file already exists.
    /// </summary>
    string GetUniqueFilePath(string intendedPath);

    /// <summary>
    ///     Calculates the XXHash128 checksum for a given file stream.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The hexadecimal string representation of the hash, or null if an error occurs.</returns>
    string? GetXxHash128(string filePath);
}