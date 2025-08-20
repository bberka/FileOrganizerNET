namespace FileOrganizerNET.Models.Result;

public class DuplicateCheckResult
{
    public required int FilesHashed { get; init; }
    public required int DuplicateSetsFound { get; init; }
    public required int DuplicateFilesDeleted { get; init; }
    public List<string> Errors { get; init; } = [];
}