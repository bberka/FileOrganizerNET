using Moq;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class FileOrganizerTests
{
    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileOrganizerTests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);

        _config = new OrganizerConfig
        {
            ExtensionMappings =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { ".jpg", "Photos" },
                    { ".pdf", "Documents" },
                    { ".zip", "Archives" },
                    { ".mp4", "Videos" }
                },
            OthersFolderName = "Others",
            SubfoldersFolderName = "Folders"
        };

        _logOutput = [];
        _mockLogger = new Mock<IFileLogger>();
        _mockLogger.Setup(l => l.Log(It.IsAny<string>()))
            .Callback<string>(s => _logOutput.Add(s));

        _organizer = new FileOrganizer(_mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    private string _testDirectory = null!;
    private OrganizerConfig _config = null!;
    private Mock<IFileLogger> _mockLogger = null!;
    private FileOrganizer _organizer = null!;
    private List<string> _logOutput = null!;

    private void CreateTestFile(string fileName)
    {
        File.Create(Path.Combine(_testDirectory, fileName)).Close();
    }

    private void CreateTestDirectory(string dirName)
    {
        Directory.CreateDirectory(Path.Combine(_testDirectory, dirName));
    }

    [Test]
    public void Organize_MovesKnownFileToCorrectCategoryFolder()
    {
        CreateTestFile("my-image.jpg");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "my-image.jpg")), Is.False);
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Photos", "my-image.jpg")),
                Is.True);
        });
    }

    [Test]
    public void Organize_MovesUnknownFileToOthersFolder()
    {
        CreateTestFile("data.unknown");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "data.unknown")), Is.False);
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Others", "data.unknown")),
                Is.True);
        });
    }

    [Test]
    public void Organize_MovesSubfolderToFoldersDirectory()
    {
        CreateTestDirectory("my-stuff");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "my-stuff")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "Folders", "my-stuff")),
                Is.True);
        });
    }

    [Test]
    public void Organize_DoesNotMoveManagedCategoryFolders()
    {
        CreateTestFile("report.pdf");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "Documents")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "Folders", "Documents")),
                Is.False);
        });
    }

    [Test]
    public void Organize_HandlesFileNameCollisions()
    {
        Directory.CreateDirectory(Path.Combine(_testDirectory, "Documents"));
        File.Create(Path.Combine(_testDirectory, "Documents", "report.pdf")).Close();
        CreateTestFile("report.pdf");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Documents", "report.pdf")),
                Is.True);
            Assert.That(
                File.Exists(Path.Combine(_testDirectory, "Documents", "report (1).pdf")),
                Is.True);
        });
    }

    [Test]
    public void Organize_DryRun_DoesNotMoveFilesOrFolders()
    {
        CreateTestFile("archive.zip");
        CreateTestDirectory("some-dir");

        _organizer.Organize(_testDirectory, _config, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "archive.zip")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "some-dir")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_testDirectory, "Archives")), Is.False);
            Assert.That(_logOutput, Has.Some.Contain("[DRY RUN]"));
        });
    }

    [Test]
    public void Organize_Recursive_MovesFilesInSubdirectories()
    {
        CreateTestDirectory("nested");
        File.Create(Path.Combine(_testDirectory, "nested", "deep-video.mp4")).Close();

        _organizer.Organize(_testDirectory, _config, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "nested", "deep-video.mp4")),
                Is.False);
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Videos", "deep-video.mp4")),
                Is.True);
        });
    }

    [Test]
    public void Organize_Recursive_IgnoresFilesAlreadyInManagedFolders()
    {
        CreateTestDirectory("Photos");
        File.Create(Path.Combine(_testDirectory, "Photos", "existing.jpg")).Close();

        _organizer.Organize(_testDirectory, _config, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Photos", "existing.jpg")),
                Is.True);
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Photos", "existing (1).jpg")),
                Is.False);
        });
    }


    [Test]
    public void Organize_HandlesUpperCaseExtensionCorrectly()
    {
        CreateTestFile("vacation.JPG");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.That(File.Exists(Path.Combine(_testDirectory, "Photos", "vacation.JPG")), Is.True);
    }

    [Test]
    public void Organize_MovesFileWithNoExtensionToOthers()
    {
        CreateTestFile("LICENSE");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.That(File.Exists(Path.Combine(_testDirectory, "Others", "LICENSE")), Is.True);
    }

    [Test]
    public void Organize_IgnoresTheConfigFile()
    {
        CreateTestFile("config.json");

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_testDirectory, "config.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(_testDirectory, "Others", "config.json")),
                Is.False);
        });
    }

    [Test]
    public void Organize_LogsErrorAndReturns_WhenTargetDirectoryNotFound()
    {
        var nonExistentDirectory = Path.Combine(_testDirectory, "non-existent-dir");

        _organizer.Organize(nonExistentDirectory, _config, false, false);

        Assert.That(_logOutput, Has.Some.Contain("ERROR: Target directory not found"));
        Assert.That(_logOutput, Has.Count.EqualTo(1));
    }

    [Test]
    public void Organize_HandlesMultipleFileNameCollisions()
    {
        Directory.CreateDirectory(Path.Combine(_testDirectory, "Documents"));
        File.Create(Path.Combine(_testDirectory, "Documents", "report.pdf")).Close();
        File.Create(Path.Combine(_testDirectory, "Documents", "report (1).pdf")).Close();
        CreateTestFile("report.pdf"); // The new file to be moved.

        _organizer.Organize(_testDirectory, _config, false, false);

        Assert.That(File.Exists(Path.Combine(_testDirectory, "Documents", "report (2).pdf")),
            Is.True);
    }
}