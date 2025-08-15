using Cocona;
using FileOrganizerNET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddSingleton<FileOrganizer>();
var app = builder.Build();

const string defaultConfigName = "config.json";

app.AddCommand("organize", (
        [FromService] FileOrganizer organizer,
        [Argument(Description = "The target directory to organize.")]
        string targetDirectory,
        [Option('c', Description = "Path to a custom JSON config file.")]
        string configFile = defaultConfigName
    ) =>
    {
        var config = LoadConfiguration(configFile);
        if (config is null)
        {
            return -1; 
        }

        Console.WriteLine($"--- Starting organization for: {targetDirectory} ---\n");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        organizer.Organize(targetDirectory, config);

        stopwatch.Stop();
        Console.WriteLine(
            $"\nOrganization complete. Time taken: {stopwatch.ElapsedMilliseconds}ms");
        return 0; 
    })
    .WithDescription("Organizes files and folders in a target directory based on a configuration.");

app.Run();
return;

OrganizerConfig? LoadConfiguration(string configPath)
{
    string resolvedPath;

    // If the provided path is not absolute (e.g., "config.json"),
    // combine it with the application's directory. This ensures it's always found.
    resolvedPath = !Path.IsPathRooted(configPath)
        ? Path.Combine(AppContext.BaseDirectory, configPath)
        : configPath;

    if (!File.Exists(resolvedPath))
    {
        Console.WriteLine($"ERROR: Configuration file not found at: {resolvedPath}");
        Console.WriteLine(
            $"Please provide a valid path with --config or place '{defaultConfigName}' next to the executable.");
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