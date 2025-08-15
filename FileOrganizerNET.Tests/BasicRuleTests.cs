namespace FileOrganizerNET.Tests;

[TestFixture]
public class BasicRuleTests : FileOrganizerTestsBase
{
    [Test]
    public void Organize_MovesFileToFolder_BasedOnExtensionRule()
    {
        CreateTestFile("my-image.jpg");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Photos",
                    Conditions = new RuleConditions { Extensions = [".jpg"] }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.That(File.Exists(Path.Combine(TestDirectory, "Photos", "my-image.jpg")), Is.True);
    }

    [Test]
    public void Organize_MovesUnmatchedFileToOthersFolder()
    {
        CreateTestFile("data.unknown");
        var config = new OrganizerConfig { Rules = [] }; // No rules defined

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "data.unknown")), Is.True);
    }

    [Test]
    public void Organize_MovesFileToFolder_BasedOnFileNameContainsRule()
    {
        CreateTestFile("invoice-december.pdf");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Invoices",
                    Conditions = new RuleConditions { FileNameContains = ["invoice"] }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.That(File.Exists(Path.Combine(TestDirectory, "Invoices", "invoice-december.pdf")),
            Is.True);
    }

    [Test]
    public void Organize_MovesFileToFolder_BasedOnOlderThanDaysRule()
    {
        CreateTestFileWithDate("old-report.log", DateTime.UtcNow.AddDays(-400));
        CreateTestFileWithDate("new-report.log", DateTime.UtcNow.AddDays(-10));
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Archive",
                    Conditions = new RuleConditions { OlderThanDays = 365 }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Archive", "old-report.log")),
                Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "new-report.log")),
                Is.True);
        });
    }

    [Test]
    public void Organize_MovesFileToFolder_BasedOnMinSizeRule()
    {
        CreateTestFile("large-video.mkv", 500 * 1024 * 1024); // 500 MB
        CreateTestFile("small-script.py"); // 1 KB
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Large Files",
                    Conditions = new RuleConditions { MinSizeMb = 100 }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Large Files", "large-video.mkv")),
                Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "small-script.py")),
                Is.True);
        });
    }
}