namespace FileOrganizerNET.Models.Config;

public class Rule
{
    public RuleAction Action { get; init; } = RuleAction.Move;
    public string DestinationFolder { get; init; } = string.Empty;
    public RuleConditions Conditions { get; init; } = new();
}