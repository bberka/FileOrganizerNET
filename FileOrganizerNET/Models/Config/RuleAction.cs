using System.Text.Json.Serialization;

namespace FileOrganizerNET.Models.Config;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleAction
{
    Move,
    Copy,
    Delete
}