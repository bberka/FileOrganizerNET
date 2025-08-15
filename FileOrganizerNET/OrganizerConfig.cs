using System.Text.Json.Serialization;

namespace FileOrganizerNET;

public class OrganizerConfig
{
    /// <summary>
    ///     An ordered list of rules to be applied to files.
    ///     The first rule that a file matches will be used.
    /// </summary>
    public List<Rule> Rules { get; set; } = [];

    public string OthersFolderName { get; set; } = "Others";
    public string SubfoldersFolderName { get; set; } = "Folders";
}

public class Rule
{
    public string DestinationFolder { get; set; } = string.Empty;
    public RuleConditions Conditions { get; set; } = new();
}

public class RuleConditions
{
    [JsonPropertyName("extensions")] public List<string>? Extensions { get; set; }

    [JsonPropertyName("fileNameContains")] public List<string>? FileNameContains { get; set; }

    [JsonPropertyName("olderThanDays")] public int? OlderThanDays { get; set; }

    [JsonPropertyName("minSizeMB")] public long? MinSizeMb { get; set; }
}