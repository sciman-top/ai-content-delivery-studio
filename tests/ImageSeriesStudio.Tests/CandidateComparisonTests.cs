using ImageSeriesStudio.Core.Experiments;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Tests;

public sealed class CandidateComparisonTests
{
    [Fact]
    public void Compare_RanksPassingCandidateWithBestWeightedScoreFirst()
    {
        var first = CreateInput("A", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 3, 4), ("quality", 1, 5));
        var second = CreateInput("B", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 3, 5), ("quality", 1, 5));

        var comparison = CandidateComparison.Compare([first, second]);

        Assert.Equal(second.CandidateImageId, comparison.BestCandidate!.CandidateImageId);
        Assert.Equal(5, comparison.BestCandidate.WeightedScore);
    }

    [Fact]
    public void Compare_PenalizesCandidatesThatNeedRepair()
    {
        var cleanPass = CreateInput("Clean", ReviewDecision.Pass, hardFailures: [], suggestedFix: null, ("match", 1, 4));
        var suggestedFix = CreateInput("Fix", ReviewDecision.Pass, hardFailures: [], "Improve composition.", ("match", 1, 5));
        var failed = CreateInput("Fail", ReviewDecision.Fail, hardFailures: [], suggestedFix: null, ("match", 1, 5));

        var rows = CandidateComparison.Compare([failed, suggestedFix, cleanPass]).Rows;

        Assert.Equal(cleanPass.CandidateImageId, rows[0].CandidateImageId);
        Assert.Equal(suggestedFix.CandidateImageId, rows[1].CandidateImageId);
        Assert.Equal(failed.CandidateImageId, rows[2].CandidateImageId);
    }

    [Fact]
    public void Compare_UsesHardFailureCountBeforeScore()
    {
        var fewerHardFailures = CreateInput("One failure", ReviewDecision.Fail, ["bad-text"], null, ("match", 1, 1));
        var moreHardFailures = CreateInput("Two failures", ReviewDecision.Fail, ["bad-text", "unsafe"], null, ("match", 1, 5));

        var rows = CandidateComparison.Compare([moreHardFailures, fewerHardFailures]).Rows;

        Assert.Equal(fewerHardFailures.CandidateImageId, rows[0].CandidateImageId);
        Assert.Equal(1, rows[0].HardFailureCount);
    }

    [Fact]
    public void Compare_ReturnsNullBestCandidateForEmptyList()
    {
        var comparison = CandidateComparison.Compare([]);

        Assert.Empty(comparison.Rows);
        Assert.Null(comparison.BestCandidate);
    }

    [Fact]
    public void Compare_CarriesExperimentVariantMetadataIntoRows()
    {
        var recipe = GenerationRecipe.Create(
            Guid.NewGuid(),
            "fake-image-v1",
            ImageTypePresetCatalog.ArticleCover,
            1024,
            1024,
            "standard",
            "png",
            ImageBackgroundMode.Auto,
            compression: null,
            ImageModerationMode.Auto,
            seed: 11,
            []);
        var variant = ParameterGridExperiment.CreateVariants(
            "A {{lighting}} article cover.",
            new GenerationSettings(1024, 1024, "standard", "png", 11),
            [new ParameterGridAxis("lighting", ["soft rim"])],
            recipe)
            .Single()
            .WithGenerationTask(Guid.NewGuid());
        var input = CreateInput(
            "Variant",
            ReviewDecision.Pass,
            hardFailures: [],
            suggestedFix: null,
            variant,
            ("match", 1, 5));

        var row = Assert.Single(CandidateComparison.Compare([input]).Rows);

        Assert.Equal("001-lighting-soft-rim", row.ExperimentSlug);
        Assert.Equal("soft rim", row.ExperimentParameters["lighting"]);
        Assert.Equal(variant.GenerationTaskId, row.GenerationTaskId);
        Assert.Equal(recipe.Id, row.RecipeId);
    }

    private static CandidateComparisonInput CreateInput(
        string title,
        ReviewDecision decision,
        IReadOnlyList<string> hardFailures,
        string? suggestedFix,
        params (string Name, int Weight, int Score)[] scores)
    {
        return CreateInput(title, decision, hardFailures, suggestedFix, experimentVariant: null, scores);
    }

    private static CandidateComparisonInput CreateInput(
        string title,
        ReviewDecision decision,
        IReadOnlyList<string> hardFailures,
        string? suggestedFix,
        ParameterGridVariant? experimentVariant,
        params (string Name, int Weight, int Score)[] scores)
    {
        var candidateId = Guid.NewGuid();
        return new CandidateComparisonInput(
            candidateId,
            title,
            $"{title}.png",
            new StructuredReviewOutput(
                candidateId,
                decision,
                scores
                    .Select(score => new StructuredReviewScore(score.Name, $"{score.Name} requirement", score.Weight, score.Score))
                    .ToArray(),
                hardFailures,
                $"{title} comments",
                suggestedFix),
            experimentVariant);
    }
}
