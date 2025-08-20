using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Utils;

public static class RuleMatcher
{
    /// <summary>
    ///     Determines if a given file matches the conditions defined in a rule.
    ///     A file must satisfy ALL defined conditions within a rule to be considered a match.
    /// </summary>
    public static bool DoesFileMatchRule(FileInfo file, RuleConditions conditions)
    {
        if (conditions.Extensions?.Count > 0)
            if (!conditions.Extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                return false;

        if (conditions.FileNameContains?.Count > 0)
            if (!conditions.FileNameContains.Any(keyword => file.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return false;

        if (conditions.OlderThanDays.HasValue)
            if (file.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-conditions.OlderThanDays.Value))
                return false;

        if (conditions.MinSizeMb.HasValue)
        {
            var minSizeBytes = conditions.MinSizeMb.Value * 1024 * 1024;
            if (file.Length < minSizeBytes) return false;
        }

        return true;
    }
}