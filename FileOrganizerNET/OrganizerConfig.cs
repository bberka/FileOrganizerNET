namespace FileOrganizerNET;

/// <summary>
///     Defines the configuration for the file organizer.
/// </summary>
public class OrganizerConfig
{
    /// <summary>
    ///     Maps file extensions (e.g., ".jpg") to a category folder name (e.g., "Photos").
    /// </summary>
    public Dictionary<string, string> ExtensionMappings { get; set; } = new();

    /// <summary>
    ///     The name of the folder for files that don't match any mapping.
    /// </summary>
    public string OthersFolderName { get; set; } = "Others";

    /// <summary>
    ///     The name of the folder to move other subdirectories into.
    /// </summary>
    public string SubfoldersFolderName { get; set; } = "Folders";
}