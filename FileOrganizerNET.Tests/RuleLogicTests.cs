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
                    Conditions = new RuleConditions
                        { FileNameContains = ["invoice"] }
                },

                new Rule
                {
                    DestinationFolder = "Documents",
                    Conditions = new RuleConditions { Extensions = [".pdf"] }
                }
            ]
        };

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Invoices", "invoice.pdf")),
                Is.True);
            Assert.That(Directory.Exists(Path.Combine(TestDirectory, "Documents")), Is.False);
        });
    }

    [Test]
    public void Organize_RequiresAllConditionsInARuleToMatch()
    {
        CreateTestFile("invoice.pdf"); // Matches both conditions
        CreateTestFile("invoice.txt"); // Matches filename, but not extension
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

        Organizer.Organize(TestDirectory, config, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Financial Docs", "invoice.pdf")),
                Is.True);
            Assert.That(File.Exists(Path.Combine(TestDirectory, "Others", "invoice.txt")),
                Is.True);
        });
    }
}