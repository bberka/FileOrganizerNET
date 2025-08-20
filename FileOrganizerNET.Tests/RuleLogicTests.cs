using FileOrganizerNET.Models.Config;

namespace FileOrganizerNET.Tests;

[TestFixture]
public class RuleLogicTests : FileOrganizerTestsBase
{
    [Test]
    public void Organize_RespectsRuleOrder_FirstMatchWins()
    {
        CreateTestFile("invoice.pdf");
        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Invoices",
                    Conditions = new RuleConditions { FileNameContains = ["invoice"] }
                },
                new Rule
                {
                    DestinationFolder = "Documents",
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
            Assert.That(processedAction.OriginalFilePath, Is.EqualTo(Path.Combine(TestDirectory, "invoice.pdf")));
            Assert.That(processedAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(processedAction.IsSuccess, Is.True);
            Assert.That(processedAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Invoices", "invoice.pdf")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "Invoices", "invoice.pdf")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Documents")), Is.False);
        });
    }

    [Test]
    public void Organize_RequiresAllConditionsInARuleToMatch()
    {
        CreateTestFile("invoice.pdf"); // Matches both conditions in rule
        CreateTestFile("invoice.txt"); // Matches filename, but not extension (should go to Others)

        var config = new OrganizerConfig
        {
            Rules =
            [
                new Rule
                {
                    DestinationFolder = "Financial Docs",
                    Conditions = new RuleConditions
                    {
                        FileNameContains = ["invoice"],
                        Extensions = [".pdf"]
                    }
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

        var pdfAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("invoice.pdf"));
        Assert.Multiple(() =>
        {
            Assert.That(pdfAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(pdfAction.IsSuccess, Is.True);
            Assert.That(pdfAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Financial Docs", "invoice.pdf")));
        });

        var txtAction = result.FileProcessingOutcome.ActionsTaken.Single(a => a.OriginalFilePath.Contains("invoice.txt"));
        Assert.Multiple(() =>
        {
            Assert.That(txtAction.Action, Is.EqualTo(RuleAction.Move));
            Assert.That(txtAction.IsSuccess, Is.True);
            Assert.That(txtAction.DestinationPath, Is.EqualTo(Path.Combine(TestDirectory, "Others", "invoice.txt")));

            Assert.That(File.Exists(Path.Combine(TestDirectory, "Financial Docs", "invoice.pdf")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "invoice.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "invoice.pdf")), Is.False);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "invoice.txt")), Is.False);
        });
    }
}