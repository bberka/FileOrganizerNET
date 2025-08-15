using System.Diagnostics;
using Cocona;
using FileOrganizerNET;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<FileOrganizer>();
var app = builder.Build();

var asmVersion = typeof(Program).Assembly.GetName().Version;
Console.WriteLine($"FileOrganizerNET v{asmVersion}");

app.AddCommand("organize", (
        [Argument(Description = "The target directory to organize.")]
        string targetDirectory,
        [Option('c', Description = "Path to a custom JSON config file.")]
        string configFile = ConfigLoader.DefaultConfigName,
        [Option('r', Description = "Process files in all subdirectories recursively.")]
        bool recursive = false,
        [Option(Description = "Simulate the organization without moving any files.")]
        bool dryRun = false,
        [Option('l', Description = "Path to a file to write log output.")]
        string? logFile = null
    ) =>
    {
        var config = ConfigLoader.LoadConfiguration(configFile);
        if (config is null) return 1;

        var logger = new FileLogger(logFile);
        var organizer = new FileOrganizer(logger);

        logger.Log($"--- Starting organization for: {targetDirectory} ---\n");
        var stopwatch = Stopwatch.StartNew();

        organizer.Organize(targetDirectory, config, recursive, dryRun);

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
    ) => ConfigLoader.GenerateDefaultConfiguration(outputPath, force) ? 0 : 1)
    .WithDescription("Generates a default 'config.json' file.");

app.AddCommand("validate", (
        [Argument(Description = "The path to the config file to validate.")]
        string configPath = ConfigLoader.DefaultConfigName
    ) =>
    {
        var config = ConfigLoader.LoadConfiguration(configPath);
        if (config != null)
        {
            Console.WriteLine($"Configuration '{configPath}' is valid.");
            return 0;
        }

        Console.WriteLine(
            $"Configuration '{configPath}' is invalid. Please check error messages above.");
        return 1;
    })
    .WithDescription("Validates the syntax and structure of a 'config.json' file.");


app.Run();