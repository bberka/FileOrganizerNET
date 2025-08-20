using System.Diagnostics;
using Cocona;
using FileOrganizerNET.Concrete;
using FileOrganizerNET.Contracts;
using FileOrganizerNET.Utils;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<IFileLogger, FileLogger>();
builder.Services.AddSingleton<IConfigLoader, ConfigLoader>();
builder.Services.AddSingleton<FileSystemActions>();
builder.Services.AddSingleton<IFileOrganizer, FileOrganizer>();
var app = builder.Build();

var asmVersion = typeof(Program).Assembly.GetName().Version;

app.AddCommand("organize", (
        [Argument(Description = "The target directory to organize.")]
        string targetDirectory,
        [Option('c', Description = "Path to a custom JSON config file.")]
        string configFile = ConfigLoader.DefaultConfigName,
        [Option('r', Description = "Process files in all subdirectories recursively.")]
        bool recursive = false,
        [Option(Description = "Simulate the organization without moving any files.")]
        bool dryRun = false,
        [Option(Description = "Checks and removes duplicate files with XxHash.")]
        bool checkDuplicates = false
    ) =>
    {
        var logger = app.Services.GetRequiredService<IFileLogger>();
        logger.Log($"FileOrganizerNET v{asmVersion?.ToString(3)}");

        var configLoader = app.Services.GetRequiredService<IConfigLoader>();
        var config = configLoader.LoadConfiguration(configFile);
        if (config is null) return 1;

        var organizer = app.Services.GetRequiredService<IFileOrganizer>();

        logger.Log($"--- Starting organization for: {targetDirectory} ---\n");
        logger.Log("Args:");
        logger.Log($"  Target Directory: {targetDirectory}");
        logger.Log($"  Config File: {configFile}");
        logger.Log($"  Recursive: {recursive}");
        logger.Log($"  Dry Run: {dryRun}");
        logger.Log($"  Check Duplicates: {checkDuplicates}");
        var stopwatch = Stopwatch.StartNew();

        organizer.Organize(targetDirectory, config, recursive, dryRun, checkDuplicates);

        stopwatch.Stop();
        logger.Log($"\nOrganization complete. Time taken: {stopwatch.ElapsedMilliseconds}ms");
        return 0;
    })
    .WithDescription("Organizes files and folders in a target directory based on a configuration.");

app.AddCommand("init", (
        [Argument(Description = "The path where the default config file will be created.")]
        string outputPath = ConfigLoader.DefaultConfigName,
        [Option('f', Description = "Force overwrite if the config file already exists.")]
        bool force = false
    ) =>
    {
        var logger = app.Services.GetRequiredService<IFileLogger>();
        logger.Log($"FileOrganizerNET v{asmVersion?.ToString(3)}");
        var configLoader = app.Services.GetRequiredService<IConfigLoader>();
        return configLoader.GenerateDefaultConfiguration(outputPath, force) ? 0 : 1;
    })
    .WithDescription("Generates a default 'config.json' file.");

app.AddCommand("validate", (
        [Argument(Description = "The path to the config file to validate.")]
        string configPath = ConfigLoader.DefaultConfigName
    ) =>
    {
        var logger = app.Services.GetRequiredService<IFileLogger>();
        logger.Log($"FileOrganizerNET v{asmVersion?.ToString(3)}");
        var configLoader = app.Services.GetRequiredService<IConfigLoader>();
        var config = configLoader.LoadConfiguration(configPath);
        if (config != null)
        {
            logger.Log($"Configuration '{configPath}' is valid.");
            return 0;
        }

        logger.Log(
            $"Configuration '{configPath}' is invalid. Please check error messages above.");
        return 1;
    })
    .WithDescription("Validates the syntax and structure of a 'config.json' file.");


app.Run();