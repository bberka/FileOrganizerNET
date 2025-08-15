using Microsoft.Extensions.Configuration;

namespace FileOrganizerNET;

public static class ConfigLoader
{
    private const string DefaultConfigName = "config.json";

    public static OrganizerConfig? LoadConfiguration(string configPath)
    {
        var resolvedPath = !Path.IsPathRooted(configPath)
            ? Path.Combine(AppContext.BaseDirectory, configPath)
            : configPath;

        if (!File.Exists(resolvedPath))
        {
            Console.WriteLine($"ERROR: Configuration file not found at: {resolvedPath}");
            Console.WriteLine(
                $"Please provide a valid path with --config or place '{DefaultConfigName}' next to the executable.");
            return null;
        }

        Console.WriteLine($"--- Loading configuration from: {resolvedPath} ---");
        var config = new OrganizerConfig();
        new ConfigurationBuilder()
            .AddJsonFile(resolvedPath, optional: false)
            .Build()
            .Bind(config);

        return config;
    }
}