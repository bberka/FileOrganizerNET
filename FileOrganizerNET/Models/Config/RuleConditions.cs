using System.Text.Json.Serialization;

namespace FileOrganizerNET.Models.Config;

public class RuleConditions
{
    [JsonPropertyName("extensions")] public List<string>? Extensions { get; init; }

    [JsonPropertyName("fileNameContains")] public List<string>? FileNameContains { get; init; }

    [JsonPropertyName("olderThanDays")] public int? OlderThanDays { get; init; }

    [JsonPropertyName("minSizeMB")] public long? MinSizeMb { get; init; }
}