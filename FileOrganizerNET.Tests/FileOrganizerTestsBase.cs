using Moq;

namespace FileOrganizerNET.Tests;

/// <summary>
///     Provides common setup, teardown, and helper methods for all test fixtures.
/// </summary>
public abstract class FileOrganizerTestsBase
{
    protected List<string> LogOutput = null!;
    protected Mock<IFileLogger> MockLogger = null!;
    protected FileOrganizer Organizer = null!;
    protected string TestDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        TestDirectory = Path.Combine(Path.GetTempPath(), "FileOrganizerTests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(TestDirectory);

        LogOutput = new List<string>();
        MockLogger = new Mock<IFileLogger>();
        MockLogger.Setup(l => l.Log(It.IsAny<string>()))
            .Callback<string>(s => LogOutput.Add(s));

        Organizer = new FileOrganizer(MockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(TestDirectory)) Directory.Delete(TestDirectory, true);
    }

    protected void CreateTestFile(string fileName, long sizeBytes = 1024)
    {
        var path = Path.Combine(TestDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.SetLength(sizeBytes);
    }

    protected void CreateTestFileWithDate(string fileName, DateTime lastWriteTimeUtc)
    {
        var path = Path.Combine(TestDirectory, fileName);
        File.Create(path).Close();
        File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
    }

    protected void CreateTestDirectory(string dirName)
    {
        Directory.CreateDirectory(Path.Combine(TestDirectory, dirName));
    }
}