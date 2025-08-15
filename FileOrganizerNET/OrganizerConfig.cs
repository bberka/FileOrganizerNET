using System.Text.Json.Serialization;

namespace FileOrganizerNET;

public class OrganizerConfig
{
    /// <summary>
    ///     An ordered list of rules to be applied to files.
    ///     The first rule that a file matches will be used.
    /// </summary>
    public List<Rule> Rules { get; init; } = [];

    public string OthersFolderName { get; init; } = "Others";
    public string SubfoldersFolderName { get; init; } = "Folders";
}

public class Rule
{
    public RuleAction Action { get; init; } = RuleAction.Move;
    public string DestinationFolder { get; init; } = string.Empty;
    public RuleConditions Conditions { get; init; } = new();
}

public class RuleConditions
{
    [JsonPropertyName("extensions")] public List<string>? Extensions { get; init; }

    [JsonPropertyName("fileNameContains")] public List<string>? FileNameContains { get; init; }

    [JsonPropertyName("olderThanDays")] public int? OlderThanDays { get; init; }

    [JsonPropertyName("minSizeMB")] public long? MinSizeMb { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleAction
{
    Move,
    Copy,
    Delete
}