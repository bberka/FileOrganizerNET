namespace FileOrganizerNET.Models.Result;

public class FolderProcessingResult
{
    public required int FoldersScanned { get; init; }
    public required int FoldersMoved { get; init; }
    public List<string> Errors { get; init; } = [];
}