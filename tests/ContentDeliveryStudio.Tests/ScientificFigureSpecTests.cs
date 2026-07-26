using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureSpecTests
{
    [Fact]
    public void Create_PreservesEvidenceAndConventionAuthority()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var force = FigureElementSpec.Create(
            "element-force",
            "Net force acting on the object.",
            FigureElementKind.Entity,
            "Net force",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
        var acceleration = FigureElementSpec.Create(
            "element-acceleration",
            "Acceleration of the object.",
            FigureElementKind.Entity,
            "Acceleration",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromConvention(
                "scientific_convention:vector-arrow",
                "A directed arrow represents vector direction."));
        var relation = FigureRelationSpec.Create(
            "relation-force-acceleration",
            force.ElementId,
            acceleration.ElementId,
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "Net force causes acceleration.",
            "single directed arrow",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));

        var spec = ScientificFigureSpec.Create(
            Guid.NewGuid(),
            understanding,
            "Explain Newton's second law.",
            "Net force causes acceleration.",
            "Secondary physics learners",
            isSchematic: true,
            ScientificFigureRiskLevel.Medium,
            [force, acceleration],
            [relation],
            []);

        Assert.Equal(1, spec.Version);
        Assert.Equal(understanding.UnderstandingId, spec.UnderstandingId);
        Assert.Equal(understanding.Version, spec.UnderstandingVersion);
        Assert.Equal(ScientificFigureSpecStatus.ReadyForGate1, spec.Status);
        Assert.Empty(spec.BlockingCodes);
        Assert.Equal(
            ScientificProvenanceKind.ClaimEvidence,
            spec.Elements[0].Provenance!.Kind);
        Assert.Equal(
            ScientificProvenanceKind.ScientificConvention,
            spec.Elements[1].Provenance!.Kind);
    }

    [Theory]
    [InlineData(true, FigureElementKind.Entity)]
    [InlineData(false, FigureElementKind.Formula)]
    public void Create_RejectsScientificElementWithoutAuthority(
        bool isCritical,
        FigureElementKind kind)
    {
        Assert.Throws<ArgumentException>(() =>
            FigureElementSpec.Create(
                "element-unsupported",
                "Unsupported scientific content.",
                kind,
                "Unsupported",
                "deterministic-node",
                FigureContentRequirement.Required,
                isCritical,
                provenance: null));
    }

    [Fact]
    public void Create_AllowsUnprovenancedDecorativeElement()
    {
        var element = FigureElementSpec.Create(
            "element-decoration",
            "A non-evidentiary background texture.",
            FigureElementKind.DecorativeAsset,
            labelOrFormula: null,
            "bounded-raster-asset",
            FigureContentRequirement.Optional,
            isCritical: false,
            provenance: null);

        Assert.Null(element.Provenance);
        Assert.False(element.IsCritical);
    }

    [Fact]
    public void Create_RejectsScientificRelationWithoutAuthority()
    {
        Assert.Throws<ArgumentException>(() =>
            FigureRelationSpec.Create(
                "relation-unsupported",
                "element-a",
                "element-b",
                FigureRelationKind.Causes,
                FigureRelationDirection.Directed,
                "causes",
                "An unsupported causal relation.",
                "single directed arrow",
                FigureContentRequirement.Required,
                isCritical: true,
                provenance: null));
    }

    [Fact]
    public void Create_RejectsFormulaWithoutExactContent()
    {
        Assert.Throws<ArgumentException>(() =>
            FigureElementSpec.Create(
                "element-formula",
                "Newton's second-law formula.",
                FigureElementKind.Formula,
                labelOrFormula: null,
                "deterministic-formula",
                FigureContentRequirement.Required,
                isCritical: true,
                ScientificFigureProvenance.FromConvention(
                    "scientific_convention:newton-second-law",
                    "Use the conventional symbolic form.")));
    }

    [Fact]
    public void Create_RejectsResolvedIssueWithoutResolution()
    {
        Assert.Throws<ArgumentException>(() =>
            ScientificFigureIssue.Create(
                "issue-resolved-without-basis",
                ScientificFigureIssueKind.Uncertainty,
                "The notation was initially uncertain.",
                ScientificFigureIssueStatus.Resolved));
    }

    [Fact]
    public void Create_RejectsUnknownRelationEndpoint()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var element = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var relation = FigureRelationSpec.Create(
            "relation-unknown-target",
            element.ElementId,
            "element-missing",
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "A relation with a missing endpoint.",
            "single directed arrow",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));

        Assert.Throws<ArgumentException>(() =>
            ScientificFigureSpec.Create(
                Guid.NewGuid(),
                understanding,
                "Explain a mechanism.",
                "A bounded central message.",
                "Scientific readers",
                isSchematic: true,
                ScientificFigureRiskLevel.Medium,
                [element],
                [relation],
                []));
    }

    [Fact]
    public void Create_RejectsIncludedRelationToForbiddenElement()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var authority = ScientificFigureProvenance.FromEvidence(claim, evidence);
        var included = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var forbidden = FigureElementSpec.Create(
            "element-forbidden",
            "Content explicitly excluded from the figure.",
            FigureElementKind.Entity,
            "Forbidden",
            "deterministic-node",
            FigureContentRequirement.Forbidden,
            isCritical: true,
            authority);
        var relation = FigureRelationSpec.Create(
            "relation-to-forbidden",
            included.ElementId,
            forbidden.ElementId,
            FigureRelationKind.AssociatesWith,
            FigureRelationDirection.Undirected,
            label: null,
            "An included relation cannot render a forbidden endpoint.",
            "plain line",
            FigureContentRequirement.Required,
            isCritical: true,
            authority);

        Assert.Throws<ArgumentException>(() =>
            ScientificFigureTestFixture.CreateSpec(
                understanding,
                [included, forbidden],
                [relation],
                []));
    }

    [Fact]
    public void Create_BlocksUnresolvedSpecificationIssue()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var issue = ScientificFigureIssue.Create(
            "issue-direction-uncertain",
            ScientificFigureIssueKind.Uncertainty,
            "The relation direction remains uncertain.",
            ScientificFigureIssueStatus.Unresolved);

        var spec = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [ScientificFigureTestFixture.RequiredElement(claim, evidence)],
            [],
            [issue]);

        Assert.Equal(ScientificFigureSpecStatus.Blocked, spec.Status);
        Assert.Contains("unresolved-uncertainty:issue-direction-uncertain", spec.BlockingCodes);
    }

    [Fact]
    public void Create_BlocksAuthorityFromDifferentUnderstanding()
    {
        var selectedUnderstanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var foreignUnderstanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var foreignClaim = Assert.Single(foreignUnderstanding.Claims);
        var foreignEvidence = Assert.Single(foreignClaim.SupportingEvidence);
        var element = ScientificFigureTestFixture.RequiredElement(
            foreignClaim,
            foreignEvidence);

        var spec = ScientificFigureTestFixture.CreateSpec(
            selectedUnderstanding,
            [element],
            [],
            []);

        Assert.Equal(ScientificFigureSpecStatus.Blocked, spec.Status);
        Assert.Contains("unsupported-element:element-force", spec.BlockingCodes);
    }

    [Fact]
    public void Create_ExposesImmutableSnapshots()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var elements = new List<FigureElementSpec>
        {
            ScientificFigureTestFixture.RequiredElement(claim, evidence),
        };
        var spec = ScientificFigureTestFixture.CreateSpec(
            understanding,
            elements,
            [],
            []);

        elements.Clear();

        Assert.Single(spec.Elements);
        var exposed = Assert.IsAssignableFrom<IList<FigureElementSpec>>(spec.Elements);
        Assert.Throws<NotSupportedException>(() =>
            exposed[0] = ScientificFigureTestFixture.RequiredElement(claim, evidence));
    }
}

