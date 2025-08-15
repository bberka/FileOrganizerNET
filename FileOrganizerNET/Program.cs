using Cocona;
using FileOrganizerNET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const string defaultConfigName = "config.json";

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<FileOrganizer>();
var app = builder.Build();

var asmVersion = typeof(Program).Assembly.GetName().Version;
Console.WriteLine($"FileOrganizerNET v{asmVersion}");

app.AddCommand("organize", (
        [Argument(Description = "The target directory to organize.")] string targetDirectory,
        [Option('c', Description = "Path to a custom JSON config file.")] string configFile = "config.json",
        [Option('r', Description = "Process files in all subdirectories recursively.")] bool recursive = false,
        [Option(Description = "Simulate the organization without moving any files.")] bool dryRun = false,
        [Option('l', Description = "Path to a file to write log output.")] string? logFile = null
    ) =>
    {
        var config = LoadConfiguration(configFile);
        if (config is null) return -1;

        var logger = new FileLogger(logFile);
        var organizer = new FileOrganizer(logger);

        logger.Log($"--- Starting organization for: {targetDirectory} ---\n");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        organizer.Organize(targetDirectory, config, recursive, dryRun);

        stopwatch.Stop();
        logger.Log($"\nOrganization complete. Time taken: {stopwatch.ElapsedMilliseconds}ms");
        return 0;
    })
    .WithDescription("Organizes files and folders in a target directory based on a configuration.");

app.Run();
return;

OrganizerConfig? LoadConfiguration(string configPath)
{
    var resolvedPath = !Path.IsPathRooted(configPath)
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