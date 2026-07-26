using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificUnderstandingTests
{
    private const string SourceHash =
        "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e";

    [Fact]
    public void AcceptedClaim_PreservesValidatedSourceAuthority()
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-newton-second-law",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration.",
            "Net force causes acceleration.",
            confidence: 0.96,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-central-mechanism",
            "The central mechanism is represented.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [claim.ClaimId]);

        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            "Explain how net force changes motion.",
            version: 1,
            [ScientificTermDefinition.Create(
                "term-net-force",
                "net force",
                "The vector sum of forces acting on an object.",
                ["resultant force"])],
            [claim],
            [],
            [coverage]);

        Assert.Equal(ScientificUnderstandingStatus.ReadyForApproval, understanding.Status);
        Assert.Empty(understanding.BlockingCodes);
        Assert.Equal(extraction.SourceAssetId, understanding.SourceAssetId);
        Assert.Equal(extraction.SourceSha256, understanding.SourceSha256);
        Assert.Equal(sourceBlock.BlockId, evidence.SourceBlockId);
        Assert.Equal(sourceBlock.Location, evidence.Location);
        Assert.Equal("Net force causes acceleration", evidence.QuotedText);
        Assert.Same(evidence, Assert.Single(claim.SupportingEvidence));
    }

    [Theory]
    [InlineData(ClaimEvidenceRole.Qualification)]
    [InlineData(ClaimEvidenceRole.Contradiction)]
    public void AcceptedClaim_RejectsNonSupportingEvidenceRoles(ClaimEvidenceRole role)
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            sourceBlock.OriginalText!,
            role,
            confidence: 0.9,
            EvidenceValidationState.Validated);

        Assert.Throws<ArgumentException>(() =>
            ScientificClaim.Create(
                "claim-without-support",
                ScientificClaimCategory.Mechanism,
                "A normalized claim.",
                sourceBlock.OriginalText!,
                confidence: 0.9,
                ScientificClaimStatus.Accepted,
                [evidence]));
    }

    [Fact]
    public void ScientificClaim_PreservesQualificationAndContradictionRoles()
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);
        var support = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            sourceBlock.OriginalText!,
            ClaimEvidenceRole.Support,
            confidence: 0.95,
            EvidenceValidationState.Validated);
        var qualification = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            "for constant mass",
            ClaimEvidenceRole.Qualification,
            confidence: 0.92,
            EvidenceValidationState.Validated);
        var contradiction = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            "unless external forces balance",
            ClaimEvidenceRole.Contradiction,
            confidence: 0.8,
            EvidenceValidationState.Validated);

        var claim = ScientificClaim.Create(
            "claim-role-preservation",
            ScientificClaimCategory.Constraint,
            "Acceleration follows net force for constant mass.",
            sourceBlock.OriginalText!,
            confidence: 0.9,
            ScientificClaimStatus.Accepted,
            [support, qualification, contradiction]);

        Assert.Single(claim.SupportingEvidence);
        Assert.Equal(ClaimEvidenceRole.Support, claim.SupportingEvidence[0].Role);
        Assert.True(claim.HasValidatedQualification);
        Assert.True(claim.HasValidatedContradiction);
        Assert.Equal(3, claim.EvidenceLinks.Count);
    }

    [Fact]
    public void Understanding_BlocksDraftClaimWithoutValidatedEvidence()
    {
        var extraction = ReadyExtraction();
        var claim = ScientificClaim.Create(
            "claim-missing-evidence",
            ScientificClaimCategory.Mechanism,
            "An unsupported mechanism.",
            "An unsupported mechanism.",
            confidence: 0.4,
            ScientificClaimStatus.Draft,
            []);

        var understanding = CreateUnderstanding(extraction, [claim]);

        Assert.Equal(ScientificUnderstandingStatus.Blocked, understanding.Status);
        Assert.Contains("claim-missing-evidence:claim-missing-evidence", understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksValidatedContradiction()
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);
        var claim = AcceptedClaim(
            extraction,
            "claim-contradicted",
            [
                ClaimEvidenceLink.Create(
                    extraction,
                    sourceBlock,
                    "unless external forces balance",
                    ClaimEvidenceRole.Contradiction,
                    confidence: 0.8,
                    EvidenceValidationState.Validated),
            ]);

        var understanding = CreateUnderstanding(extraction, [claim]);

        Assert.Equal(ScientificUnderstandingStatus.Blocked, understanding.Status);
        Assert.Contains("claim-contradicted:claim-contradicted", understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksUnresolvedConflict()
    {
        var extraction = ReadyExtraction();
        var first = AcceptedClaim(extraction, "claim-first");
        var second = AcceptedClaim(extraction, "claim-second");
        var conflict = ScientificClaimConflict.Create(
            "conflict-force-direction",
            first.ClaimId,
            second.ClaimId,
            "The claims assign opposite directions to the same force.",
            ScientificConflictStatus.Unresolved,
            resolution: null);

        var understanding = CreateUnderstanding(extraction, [first, second], [conflict]);

        Assert.Equal(ScientificUnderstandingStatus.Blocked, understanding.Status);
        Assert.Contains("unresolved-conflict:conflict-force-direction", understanding.BlockingCodes);
    }

    [Theory]
    [InlineData(ScientificCoverageStatus.Incomplete)]
    [InlineData(ScientificCoverageStatus.Uncertain)]
    public void Understanding_BlocksIncompleteRequiredCoverage(ScientificCoverageStatus status)
    {
        var extraction = ReadyExtraction();
        var claim = AcceptedClaim(extraction, "claim-covered");
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-boundary-condition",
            "The boundary condition is represented.",
            isRequired: true,
            status,
            [claim.ClaimId]);

        var understanding = CreateUnderstanding(extraction, [claim], coverage: [coverage]);

        Assert.Equal(ScientificUnderstandingStatus.Blocked, understanding.Status);
        Assert.Contains(
            "required-coverage-incomplete:coverage-boundary-condition",
            understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksRequiredCoverageWithoutAcceptedClaim()
    {
        var extraction = ReadyExtraction();
        var draft = ScientificClaim.Create(
            "claim-draft",
            ScientificClaimCategory.Uncertainty,
            "The uncertainty is not resolved.",
            "The uncertainty is not resolved.",
            confidence: 0.5,
            ScientificClaimStatus.Draft,
            []);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-uncertainty",
            "The relevant uncertainty is represented.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [draft.ClaimId]);

        var understanding = CreateUnderstanding(extraction, [draft], coverage: [coverage]);

        Assert.Contains(
            "required-coverage-unaccepted:coverage-uncertainty",
            understanding.BlockingCodes);
    }

    [Fact]
    public void ClaimEvidenceLink_RejectsQuotationOutsideSourceBlock()
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);

        Assert.Throws<ArgumentException>(() =>
            ClaimEvidenceLink.Create(
                extraction,
                sourceBlock,
                "This wording does not occur in the source.",
                ClaimEvidenceRole.Support,
                confidence: 0.9,
                EvidenceValidationState.Validated));
    }

    [Fact]
    public void Understanding_ExposesImmutableSnapshots()
    {
        var extraction = ReadyExtraction();
        var claims = new List<ScientificClaim>
        {
            AcceptedClaim(extraction, "claim-immutable"),
        };
        var understanding = CreateUnderstanding(extraction, claims);

        claims.Clear();

        Assert.Single(understanding.Claims);
        var exposedClaims = Assert.IsAssignableFrom<IList<ScientificClaim>>(understanding.Claims);
        Assert.Throws<NotSupportedException>(() =>
            exposedClaims[0] = AcceptedClaim(extraction, "claim-replacement"));
    }

    [Fact]
    public void Understanding_BlocksSupportedClaimThatIsNotAccepted()
    {
        var extraction = ReadyExtraction();
        var sourceBlock = Assert.Single(extraction.Blocks);
        var support = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        var draft = ScientificClaim.Create(
            "claim-supported-draft",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration.",
            sourceBlock.OriginalText!,
            confidence: 0.9,
            ScientificClaimStatus.Draft,
            [support]);

        var understanding = CreateUnderstanding(extraction, [draft]);

        Assert.Contains("claim-not-accepted:claim-supported-draft", understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksEvidenceFromAnotherSourceAsset()
    {
        var authorityExtraction = ReadyExtraction();
        var foreignExtraction = ReadyExtraction();
        var foreignClaim = AcceptedClaim(foreignExtraction, "claim-foreign-source");
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-source-authority",
            "The claim is grounded in the selected source.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [foreignClaim.ClaimId]);

        var understanding = CreateUnderstanding(
            authorityExtraction,
            [foreignClaim],
            coverage: [coverage]);

        Assert.Contains("claim-missing-evidence:claim-foreign-source", understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksWhenRequiredCoverageIsNotDeclared()
    {
        var extraction = ReadyExtraction();
        var claim = AcceptedClaim(extraction, "claim-without-coverage");

        var understanding = CreateUnderstanding(extraction, [claim]);

        Assert.Contains("no-required-coverage", understanding.BlockingCodes);
    }

    [Fact]
    public void Understanding_BlocksCoverageThatIncludesRejectedClaim()
    {
        var extraction = ReadyExtraction();
        var accepted = AcceptedClaim(extraction, "claim-accepted");
        var rejected = ScientificClaim.Create(
            "claim-rejected",
            ScientificClaimCategory.Mechanism,
            "A rejected interpretation.",
            "A rejected interpretation.",
            confidence: 0.2,
            ScientificClaimStatus.Rejected,
            []);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-mixed-status",
            "Every claim used for coverage is accepted.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [accepted.ClaimId, rejected.ClaimId]);

        var understanding = CreateUnderstanding(
            extraction,
            [accepted, rejected],
            coverage: [coverage]);

        Assert.Contains(
            "required-coverage-unaccepted:coverage-mixed-status",
            understanding.BlockingCodes);
    }

    [Fact]
    public void ScientificClaim_RejectsNullEvidenceElements()
    {
        Assert.Throws<ArgumentException>(() =>
            ScientificClaim.Create(
                "claim-null-evidence",
                ScientificClaimCategory.Definition,
                "A definition.",
                "A definition.",
                confidence: 0.5,
                ScientificClaimStatus.Draft,
                [null!]));
    }

    private static ScientificDocumentUnderstanding CreateUnderstanding(
        ScientificDocumentExtraction extraction,
        IReadOnlyList<ScientificClaim> claims,
        IReadOnlyList<ScientificClaimConflict>? conflicts = null,
        IReadOnlyList<ScientificCoverageRequirement>? coverage = null)
    {
        return ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(),
            extraction,
            "Explain the bounded scientific mechanism.",
            version: 1,
            [],
            claims,
            conflicts ?? [],
            coverage ?? []);
    }

    private static ScientificClaim AcceptedClaim(
        ScientificDocumentExtraction extraction,
        string claimId,
        IReadOnlyList<ClaimEvidenceLink>? additionalEvidence = null)
    {
        var sourceBlock = Assert.Single(extraction.Blocks);
        var support = ClaimEvidenceLink.Create(
            extraction,
            sourceBlock,
            "Net force causes acceleration",
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        return ScientificClaim.Create(
            claimId,
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration.",
            sourceBlock.OriginalText!,
            confidence: 0.95,
            ScientificClaimStatus.Accepted,
            [support, .. additionalEvidence ?? []]);
    }

    private static ScientificDocumentExtraction ReadyExtraction()
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
            "Net force causes acceleration for constant mass unless external forces balance.",
            isRequired: true,
            ScientificRecoveryStatus.NotRequired);

        return ScientificDocumentExtraction.Create(
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
    }
}