internal static class ScientificFigureTestFixture
{
    private const string SourceHash =
        "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e";

    public static ScientificDocumentUnderstanding ReadyUnderstanding()
    {
        var location = ScientificSourceLocation.Create(
            pageNumber: 4,
            section: "2.1 Dynamics",
            ScientificBoundingRegion.Create(72, 144, 320, 48),
            ScientificCharacterRange.Create(20, 112));
        var block = ScientificSourceBlock.Create(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            location,
            "Net force causes acceleration for constant mass.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);
        var extraction = ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            SourceHash,
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            [block],
            []);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            block.OriginalText!,
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-newton-second-law",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration for constant mass.",
            block.OriginalText!,
            confidence: 0.96,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-central-mechanism",
            "The central mechanism is represented.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [claim.ClaimId]);

        return ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            "Explain how net force changes motion.",
            version: 1,
            [],
            [claim],
            [],
            [coverage]);
    }

    public static FigureElementSpec RequiredElement(
        ScientificClaim claim,
        ClaimEvidenceLink evidence)
    {
        return FigureElementSpec.Create(
            "element-force",
            "Net force acting on the object.",
            FigureElementKind.Entity,
            "Net force",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
    }

    public static ScientificFigureSpec CreateSpec(
        ScientificDocumentUnderstanding understanding,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        return ScientificFigureSpec.Create(
            Guid.NewGuid(),
            understanding,
            "Explain a bounded scientific mechanism.",
            "Net force causes acceleration.",
            "Secondary physics learners",
            isSchematic: true,
            ScientificFigureRiskLevel.Medium,
            elements,
            relations,
            issues);
    }
}
