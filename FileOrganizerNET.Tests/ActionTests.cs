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
}