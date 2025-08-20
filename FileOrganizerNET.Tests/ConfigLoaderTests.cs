using FileOrganizerNET.Concrete;
using FileOrganizerNET.Contracts;
using Moq;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class ConfigLoaderTests
{
    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigLoaderTests", Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);
        _defaultConfigPath = Path.Combine(_testDirectory, "config.json");

        _mockLogger = new Mock<IFileLogger>();
        // Redirect Console.Out to capture output (for mocking logger interactions)
        _originalConsoleOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);

        _originalConsoleIn = Console.In; // Store original for redirecting Console.In

        _configLoader = new ConfigLoader(_mockLogger.Object);
    }

    [TearDown]
    public void Teardown()
    {
        Console.SetOut(_originalConsoleOut); // Restore Console.Out
        Console.SetIn(_originalConsoleIn); // Restore Console.In

        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);

        _consoleOutput.Dispose();
    }

    private string _testDirectory = null!;
    private string _defaultConfigPath = null!;
    private StringWriter _consoleOutput = null!; // Still used for console redirect for ReadLine
    private TextReader _originalConsoleIn = null!;
    private TextWriter _originalConsoleOut = null!;
    private Mock<IFileLogger> _mockLogger = null!;
    private ConfigLoader _configLoader = null!;

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

        var config = _configLoader.LoadConfiguration(_defaultConfigPath);

        Assert.That(config, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(config!.Rules, Has.Count.EqualTo(1));
            Assert.That(config.Rules[0].DestinationFolder, Is.EqualTo("TestFolder"));
            Assert.That(config.OthersFolderName, Is.EqualTo("TestOthers"));
        });
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("--- Loading configuration from:") && s.Contains(_defaultConfigPath))),
            Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Configuration loaded successfully from") && s.Contains("is valid."))),
            Times.Once);
    }

    [Test]
    public void LoadConfiguration_ReturnsNullAndLogsError_ForNonExistentFile()
    {
        var config = _configLoader.LoadConfiguration(Path.Combine(_testDirectory, "nonexistent.json"));

        Assert.That(config, Is.Null);
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("ERROR: Configuration file not found at:") && s.Contains("nonexistent.json"))),
            Times.Once);
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Please provide a valid path"))), Times.Once);
    }

    [Test]
    public void LoadConfiguration_ReturnsNullAndLogsError_ForInvalidJsonFormat()
    {
        File.WriteAllText(_defaultConfigPath, "{ \"Rules\": [ "); // Malformed JSON

        var config = _configLoader.LoadConfiguration(_defaultConfigPath);

        Assert.That(config, Is.Null);
        // Verify logger calls
        _mockLogger.Verify(
            l => l.Log(It.Is<string>(s =>
                s.Contains("ERROR: Configuration file") && s.Contains("has invalid JSON format.") && s.Contains(_defaultConfigPath))), Times.Once);
    }

    [Test]
    public void GenerateDefaultConfiguration_CreatesNewFile_WhenNotExists()
    {
        var result = _configLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.Exists(_defaultConfigPath), Is.True);
        });
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Default configuration file created at:") && s.Contains(_defaultConfigPath))),
            Times.Once);
    }

    [Test]
    public void GenerateDefaultConfiguration_OverwritesExistingFile_WhenForceIsTrue()
    {
        File.WriteAllText(_defaultConfigPath, "Old content");

        var result = _configLoader.GenerateDefaultConfiguration(_defaultConfigPath, true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.ReadAllText(_defaultConfigPath), Does.Not.Contain("Old content"));
        });
        Assert.That(File.ReadAllText(_defaultConfigPath), Does.Contain("OthersFolderName")); // Verify default content
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Default configuration file created at:") && s.Contains(_defaultConfigPath))),
            Times.Once);
    }

    [Test]
    public void GenerateDefaultConfiguration_DoesNotOverwriteExistingFile_WhenForceIsFalseAndUserDeclines()
    {
        File.WriteAllText(_defaultConfigPath, "Original content");
        Console.SetIn(new StringReader("n\n")); // Simulate 'n' input for no

        var result = _configLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(File.ReadAllText(_defaultConfigPath), Is.EqualTo("Original content"));
        });
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Operation cancelled."))), Times.Once);
    }

    [Test]
    public void GenerateDefaultConfiguration_OverwritesExistingFile_WhenForceIsFalseAndUserAccepts()
    {
        File.WriteAllText(_defaultConfigPath, "Original content");
        Console.SetIn(new StringReader("y\n")); // Simulate 'y' input for yes

        var result = _configLoader.GenerateDefaultConfiguration(_defaultConfigPath, false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(File.ReadAllText(_defaultConfigPath), Does.Not.Contain("Original content"));
        });
        Assert.That(File.ReadAllText(_defaultConfigPath), Does.Contain("OthersFolderName")); // Verify default content
        // Verify logger calls
        _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Default configuration file created at:") && s.Contains(_defaultConfigPath))),
            Times.Once);
    }
}