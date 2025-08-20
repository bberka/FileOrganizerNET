using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class FileOrganizerGeneralTests : FileOrganizerTestsBase
{
    [Test]
    public void Organize_HandlesFileNameCollisions()
    {
        // Arrange
        CreateTestDirectory("Documents");
        CreateTestFileWithContent(Path.Combine("Documents", "report.pdf"), "Original content"); // Existing file
        CreateTestFileWithContent("report.pdf", "New content"); // File to be moved, causing collision

        // OVERRIDE MOCK BEHAVIOR FOR THIS TEST:
        // When GetUniqueFilePath is called for 'report.pdf' into 'Documents', simulate a collision
        // and return the renamed path.
        MockFileSystemActions.Setup(fsa => fsa.GetUniqueFilePath(
                Path.Combine(TestDirectory, "Documents", "report.pdf")))
            .Returns(Path.Combine(TestDirectory, "Documents", "report (1).pdf"));


        // Act
        var result = Organizer.Organize(TestDirectory, DefaultConfig);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(1));
        });

        var processedAction = result.FileProcessingOutcome.ActionsTaken.Single();
        Assert.Multiple(() =>
        {
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "report.pdf")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            // This is now expected to be the (1) path because we mocked GetUniqueFilePath to return it.
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Documents", "report (1).pdf")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "Documents", "report.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Documents", "report (1).pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "report.pdf")), Is.False);
        });
    }

    [Test]
    public void Organize_DoesNotMoveManagedCategoryFolders()
    {
        CreateTestFile("report.pdf"); // This will create the "Documents" folder.

        var result = Organizer.Organize(TestDirectory, DefaultConfig);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FolderProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FolderProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FolderProcessingOutcome.FoldersScanned,
                Is.EqualTo(1)); // Only "Documents" folder would be scanned if no other custom folders.
            Assert.That(result.FolderProcessingOutcome.FoldersMoved, Is.EqualTo(0)); // No managed folders should be moved.

            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Documents")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Folders", "Documents")), Is.False);
        });
    }

    [Test]
    public void Organize_MovesSubfolderToFoldersDirectory()
    {
        CreateTestDirectory("my-stuff");

        var result = Organizer.Organize(TestDirectory, DefaultConfig);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FolderProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FolderProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FolderProcessingOutcome.FoldersScanned, Is.EqualTo(1));
            Assert.That(result.FolderProcessingOutcome.FoldersMoved, Is.EqualTo(1));

            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "my-stuff")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Folders", "my-stuff")), Is.True);
        });
    }

    [Test]
    public void Organize_Recursive_MovesFilesInSubdirectories()
    {
        CreateTestDirectory("nested");
        CreateTestFile(Path.Combine("nested", "deep-video.mp4"));

        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule { DestinationFolder = "Videos", Conditions = new RuleConditions { Extensions = [".mp4"] } }
            ],
            OthersFolderName = "Others",
            SubfoldersFolderName = "Folders"
        };

        var result = Organizer.Organize(TestDirectory, config, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(1));
        });

        var processedAction = result.FileProcessingOutcome.ActionsTaken.Single();
        Assert.Multiple(() =>
        {
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "nested", "deep-video.mp4")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Videos", "deep-video.mp4")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "nested", "deep-video.mp4")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Videos", "deep-video.mp4")), Is.True);
        });
    }

    [Test]
    public void Organize_Recursive_IgnoresFilesAlreadyInManagedFolders()
    {
        CreateTestDirectory("Photos");
        CreateTestFile(Path.Combine("Photos", "existing.jpg"));

        var result = Organizer.Organize(TestDirectory, DefaultConfig, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(0)); // File in Photos should be skipped from scanning
            Assert.That(result.FileProcessingOutcome.ActionsTaken, Is.Empty); // No actions should be taken

            Assert.That(File.Exists(Path.Combine(TestDirectory, "Photos", "existing.jpg")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Photos", "existing (1).jpg")), Is.False);
        });
    }
}