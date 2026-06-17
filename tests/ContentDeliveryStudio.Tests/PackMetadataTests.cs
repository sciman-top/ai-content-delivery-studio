using ContentDeliveryStudio.Core.Packs;

namespace ContentDeliveryStudio.Tests;

public sealed class PackMetadataTests
{
    [Fact]
    public void PackMetadataRecords_NormalizeVersionCompatibilityAndMigrationFields()
    {
        var createdAt = DateTimeOffset.Parse("2026-06-03T12:00:00Z");
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");

        var workflow = WorkflowPack.Create(
            " generic-image-series ",
            " Generic Image Series ",
            "1.2.3",
            compatibility,
            [" Source ", "Brief", "source", "Plan", "Produce", "Review", "Deliver"],
            [" article-illustration-pack "],
            PackLifecycleState.Active,
            [],
            createdAt);
        var blueprint = BlueprintPack.Create(
            " article-illustration-pack ",
            " Article Illustration Pack ",
            "1.0.0",
            compatibility,
            ["article-illustration", "concept-explainer"],
            PackLifecycleState.Deprecated,
            ["Use article-illustration-pack-v2 before 2026-12-31."],
            createdAt);
        var industry = IndustryPack.Create(
            " education-k12 ",
            "K12 Education",
            "1.0.0",
            compatibility,
            ["teacher", "courseware"],
            ["generic-image-series"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var renderer = RendererPack.Create(
            " deterministic-doc-renderer ",
            "Deterministic Document Renderer",
            "1.0.1",
            compatibility,
            ["pdf", "docx", "markdown"],
            PackLifecycleState.Active,
            [],
            createdAt);
        var rubric = ReviewRubricPack.Create(
            " education-review-rubrics ",
            "Education Review Rubrics",
            "1.0.0",
            compatibility,
            ["educational-accuracy", "text-readability"],
            PackLifecycleState.Active,
            [],
            createdAt);

        Assert.Equal("generic-image-series", workflow.Metadata.Id);
        Assert.Equal("Generic Image Series", workflow.Metadata.DisplayName);
        Assert.Equal("1.2.3", workflow.Metadata.Version.ToString());
        Assert.Equal("1.0.0", workflow.Metadata.Compatibility.MinimumAppVersion.ToString());
        Assert.Equal("2.0.0", workflow.Metadata.Compatibility.MaximumAppVersion!.ToString());
        Assert.Equal(["generic-image-series"], workflow.ScenarioIds);
        Assert.Empty(workflow.IndustryPackIds);
        Assert.Empty(workflow.RendererPackIds);
        Assert.Empty(workflow.ReviewRubricPackIds);
        Assert.Equal(["Source", "Brief", "Plan", "Produce", "Review", "Deliver"], workflow.StageIds);
        Assert.Equal(["article-illustration-pack"], workflow.BlueprintPackIds);

        Assert.Equal(PackLifecycleState.Deprecated, blueprint.Metadata.LifecycleState);
        Assert.Equal(["Use article-illustration-pack-v2 before 2026-12-31."], blueprint.Metadata.MigrationNotes);
        Assert.Equal(["article-illustration", "concept-explainer"], blueprint.BlueprintIds);
        Assert.Equal(["teacher", "courseware"], industry.AudienceTags);
        Assert.Equal(["generic-image-series"], industry.WorkflowPackIds);
        Assert.Equal(["pdf", "docx", "markdown"], renderer.OutputFormats);
        Assert.Equal(["educational-accuracy", "text-readability"], rubric.RubricTemplateIds);
    }

    [Fact]
    public void PackMetadata_RejectsInvalidVersionsAndMissingDeprecatedMigrationNotes()
    {
        var compatibility = PackCompatibilityRange.Create("1.0.0", "2.0.0");
        var createdAt = DateTimeOffset.Parse("2026-06-03T12:00:00Z");

        Assert.Throws<ArgumentException>(() => PackVersion.Parse("1.2"));
        Assert.Throws<ArgumentException>(() => PackCompatibilityRange.Create("2.0.0", "1.0.0"));
        Assert.Throws<ArgumentException>(() =>
            WorkflowPack.Create(
                "bad deprecated pack",
                "Bad Deprecated Pack",
                "1.0.0",
                compatibility,
                ["Source"],
                [],
                PackLifecycleState.Deprecated,
                [],
                createdAt));
    }
}
