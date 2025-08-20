using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Contracts;

public interface IConfigLoader
{
    /// <summary>
    ///     Loads the OrganizerConfig from a specified file path.
    ///     Resolves relative paths against the application's base directory.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>The loaded OrganizerConfig or null if loading fails.</returns>
    OrganizerConfig? LoadConfiguration(string configPath);

    /// <summary>
    ///     Generates a default configuration file at the specified path.
    /// </summary>
    /// <param name="outputPath">The path where the default config file should be created.</param>
    /// <param name="overwrite">If true, overwrite existing file without prompt.</param>
    /// <returns>True if the file was created or already existed, false on error or user cancellation.</returns>
    bool GenerateDefaultConfiguration(string outputPath, bool overwrite);
}