using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class ActionTests : FileOrganizerTestsBase
{
    [Test]
    public void Organize_CopiesFile_WhenActionIsCopy()
    {
        CreateTestFile("report.pdf");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    Action = RuleAction.Copy,
                    DestinationFolder = "Backup",
                    Conditions = new RuleConditions { Extensions = [".pdf"] }
                }
            ]
        };

        var result = Organizer.Organize(TestDirectory, config);

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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "report.pdf")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Copy));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Backup", "report.pdf")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "report.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Backup", "report.pdf")), Is.True);
        });
    }

    [Test]
    public void Organize_DeletesFile_WhenActionIsDelete()
    {
        CreateTestFile("temp.tmp");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    Action = RuleAction.Delete,
                    Conditions = new RuleConditions { Extensions = [".tmp"] }
                }
            ]
        };

        var result = Organizer.Organize(TestDirectory, config);

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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "temp.tmp")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Delete));
            Assert.That(processedAction.IsSuccess, Is.True);

            Assert.That(File.Exists(Path.Combine(TestDirectory, "temp.tmp")), Is.False);
        });
    }

    [Test]
    public void Organize_MovesFile_WhenActionIsNotSpecifiedInRule()
    {
        CreateTestFile("document.docx");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Documents", // Action defaults to Move
                    Conditions = new RuleConditions { Extensions = [".docx"] }
                }
            ]
        };

        var result = Organizer.Organize(TestDirectory, config);

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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "document.docx")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Documents", "document.docx")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "document.docx")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Documents", "document.docx")), Is.True);
        });
    }


    [Test]
    public void Organize_DryRun_LogsCorrectActionVerbsForVariousActions()
    {
        CreateTestFile("report.pdf"); // copy
        CreateTestFile("image.jpg"); // move
        CreateTestFile("old.log"); // delete
        CreateTestFile("misc.txt"); // move to Others by default

        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    Action = RuleAction.Copy, DestinationFolder = "Backup",
                    Conditions = new RuleConditions { Extensions = [".pdf"] }
                },
                new Rule
                {
                    Action = RuleAction.Move, DestinationFolder = "Images",
                    Conditions = new RuleConditions { Extensions = [".jpg"] }
                },
                new Rule
                {
                    Action = RuleAction.Delete,
                    Conditions = new RuleConditions { Extensions = [".log"] }
                }
            ]
        };

        var result = Organizer.Organize(TestDirectory, config, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(4));
        });

        Assert.Multiple(() =>
        {
            var copyAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("report.pdf"));
            Assert.That(copyAction.Action, Is.EqualTo(RuleAction.Copy));
            Assert.That(copyAction.IsSuccess, Is.True);
            Assert.That(copyAction.ResultMessage,
                Does.Contain("[DRY RUN] Would COPY file").And.Contains("report.pdf").And.Contain($"-> \"{Path.Combine(TestDirectory, "Backup")}"));

            var moveAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("image.jpg"));
            Assert.That(moveAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(moveAction.IsSuccess, Is.True);
            Assert.That(moveAction.ResultMessage,
                Does.Contain("[DRY RUN] Would MOVE file").And.Contains("image.jpg").And.Contains($"-> \"{Path.Combine(TestDirectory, "Images")}"));

            var deleteAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("old.log"));
            Assert.That(deleteAction.Action, Is.EqualTo(RuleAction.Delete));
            Assert.That(deleteAction.IsSuccess, Is.True);
            Assert.That(deleteAction.ResultMessage, Does.Contain("[DRY RUN] Would DELETE file").And.Contains("old.log"));
            var othersAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("misc.txt"));
            Assert.That(othersAction.Action, Is.EqualTo(RuleAction.Move)); // Unmatched files move to Others
            Assert.That(othersAction.IsSuccess, Is.True);
            Assert.That(othersAction.ResultMessage,
                Does.Contain("[DRY RUN] Would MOVE file").And.Contains("misc.txt").And.Contains($"-> \"{Path.Combine(TestDirectory, "Others")}"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "report.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "image.jpg")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "old.log")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "misc.txt")), Is.True);

            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Backup")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Images")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Others")), Is.False);
        });
    }
}