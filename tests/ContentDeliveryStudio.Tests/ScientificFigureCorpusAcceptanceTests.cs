using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

[Trait("Category", "ReleaseOnly")]
public sealed class ScientificFigureCorpusAcceptanceTests
{
    [Fact]
    public async Task AcceptedCorpus_ReachesReviewPassedAndBlocksEveryMutation()
    {
        var report = await RunCorpusAsync();

        Assert.True(report.Passed);
        Assert.Equal("scientific-figures-v1", report.CorpusId);
        Assert.Equal(12, report.Items.Count);
        Assert.Equal(4, report.Items.Count(item => item.Category == "mechanism-process"));
        Assert.Equal(4, report.Items.Count(item => item.Category == "concept-comparison"));
        Assert.Equal(4, report.Items.Count(item => item.Category == "graphical-abstract"));
        Assert.All(report.Items, item =>
        {
            Assert.Equal(ScientificFigureWorkflowState.ReviewPassed, item.WorkflowState);
            Assert.True(item.ContractReviewPassed);
            Assert.True(item.MachineReviewPassed);
            Assert.True(item.Coverage.AllMapped);
            Assert.NotEmpty(item.Mutations);
            Assert.All(item.Mutations, mutation =>
            {
                Assert.Equal("block", mutation.ExpectedOutcome);
                Assert.Equal("blocked", mutation.ActualOutcome);
                Assert.False(string.IsNullOrWhiteSpace(mutation.FindingCode));
                Assert.False(string.IsNullOrWhiteSpace(mutation.ResponsibleItemId));
            });
        });
        Assert.Equal(
            report.Items.Sum(item => item.Mutations.Count),
            report.BlockedMutationCount);
    }

    [Fact]
    public async Task Report_IsDeterministicAndWritesConfiguredCommandArtifact()
    {
        var first = await RunCorpusAsync();
        var second = await RunCorpusAsync();

        Assert.Equal(first.ToJson(), second.ToJson());

        var configuredPath = Environment.GetEnvironmentVariable(
            "SCIENTIFIC_FIGURE_CORPUS_REPORT_PATH");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return;
        }

        ScientificFigureCorpusRunner.WriteReport(first, configuredPath);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configuredPath));
        Assert.True(document.RootElement.GetProperty("passed").GetBoolean());
        Assert.Equal(12, document.RootElement.GetProperty("itemCount").GetInt32());
    }

    private static Task<ScientificFigureCorpusAcceptanceReport> RunCorpusAsync()
    {
        var runner = new ScientificFigureCorpusRunner(
            new DeterministicSvgRenderer(),
            new ScientificFigureExporter(),
            new SkiaScientificReviewImageCropper());
        return runner.RunAsync(
            Path.Combine(FindRepositoryRoot(), "eval", "scientific-figures", "corpus.json"),
            CancellationToken.None);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find ContentDeliveryStudio.sln from the test output path.");
    }
}
