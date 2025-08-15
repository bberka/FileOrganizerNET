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

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "report.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Backup", "report.pdf")),
                Is.True);
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

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.That(File.Exists(Path.Combine(TestDirectory, "temp.tmp")), Is.False);
    }

    [Test]
    public void Organize_MovesFile_WhenActionIsNotSpecified()
    {
        CreateTestFile("document.docx");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Documents",
                    Conditions = new RuleConditions { Extensions = [".docx"] }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "document.docx")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Documents", "document.docx")),
                Is.True);
        });
    }

    [Test]
    public void Organize_DryRun_LogsCorrectActionVerbs()
    {
        CreateTestFile("report.pdf"); // copy
        CreateTestFile("image.jpg"); // move
        CreateTestFile("old.log"); // delete
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

        Organizer.Organize(TestDirectory, config, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(LogOutput.Any(s =>
                s.Contains("Would COPY file") && s.Contains("report.pdf")));
            Assert.That(
                LogOutput.Any(s => s.Contains("Would MOVE file") && s.Contains("image.jpg")));
            Assert.That(
                LogOutput.Any(s => s.Contains("Would DELETE file") && s.Contains("old.log")));
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

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "document.docx")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Documents", "document.docx")),
                Is.True);
            Assert.That(LogOutput,
                Has.Some.Contain("Moving file:").And.Contain("document.docx").And
                    .Contain("-> Documents"));
        });
    }

    [Test]
    public void Organize_DryRun_LogsCorrectActionVerbsForVariousActions()
    {
        CreateTestFile("report.pdf"); // Expected: Copy
        CreateTestFile("image.jpg"); // Expected: Move
        CreateTestFile("old.log"); // Expected: Delete
        CreateTestFile("misc.txt"); // Expected: Move to Others (default)

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

        Organizer.Organize(TestDirectory, config, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(LogOutput,
                Has.Some.Contain("[DRY RUN] Would COPY file").And.Contains("report.pdf").And
                    .Contains("-> Backup"));
            Assert.That(LogOutput,
                Has.Some.Contain("[DRY RUN] Would MOVE file").And.Contains("image.jpg").And
                    .Contains("-> Images"));
            Assert.That(LogOutput,
                Has.Some.Contain("[DRY RUN] Would DELETE file").And.Contains("old.log"));
            Assert.That(LogOutput,
                Has.Some.Contain("[DRY RUN] Would MOVE file").And.Contains("misc.txt").And
                    .Contains("-> Others"));
        });
    }
}