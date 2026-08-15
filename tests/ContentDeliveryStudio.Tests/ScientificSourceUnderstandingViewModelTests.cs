using System.Collections.ObjectModel;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificSourceUnderstandingViewModelTests
{
    [Fact]
    public void SelectingClaim_LocatesExactAuthoritativeSourceEvidence()
    {
        var fixture = CreateReadyFixture();
        var viewModel = new ScientificSourceUnderstandingViewModel(
            fixture.Extraction,
            fixture.Understanding);

        viewModel.SelectClaim(fixture.Claim.ClaimId);

        Assert.Equal(fixture.Claim.ClaimId, viewModel.SelectedClaim!.ClaimId);
        Assert.Equal(fixture.Evidence.SourceBlockId, viewModel.SelectedEvidence!.SourceBlockId);
        Assert.Equal(fixture.Evidence.QuotedText, viewModel.SelectedEvidence.QuotedText);
        Assert.Equal(fixture.Evidence.Location.PageNumber, viewModel.SelectedEvidence.PageNumber);
        Assert.Equal(fixture.Evidence.Location.Section, viewModel.SelectedEvidence.Section);
        Assert.Equal(fixture.Block.BlockId, viewModel.SelectedSourceBlock!.BlockId);
        Assert.Equal(fixture.Block.OriginalText, viewModel.SelectedSourceBlock.OriginalText);
        Assert.True(viewModel.CanProceed);
    }

    [Fact]
    public void BlockedExtractionMissingEvidenceAndConflict_AreVisibleAndPreventProgression()
    {
        var fixture = CreateBlockedFixture();

        var viewModel = new ScientificSourceUnderstandingViewModel(
            fixture.Extraction,
            fixture.Understanding);

        Assert.False(viewModel.CanProceed);
        Assert.True(viewModel.HasBlockingIssues);
        Assert.Contains("required-formula-missing", viewModel.BlockingReasons);
        Assert.Contains("claim-missing-evidence:claim-unsupported", viewModel.BlockingReasons);
        Assert.Contains("unresolved-conflict:conflict-direction", viewModel.BlockingReasons);
        Assert.Single(viewModel.Conflicts);
        Assert.Equal(ScientificConflictStatus.Unresolved, viewModel.Conflicts[0].Status);
        Assert.Contains(viewModel.Claims, claim => claim.ClaimId == "claim-unsupported" && claim.HasMissingEvidence);
    }

    [Fact]
    public void Correction_CreatesAuditedDraftWithoutMutatingApprovedClaim()
    {
        var fixture = CreateReadyFixture();
        var timestamp = new DateTimeOffset(2026, 7, 26, 16, 0, 0, TimeSpan.Zero);
        var viewModel = new ScientificSourceUnderstandingViewModel(
            fixture.Extraction,
            fixture.Understanding,
            () => timestamp);
        viewModel.SelectClaim(fixture.Claim.ClaimId);
        viewModel.ProposedStatement = "Net force changes acceleration when mass is constant.";
        viewModel.CorrectionReviewer = "human-reviewer";
        viewModel.CorrectionReason = "Make the boundary condition explicit.";

        viewModel.CreateCorrectionDraftCommand.Execute(null);

        var draft = Assert.Single(viewModel.CorrectionDrafts);
        Assert.IsType<ReadOnlyObservableCollection<ScientificClaimCorrectionDraft>>(
            viewModel.CorrectionDrafts);
        Assert.Equal(fixture.Claim.ClaimId, draft.ClaimId);
        Assert.Equal(fixture.Claim.NormalizedStatement, draft.OriginalStatement);
        Assert.Equal("Net force changes acceleration when mass is constant.", draft.ProposedStatement);
        Assert.Equal("human-reviewer", draft.Reviewer);
        Assert.Equal("Make the boundary condition explicit.", draft.Reason);
        Assert.Equal(timestamp, draft.CreatedAt);
        Assert.Equal(fixture.Claim.NormalizedStatement, fixture.Understanding.Claims[0].NormalizedStatement);
        Assert.Equal(ScientificClaimStatus.Accepted, fixture.Understanding.Claims[0].Status);
    }

    private static ScientificSourceUnderstandingFixture CreateReadyFixture()
    {
        var block = CreateBlock(
            "block-dynamics",
            ScientificSourceBlockKind.Paragraph,
            "Net force causes acceleration for constant mass.",
            ScientificRecoveryStatus.NotRequired);
        var extraction = CreateExtraction([block], []);
        var evidence = ClaimEvidenceLink.Create(
            extraction,
            block,
            block.OriginalText!,
            ClaimEvidenceRole.Support,
            confidence: 0.98,
            EvidenceValidationState.Validated);
        var claim = ScientificClaim.Create(
            "claim-force",
            ScientificClaimCategory.CausalRelation,
            "Net force causes acceleration for constant mass.",
            block.OriginalText!,
            confidence: 0.96,
            ScientificClaimStatus.Accepted,
            [evidence]);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-force",
            "The central causal relation is covered.",
            isRequired: true,
            ScientificCoverageStatus.Complete,
            [claim.ClaimId]);
        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(), extraction, "Explain force and acceleration.", 1,
            [], [claim], [], [coverage]);
        return new ScientificSourceUnderstandingFixture(extraction, understanding, block, claim, evidence);
    }

    private static ScientificSourceUnderstandingFixture CreateBlockedFixture()
    {
        var paragraph = CreateBlock(
            "block-direction",
            ScientificSourceBlockKind.Paragraph,
            "The force points upward in the first case and downward in the second case.",
            ScientificRecoveryStatus.NotRequired);
        var formula = CreateBlock(
            "block-formula",
            ScientificSourceBlockKind.Formula,
            null,
            ScientificRecoveryStatus.Missing);
        var extraction = CreateExtraction([paragraph, formula], []);
        var support = ClaimEvidenceLink.Create(
            extraction, paragraph, "The force points upward",
            ClaimEvidenceRole.Support, 0.8, EvidenceValidationState.Validated);
        var first = ScientificClaim.Create(
            "claim-up", ScientificClaimCategory.CausalRelation,
            "The force points upward.", "The force points upward", 0.8,
            ScientificClaimStatus.Accepted, [support]);
        var unsupported = ScientificClaim.Create(
            "claim-unsupported", ScientificClaimCategory.QuantitativeResult,
            "The missing formula determines the magnitude.", "The missing formula determines the magnitude.", 0.4,
            ScientificClaimStatus.Draft, []);
        var conflict = ScientificClaimConflict.Create(
            "conflict-direction", first.ClaimId, unsupported.ClaimId,
            "Direction cannot be reconciled until the formula is recovered.",
            ScientificConflictStatus.Unresolved, null);
        var coverage = ScientificCoverageRequirement.Create(
            "coverage-direction", "Direction must be resolved.", true,
            ScientificCoverageStatus.Uncertain, [first.ClaimId, unsupported.ClaimId]);
        var understanding = ScientificDocumentUnderstanding.Create(
            Guid.NewGuid(), extraction, "Explain direction.", 1,
            [], [first, unsupported], [conflict], [coverage]);
        return new ScientificSourceUnderstandingFixture(extraction, understanding, paragraph, first, support);
    }

    private static ScientificSourceBlock CreateBlock(
        string id,
        ScientificSourceBlockKind kind,
        string? text,
        ScientificRecoveryStatus recovery)
    {
        return ScientificSourceBlock.Create(
            id,
            kind,
            ScientificSourceLocation.Create(
                4,
                "2.1 Dynamics",
                ScientificBoundingRegion.Create(72, 144, 320, 48),
                ScientificCharacterRange.Create(20, 112)),
            text,
            isRequired: true,
            recovery);
    }

    private static ScientificDocumentExtraction CreateExtraction(
        IReadOnlyList<ScientificSourceBlock> blocks,
        IReadOnlyList<ScientificExtractionDiagnostic> diagnostics)
    {
        return ScientificDocumentExtraction.Create(
            Guid.NewGuid(),
            "sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e",
            ScientificExtractorIdentity.Create("fixture-extractor", "1.0"),
            ScientificExtractionQuality.Create(
                false, false, ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            diagnostics);
    }

    private sealed record ScientificSourceUnderstandingFixture(
        ScientificDocumentExtraction Extraction,
        ScientificDocumentUnderstanding Understanding,
        ScientificSourceBlock Block,
        ScientificClaim Claim,
        ClaimEvidenceLink Evidence);
}
