namespace FileOrganizerNET.Models.Config;

public class OrganizerConfig
{
    /// <summary>
    ///     An ordered list of rules to be applied to files.
    ///     The first rule that a file matches will be used.
    /// </summary>
    public required List<Rule> Rules { get; init; }

    public string OthersFolderName { get; init; } = "Others";
    public string SubfoldersFolderName { get; init; } = "Folders";
}