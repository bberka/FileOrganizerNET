namespace FileOrganizerNET.Tests;

[TestFixture]
public class ConfigLoaderTests
{
    [SetUp]
    public void Setup()
    {
        _testDirectory =
            Path.Combine(Path.GetTempPath(), "ConfigLoaderTests", Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);
        _defaultConfigPath = Path.Combine(_testDirectory, "config.json");

        _originalConsoleOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);

        _originalConsoleIn = Console.In;
    }

    [TearDown]
    public void Teardown()
    {
        Console.SetOut(_originalConsoleOut);
        Console.SetIn(_originalConsoleIn);

        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);

        _consoleOutput.Dispose();
    }

    private string _testDirectory = null!;
    private string _defaultConfigPath = null!;
    private StringWriter _consoleOutput = null!;
    private TextReader _originalConsoleIn = null!;
    private TextWriter _originalConsoleOut = null!;

    [Test]
    public void LoadConfiguration_LoadsValidConfigFromFile()
    {
        const string validJson = """
                                 {
                                                 "Rules": [
                                                     {
                                                         "Action": "Move",
                                                         "DestinationFolder": "TestFolder",
                                                         "Conditions": { "extensions": [".txt"] }
                                                     }
                                                 ],
                                                 "OthersFolderName": "TestOthers",
                                                 "SubfoldersFolderName": "TestFolders"
                                             }
                                 """;
        File.WriteAllText(_defaultConfigPath, validJson);

        var config = ConfigLoader.LoadConfiguration(_defaultConfigPath);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Rules, Has.Count.EqualTo(1));
            Assert.That(config.Rules[0].DestinationFolder, Is.EqualTo("TestFolder"));
            Assert.That(config.OthersFolderName, Is.EqualTo("TestOthers"));
            Assert.That(_consoleOutput.ToString(),
                Does.Contain("--- Loading configuration from:").And.Contain("is valid.")
                    .IgnoreCase);
        });
    }

    [Test]
    public void LoadConfiguration_ReturnsNullAndLogsError_ForNonExistentFile()
    {
        var config =
            ConfigLoader.LoadConfiguration(Path.Combine(_testDirectory, "nonexistent.json"));

        Assert.Multiple(() =>
        {
            Assert.That(config, Is.Null);
            Assert.That(_consoleOutput.ToString(),
                Does.Contain("ERROR: Configuration file not found at:").And
                    .Contain("Please provide a valid path").IgnoreCase);
        });
    }

    [Test]
    public void LoadConfiguration_ReturnsNullAndLogsError_ForInvalidJsonFormat()
    {
        File.WriteAllText(_defaultConfigPath, "{ \"Rules\": [ ");

        var config = ConfigLoader.LoadConfiguration(_defaultConfigPath);

        Assert.Multiple(() =>
        {
            Assert.That(config, Is.Null);
            Assert.That(_consoleOutput.ToString(),
                Does.Contain("ERROR: Configuration file").And.Contain("has invalid JSON format.")
                    .IgnoreCase);
        });
    }

    [Test]
    public void GenerateDefaultConfiguration_CreatesNewFile_WhenNotExists()
    {
        var result = ConfigLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.Exists(_defaultConfigPath), Is.True);
            Assert.That(_consoleOutput.ToString(),
                Does.Contain("Default configuration file created at:"));
        });
    }

    [Test]
    public void GenerateDefaultConfiguration_OverwritesExistingFile_WhenForceIsTrue()
    {
        File.WriteAllText(_defaultConfigPath, "Old content");

        var result = ConfigLoader.GenerateDefaultConfiguration(_defaultConfigPath, true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.ReadAllText(_defaultConfigPath), Does.Not.Contain("Old content"));
        });
        Assert.That(File.ReadAllText(_defaultConfigPath),
            Does.Contain("OthersFolderName")); // Check for known default content
    }

    [Test]
    public void
        GenerateDefaultConfiguration_DoesNotOverwriteExistingFile_WhenForceIsFalseAndUserDeclines()
    {
        File.WriteAllText(_defaultConfigPath, "Original content");
        Console.SetIn(new StringReader("n\n")); // Simulate 'n' for no, followed by newline

        var result = ConfigLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(File.ReadAllText(_defaultConfigPath), Is.EqualTo("Original content"));
            Assert.That(_consoleOutput.ToString(),
                Does.Contain("Configuration file").And.Contain("already exists.").And
                    .Contain("Operation cancelled."));
        });
    }

    [Test]
    public void GenerateDefaultConfiguration_OverwritesExistingFile_WhenForceIsFalseAndUserAccepts()
    {
        File.WriteAllText(_defaultConfigPath, "Original content");
        Console.SetIn(new StringReader("y\n")); // Simulate 'y' for yes, followed by newline

        var result = ConfigLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.ReadAllText(_defaultConfigPath), Does.Not.Contain("Original content"));
        });
        Assert.That(File.ReadAllText(_defaultConfigPath),
            Does.Contain("OthersFolderName")); // Check for known default content
    }
}