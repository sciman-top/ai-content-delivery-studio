using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Tests;

public sealed class DocumentIllustrationModelTests
{
    [Fact]
    public void DocumentBriefCreate_NormalizesListsAndPreservesBriefMetadata()
    {
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-02T09:00:00Z");

        var brief = DocumentBrief.Create(
            projectId,
            DocumentSourceKind.Markdown,
            " chapter-1.md ",
            "Quantum Primer",
            DocumentFamily.Educational,
            "Middle school students",
            [" Introduction ", "introduction", "Mechanism", " "],
            ["Light bends", " light bends ", "Energy is quantized"],
            [" timeline ", "", "Timeline", "Concept diagram"],
            [" No fake lab data ", "no fake lab data", "Keep labels readable"],
            IllustrationStrictnessLevel.Educational,
            createdAt);

        Assert.NotEqual(Guid.Empty, brief.Id);
        Assert.Equal(projectId, brief.ProjectId);
        Assert.Equal(DocumentSourceKind.Markdown, brief.SourceKind);
        Assert.Equal("chapter-1.md", brief.SourceDisplayName);
        Assert.Equal("Quantum Primer", brief.Title);
        Assert.Equal(DocumentFamily.Educational, brief.DocumentFamily);
        Assert.Equal("Middle school students", brief.Audience);
        Assert.Equal(["Introduction", "Mechanism"], brief.Sections);
        Assert.Equal(["Light bends", "Energy is quantized"], brief.KeyClaims);
        Assert.Equal(["timeline", "Concept diagram"], brief.VisualOpportunities);
        Assert.Equal(["No fake lab data", "Keep labels readable"], brief.KnownConstraints);
        Assert.Equal(IllustrationStrictnessLevel.Educational, brief.StrictnessLevel);
        Assert.Equal(createdAt, brief.CreatedAt);

        Assert.Throws<ArgumentException>(() =>
            DocumentBrief.Create(
                projectId,
                DocumentSourceKind.Markdown,
                "chapter-1.md",
                " ",
                DocumentFamily.Educational,
                "Middle school students",
                [],
                [],
                [],
                [],
                IllustrationStrictnessLevel.Educational,
                createdAt));
    }

    [Fact]
    public void IllustrationPlanCreate_ApprovesAndRejectsTargetsImmutably()
    {
        var projectId = Guid.NewGuid();
        var documentBriefId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-02T09:00:00Z");
        var approvedAt = createdAt.AddMinutes(10);
        var rejectedAt = createdAt.AddMinutes(20);
        var timelineTarget = CreateTarget(documentBriefId, "Timeline", IllustrationPurpose.Timeline, createdAt);
        var diagramTarget = CreateTarget(documentBriefId, "Diagram", IllustrationPurpose.ConceptDiagram, createdAt);
        var plan = IllustrationPlan.Create(
            projectId,
            documentBriefId,
            "Illustrate the key teaching moments.",
            [timelineTarget, diagramTarget],
            ["Covers the introduction"],
            ["Avoid unsupported experimental claims"],
            createdAt);

        var approvedPlan = plan.ApproveTarget(timelineTarget.Id, approvedAt);
        var rejectedPlan = approvedPlan.RejectTarget(diagramTarget.Id, rejectedAt);

        Assert.Equal(projectId, plan.ProjectId);
        Assert.Equal(documentBriefId, plan.DocumentBriefId);
        Assert.Empty(plan.ApprovedTargets);
        Assert.Equal(createdAt, plan.UpdatedAt);
        Assert.Equal(IllustrationTargetApprovalState.Draft, plan.Targets[0].ApprovalState);
        Assert.Equal(IllustrationTargetApprovalState.Draft, plan.Targets[1].ApprovalState);

        var approvedTarget = Assert.Single(approvedPlan.ApprovedTargets);
        Assert.Equal(timelineTarget.Id, approvedTarget.Id);
        Assert.Equal(IllustrationTargetApprovalState.Approved, approvedPlan.Targets[0].ApprovalState);
        Assert.Equal(IllustrationTargetApprovalState.Draft, approvedPlan.Targets[1].ApprovalState);
        Assert.Equal(approvedAt, approvedPlan.UpdatedAt);
        Assert.Equal(IllustrationTargetApprovalState.Draft, plan.Targets[0].ApprovalState);

        Assert.Single(rejectedPlan.ApprovedTargets);
        Assert.Equal(IllustrationTargetApprovalState.Approved, rejectedPlan.Targets[0].ApprovalState);
        Assert.Equal(IllustrationTargetApprovalState.Rejected, rejectedPlan.Targets[1].ApprovalState);
        Assert.Equal(rejectedAt, rejectedPlan.UpdatedAt);

        Assert.Throws<InvalidOperationException>(() => rejectedPlan.ApproveTarget(Guid.NewGuid(), rejectedAt.AddMinutes(1)));
    }

    [Fact]
    public void IllustrationTargetCreate_BlocksExperimentalEvidencePurpose()
    {
        var documentBriefId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-02T09:00:00Z");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            IllustrationTarget.Create(
                documentBriefId,
                "Lab evidence",
                "Section 4",
                IllustrationPurpose.ExperimentalEvidence,
                ["Measured spectrum"],
                [],
                ["Figure 2 reports the measured spectrum."],
                ImageTypePresetCatalog.BackgroundPlate,
                ReviewRubricTemplateCatalog.GeneralImage,
                ImageTextPolicy.DeterministicPostRender,
                ["Use only verified source material"],
                createdAt));

        Assert.Contains("Experimental evidence targets are blocked", exception.Message);
    }

    private static IllustrationTarget CreateTarget(
        Guid documentBriefId,
        string title,
        IllustrationPurpose purpose,
        DateTimeOffset createdAt)
    {
        return IllustrationTarget.Create(
            documentBriefId,
            title,
            "Section 1",
            purpose,
            ["Main concept"],
            ["Photorealistic lab setup"],
            ["The document describes the concept in Section 1."],
            ImageTypePresetCatalog.EducationalPoster,
            ReviewRubricTemplateCatalog.TextHeavyPoster,
            ImageTextPolicy.DeterministicPostRender,
            ["Keep claims grounded"],
            createdAt);
    }
}
