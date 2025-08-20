namespace FileOrganizerNET.Models.Result;

public class FileProcessingResult
{
    public required int FilesScanned { get; init; }
    public required List<ProcessedFileAction> ActionsTaken { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}