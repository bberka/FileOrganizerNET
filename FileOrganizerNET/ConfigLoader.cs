using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FileOrganizerNET;

public static class ConfigLoader
{
    public const string DefaultConfigName = "config.json";

    /// <summary>
    ///     Loads the OrganizerConfig from a specified file path.
    ///     Resolves relative paths against the application's base directory.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>The loaded OrganizerConfig or null if loading fails.</returns>
    public static OrganizerConfig? LoadConfiguration(string configPath)
    {
        var resolvedPath = Path.IsPathRooted(configPath)
            ? configPath
            : Path.Combine(AppContext.BaseDirectory, configPath);

        if (!File.Exists(resolvedPath))
        {
            Console.WriteLine($"ERROR: Configuration file not found at: {resolvedPath}");
            Console.WriteLine(
                $"Please provide a valid path with --config or place '{DefaultConfigName}' next to the executable.");
            return null;
        }

        try
        {
            Console.WriteLine($"--- Loading configuration from: {resolvedPath} ---");
            var config = new OrganizerConfig();
            new ConfigurationBuilder()
                .AddJsonFile(resolvedPath, false)
                .Build()
                .Bind(config);
            return config;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                $"ERROR: Configuration file '{resolvedPath}' has invalid JSON format. Details: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR: Failed to load configuration from '{resolvedPath}'. Details: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Generates a default configuration file at the specified path.
    /// </summary>
    /// <param name="outputPath">The path where the default config file should be created.</param>
    /// <param name="overwrite">If true, overwrite existing file without prompt.</param>
    /// <returns>True if the file was created or already existed, false on error or user cancellation.</returns>
    public static bool GenerateDefaultConfiguration(string outputPath, bool overwrite)
    {
        if (File.Exists(outputPath))
            if (!overwrite)
            {
                Console.Write(
                    $"Configuration file '{outputPath}' already exists. Overwrite? (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLower();
                if (response != "y")
                {
                    Console.WriteLine("Operation cancelled.");
                    return false;
                }
            }

        try
        {
            var defaultConfig = GetDefaultConfig();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(defaultConfig, options);

            File.WriteAllText(outputPath, jsonString);
            Console.WriteLine($"Default configuration file created at: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERROR: Failed to generate default configuration at '{outputPath}'. Details: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Provides the default OrganizerConfig structure, including common rules.
    /// </summary>
    private static OrganizerConfig GetDefaultConfig()
    {
        return new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    Action = RuleAction.Delete,
                    Conditions = new RuleConditions
                        { Extensions = [".tmp", ".bak", ".log"] }
                },

                new Rule
                {
                    Action = RuleAction.Copy,
                    DestinationFolder = "Backups",
                    Conditions = new RuleConditions
                        { FileNameContains = ["important"] }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Archive/Old Files",
                    Conditions = new RuleConditions { OlderThanDays = 365 }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Large Files",
                    Conditions = new RuleConditions { MinSizeMb = 1024 }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Photos",
                    Conditions = new RuleConditions
                    {
                        Extensions =
                        [
                            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".svg",
                            ".ai",
                            ".eps", ".psd", ".tiff", ".cr2", ".nef", ".orf", ".sr2"
                        ]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Archives",
                    Conditions = new RuleConditions
                    {
                        Extensions =
                            [".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".iso", ".img", ".dmg"]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Documents",
                    Conditions = new RuleConditions
                    {
                        Extensions =
                        [
                            ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt",
                            ".rtf",
                            ".odt", ".ods", ".md", ".csv", ".epub", ".mobi"
                        ]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Executables",
                    Conditions = new RuleConditions
                    {
                        Extensions = [".exe", ".msi", ".jar"]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Scripts",
                    Conditions = new RuleConditions
                    {
                        Extensions = [".bat", ".cmd", ".ps1", ".sh"]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Videos",
                    Conditions = new RuleConditions
                    {
                        Extensions = [".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".flv"]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Audio",
                    Conditions = new RuleConditions
                    {
                        Extensions = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a"]
                    }
                },

                new Rule
                {
                    Action = RuleAction.Move,
                    DestinationFolder = "Source Code",
                    Conditions = new RuleConditions
                    {
                        Extensions =
                        [
                            ".html", ".css", ".js", ".ts", ".jsx", ".tsx", ".php", ".scss", ".less",
                            ".vue", ".c", ".h", ".cpp", ".hpp", ".cs", ".csproj", ".sln", ".vb",
                            ".java", ".kt", ".swift", ".py", ".go", ".rs", ".rb", ".sh", ".pl",
                            ".sql",
                            ".json", ".xml", ".yml", ".yaml", ".toml", ".dockerfile", ".gitignore"
                        ]
                    }
                }
            ],
            OthersFolderName = "Others",
            SubfoldersFolderName = "Folders"
        };
    }
}