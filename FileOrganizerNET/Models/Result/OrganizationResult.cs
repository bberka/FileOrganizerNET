namespace FileOrganizerNET.Models.Result;

public class OrganizationResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; } = string.Empty;
    public FileProcessingResult? FileProcessingOutcome { get; init; }
    public FolderProcessingResult? FolderProcessingOutcome { get; init; }
    public DuplicateCheckResult? DuplicateCheckOutcome { get; init; }
    public required TimeSpan ElapsedTime { get; init; }
}