using FileOrganizerNET.Models.Config;

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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "my-image.jpg")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Photos", "my-image.jpg")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "my-image.jpg")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Photos", "my-image.jpg")), Is.True);
        });
    }

    [Test]
    public void Organize_MovesUnmatchedFileToOthersFolder()
    {
        CreateTestFile("data.unknown");
        var config = new OrganizerConfig { Rules = [] };

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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "data.unknown")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Others", "data.unknown")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "data.unknown")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "data.unknown")), Is.True);
        });
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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "invoice-december.pdf")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Invoices", "invoice-december.pdf")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "Invoices", "invoice-december.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "invoice-december.pdf")), Is.False);
        });
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

        var result = Organizer.Organize(TestDirectory, config);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(2));
        });

        var oldReportAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("old-report.log"));
        Assert.Multiple(() =>
        {
            Assert.That(oldReportAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(oldReportAction.IsSuccess, Is.True);
            Assert.That(oldReportAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Archive", "old-report.log")));
        });

        var newReportAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("new-report.log"));
        Assert.Multiple(() =>
        {
            Assert.That(newReportAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(newReportAction.IsSuccess, Is.True);
            Assert.That(newReportAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Others", "new-report.log")));
        });

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Archive", "old-report.log")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "new-report.log")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "old-report.log")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "new-report.log")), Is.False);
        });
    }

    [Test]
    public void Organize_MovesFileToFolder_BasedOnMinSizeRule()
    {
        CreateTestFile("large-video.mkv", 500 * 1024 * 1024);
        CreateTestFile("small-script.py");
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

        var result = Organizer.Organize(TestDirectory, config);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileProcessingOutcome, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.FileProcessingOutcome!.Errors, Is.Empty);
            Assert.That(result.FileProcessingOutcome.FilesScanned, Is.EqualTo(2));
        });

        var largeVideoAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("large-video.mkv"));
        Assert.Multiple(() =>
        {
            Assert.That(largeVideoAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(largeVideoAction.IsSuccess, Is.True);
            Assert.That(largeVideoAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Large Files", "large-video.mkv")));
        });

        var smallScriptAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("small-script.py"));
        Assert.Multiple(() =>
        {
            Assert.That(smallScriptAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(smallScriptAction.IsSuccess, Is.True);
            Assert.That(smallScriptAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Others", "small-script.py")));
        });

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Large Files", "large-video.mkv")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "small-script.py")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "large-video.mkv")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "small-script.py")), Is.False);
        });
    }
}