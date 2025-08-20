using FileOrganizerNET.Models.Config;
using Moq;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class DuplicateCheckTests : FileOrganizerTestsBase
{
    [SetUp]
    public new void SetUp()
    {
        base.SetUp();

        _baseConfig = new OrganizerConfig
        {
            Rules =
            [
                new Rule { DestinationFolder = "Photos", Conditions = new RuleConditions { Extensions = [".jpg"] } },
                new Rule { DestinationFolder = "Documents", Conditions = new RuleConditions { Extensions = [".pdf"] } }
            ],
            OthersFolderName = "Others",
            SubfoldersFolderName = "Folders"
        };
    }

    private OrganizerConfig _baseConfig = null!;

    [Test]
    public void Organize_DeletesDuplicateFiles_WhenCheckDuplicatesFlagIsEnabled()
    {
        CreateTestDirectory("Photos");
        CreateTestDirectory("Documents");

        var file1Path = Path.Combine(TestDirectory, "Photos", "image_a.jpg");
        var file2Path = Path.Combine(TestDirectory, "Documents", "image_b.jpg");
        var file3Path = Path.Combine(TestDirectory, "Photos", "image_c.jpg");
        CreateTestFileWithContent(file1Path, "This is unique content 1");
        CreateTestFileWithContent(file2Path, "This is unique content 1");
        CreateTestFileWithContent(file3Path, "This is unique content 1");

        var file4Path = Path.Combine(TestDirectory, "Photos", "unique_image.jpg");
        CreateTestFileWithContent(file4Path, "This is unique content 2");

        var result = Organizer.Organize(TestDirectory, _baseConfig, false, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicateCheckOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DuplicateCheckOutcome!.Errors, Is.Empty);
            Assert.That(result.DuplicateCheckOutcome.FilesHashed, Is.EqualTo(4)); // 3 duplicates + 1 unique
            Assert.That(result.DuplicateCheckOutcome.DuplicateSetsFound, Is.EqualTo(1));
            Assert.That(result.DuplicateCheckOutcome.DuplicateFilesDeleted, Is.EqualTo(2));

            Assert.That(File.Exists(file1Path), Is.True, "Original file (image_a.jpg) should remain.");
            Assert.That(File.Exists(file2Path), Is.False, "Duplicate file (image_b.jpg) should be deleted.");
            Assert.That(File.Exists(file3Path), Is.False, "Duplicate file (image_c.jpg) should be deleted.");
            Assert.That(File.Exists(file4Path), Is.True, "Unique file should remain.");
        });
    }

    [Test]
    public void Organize_DoesNotDeleteDuplicates_WhenCheckDuplicatesFlagIsDisabled()
    {
        CreateTestDirectory("Photos");
        CreateTestDirectory("Documents");
        var file1Path = Path.Combine(TestDirectory, "Photos", "image_a.jpg");
        var file2Path = Path.Combine(TestDirectory, "Documents", "image_b.jpg");
        CreateTestFileWithContent(file1Path, "Duplicate content");
        CreateTestFileWithContent(file2Path, "Duplicate content");

        var result = Organizer.Organize(TestDirectory, _baseConfig);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicateCheckOutcome, Is.Null, "DuplicateCheckOutcome should be null if feature is disabled.");

            Assert.That(File.Exists(file1Path), Is.True);
            Assert.That(File.Exists(file2Path), Is.True);
        });
    }

    [Test]
    public void Organize_DryRun_LogsDuplicateDeletionWithoutActualDeletion_WhenCheckDuplicatesFlagIsEnabled()
    {
        CreateTestDirectory("Photos");
        var file1Path = Path.Combine(TestDirectory, "Photos", "image_a.jpg");
        var file2Path = Path.Combine(TestDirectory, "Photos", "image_b.jpg");
        CreateTestFileWithContent(file1Path, "Duplicate content");
        CreateTestFileWithContent(file2Path, "Duplicate content");

        var result = Organizer.Organize(TestDirectory, _baseConfig, false, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True); // Dry run is always successful
            Assert.That(result.DuplicateCheckOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DuplicateCheckOutcome!.Errors, Is.Empty);
            Assert.That(result.DuplicateCheckOutcome.DuplicateSetsFound, Is.EqualTo(1));
            Assert.That(result.DuplicateCheckOutcome.DuplicateFilesDeleted, Is.EqualTo(0)); // Crucially, no actual deletions

            Assert.That(File.Exists(file1Path), Is.True); // Files should still exist
            Assert.That(File.Exists(file2Path), Is.True);
        });

        // Verify that the dry run log message for deletion was recorded.
        MockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("[DRY RUN] Would DELETE duplicate:") && s.Contains("image_b.jpg"))), Times.Once);
    }

    [Test]
    public void Organize_HandlesDifferentContentSameName_NoDeletion_WhenCheckDuplicatesFlagIsEnabled()
    {
        CreateTestDirectory("Photos");
        var file1Path = Path.Combine(TestDirectory, "Photos", "same_name.jpg");
        var file2Path = Path.Combine(TestDirectory, "Photos", "same_name (1).jpg");
        CreateTestFileWithContent(file1Path, "Content A");
        CreateTestFileWithContent(file2Path, "Content B");

        var result = Organizer.Organize(TestDirectory, _baseConfig, false, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicateCheckOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DuplicateCheckOutcome!.DuplicateSetsFound, Is.EqualTo(0)); // No duplicate sets expected
            Assert.That(result.DuplicateCheckOutcome.DuplicateFilesDeleted, Is.EqualTo(0));

            Assert.That(File.Exists(file1Path), Is.True);
            Assert.That(File.Exists(file2Path), Is.True);
        });
    }

    [Test]
    public void Organize_HandlesEmptyManagedFolders_WhenCheckDuplicatesFlagIsEnabled()
    {
        // No files are created, so managed folders are effectively empty.
        var result = Organizer.Organize(TestDirectory, _baseConfig, false, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicateCheckOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DuplicateCheckOutcome!.FilesHashed, Is.EqualTo(0));
            Assert.That(result.DuplicateCheckOutcome.DuplicateSetsFound, Is.EqualTo(0));
            Assert.That(result.DuplicateCheckOutcome.DuplicateFilesDeleted, Is.EqualTo(0));
            Assert.That(result.DuplicateCheckOutcome.Errors, Is.Empty);
        });
    }
}