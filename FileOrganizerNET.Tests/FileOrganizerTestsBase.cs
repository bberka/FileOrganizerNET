// FileOrganizerNET.Tests/FileOrganizerTestsBase.cs

using FileOrganizerNET.Concrete;
using FileOrganizerNET.Contracts;
using FileOrganizerNET.Models.Config;
using FileOrganizerNET.Models.Result;
using FileOrganizerNET.Utils;
using Moq;

namespace FileOrganizerNET.Tests;

public abstract class FileOrganizerTestsBase
{
    protected OrganizerConfig DefaultConfig = null!;
    protected List<string> LogOutput = null!;
    protected Mock<FileSystemActions> MockFileSystemActions = null!;
    protected Mock<IFileLogger> MockLogger = null!;
    protected IFileOrganizer Organizer = null!;
    protected string TestDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        TestDirectory = Path.Combine(Path.GetTempPath(), "FileOrganizerTests", Path.GetRandomFileName());
        Directory.CreateDirectory(TestDirectory);

        LogOutput = [];
        MockLogger = new Mock<IFileLogger>();
        MockLogger.Setup(l => l.Log(It.IsAny<string>()))
            .Callback<string>(s => LogOutput.Add(s));

        MockFileSystemActions = new Mock<FileSystemActions>(MockLogger.Object);

        // Default behavior for GetUniqueFilePath: just return the path (no collision)
        MockFileSystemActions.Setup(fsa => fsa.GetUniqueFilePath(It.IsAny<string>()))
            .Returns((string path) => path);

        // --- CRITICAL CORRECTION TO MoveFile MOCK ---
        // The MoveFile mock needs to call GetUniqueFilePath from the MockFileSystemActions
        // itself to simulate realistic collision handling.
        MockFileSystemActions.Setup(fsa => fsa.MoveFile(It.IsAny<FileInfo>(), It.IsAny<string>()))
            .Returns((FileInfo file, string destPath) =>
            {
                var originalFullName = file.FullName; // Capture original path

                // Call the MOCKED GetUniqueFilePath from the Mocked object
                // This ensures that if GetUniqueFilePath is overridden in a test,
                // this mock uses that overridden behavior.
                var uniqueDestFilePath = MockFileSystemActions.Object.GetUniqueFilePath(Path.Combine(destPath, file.Name));

                if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                file.MoveTo(uniqueDestFilePath, true); // Use the unique path for actual file move

                return new ProcessedFileAction
                {
                    OriginalFilePath = originalFullName,
                    Action = RuleAction.Move,
                    DestinationPath = uniqueDestFilePath, // Report the unique path
                    IsSuccess = true,
                    ResultMessage = $"Simulated move from {originalFullName} to {uniqueDestFilePath}"
                };
            });

        // Similar correction needed for CopyFile mock if it also calls GetUniqueFilePath internally
        MockFileSystemActions.Setup(fsa => fsa.CopyFile(It.IsAny<FileInfo>(), It.IsAny<string>()))
            .Returns((FileInfo file, string destPath) =>
            {
                var originalFullName = file.FullName;
                var uniqueDestFilePath =
                    MockFileSystemActions.Object.GetUniqueFilePath(Path.Combine(destPath, file.Name)); // Call mocked GetUniqueFilePath
                if (!Directory.Exists(destPath)) Directory.CreateDirectory(destPath);
                File.Copy(file.FullName, uniqueDestFilePath, true);
                return new ProcessedFileAction
                {
                    OriginalFilePath = originalFullName,
                    Action = RuleAction.Copy,
                    DestinationPath = uniqueDestFilePath,
                    IsSuccess = true,
                    ResultMessage = $"Simulated copy from {originalFullName} to {uniqueDestFilePath}"
                };
            });

        MockFileSystemActions.Setup(fsa => fsa.DeleteFile(It.IsAny<FileInfo>()))
            .Returns((FileInfo file) =>
            {
                var originalFullName = file.FullName;
                file.Delete();
                return new ProcessedFileAction
                {
                    OriginalFilePath = originalFullName,
                    Action = RuleAction.Delete,
                    IsSuccess = true,
                    ResultMessage = $"Simulated delete of {originalFullName}"
                };
            });

        // The GetXxHash128 mock doesn't need to change as it doesn't call other mocked methods internally for its result.
        MockFileSystemActions.Setup(fsa => fsa.GetXxHash128(It.IsAny<string>()))
            .Returns((string filePath) =>
            {
                var content = File.ReadAllText(filePath);
                return content.GetHashCode().ToString();
            });


        Organizer = new FileOrganizer(MockLogger.Object, MockFileSystemActions.Object);

        DefaultConfig = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    Action = RuleAction.Move, DestinationFolder = "Photos", Conditions = new RuleConditions { Extensions = [".jpg", ".jpeg", ".png"] }
                },
                new Rule
                {
                    Action = RuleAction.Move, DestinationFolder = "Documents", Conditions = new RuleConditions { Extensions = [".pdf", ".docx"] }
                },
                new Rule
                {
                    Action = RuleAction.Copy, DestinationFolder = "Backups", Conditions = new RuleConditions { FileNameContains = ["report"] }
                },
                new Rule { Action = RuleAction.Delete, Conditions = new RuleConditions { Extensions = [".tmp", ".log"] } },
                new Rule
                {
                    Action = RuleAction.Move, DestinationFolder = "Archive/Old Files", Conditions = new RuleConditions { OlderThanDays = 365 }
                },
                new Rule { Action = RuleAction.Move, DestinationFolder = "Large Files", Conditions = new RuleConditions { MinSizeMb = 100 } }
            ],
            OthersFolderName = "Others",
            SubfoldersFolderName = "Folders"
        };
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

    protected void CreateTestFileWithContent(string fileName, string content)
    {
        var path = Path.Combine(TestDirectory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
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