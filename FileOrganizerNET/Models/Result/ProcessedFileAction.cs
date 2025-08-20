using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Models.Result;

public class ProcessedFileAction
{
    public required string OriginalFilePath { get; init; }
    public required RuleAction Action { get; init; }
    public string? DestinationPath { get; init; }
    public required string ResultMessage { get; init; }
    public required bool IsSuccess { get; init; }
}