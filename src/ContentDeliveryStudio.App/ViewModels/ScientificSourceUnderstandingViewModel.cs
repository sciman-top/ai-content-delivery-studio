using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificSourceUnderstandingViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _clock;
    private ScientificClaimRowViewModel? _selectedClaim;
    private ScientificEvidenceRowViewModel? _selectedEvidence;
    private ScientificSourceBlockRowViewModel? _selectedSourceBlock;
    private readonly ObservableCollection<ScientificClaimCorrectionDraft> _correctionDrafts = [];
    private string _proposedStatement = string.Empty;
    private string _correctionReviewer = string.Empty;
    private string _correctionReason = string.Empty;

    public ScientificSourceUnderstandingViewModel(
        ScientificDocumentExtraction extraction,
        ScientificDocumentUnderstanding understanding,
        Func<DateTimeOffset>? clock = null,
        LocalizationService? localizationService = null)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(understanding);
        if (extraction.SourceAssetId != understanding.SourceAssetId
            || !string.Equals(
                extraction.SourceSha256,
                understanding.SourceSha256,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Understanding must reference the supplied authoritative extraction.",
                nameof(understanding));
        }

        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        var localization = localizationService ?? new LocalizationService();
        SourceBlocks = extraction.Blocks.Select(MapSourceBlock).ToArray();
        Claims = understanding.Claims.Select(MapClaim).ToArray();
        Conflicts = understanding.Conflicts.Select(MapConflict).ToArray();
        BlockingReasons = extraction.BlockingCodes
            .Concat(understanding.BlockingCodes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Diagnostics = extraction.Diagnostics.Select(item =>
            new ScientificDiagnosticRowViewModel(item.Code, item.Severity, item.Message)).ToArray();
        ExtractionStatus = extraction.Status;
        UnderstandingStatus = understanding.Status;
        CanProceed = extraction.Status == ScientificExtractionStatus.Ready
            && understanding.Status == ScientificUnderstandingStatus.ReadyForApproval;
        HasBlockingIssues = BlockingReasons.Count > 0;

        SourceTitle = localization.GetText(LocalizationKey.ScientificSource);
        UnderstandingTitle = localization.GetText(LocalizationKey.ScientificUnderstanding);
        SourceBlocksTitle = localization.GetText(LocalizationKey.ScientificSourceBlocks);
        DiagnosticsTitle = localization.GetText(LocalizationKey.ScientificDiagnostics);
        ClaimsTitle = localization.GetText(LocalizationKey.ScientificClaims);
        EvidenceTitle = localization.GetText(LocalizationKey.ScientificEvidence);
        ConflictsTitle = localization.GetText(LocalizationKey.ScientificConflicts);
        CorrectionDraftTitle = localization.GetText(LocalizationKey.ScientificCorrectionDraft);
        ProposedStatementLabel = localization.GetText(LocalizationKey.ScientificProposedStatement);
        ReviewerLabel = localization.GetText(LocalizationKey.ScientificReviewer);
        ReasonLabel = localization.GetText(LocalizationKey.ScientificReason);
        CreateCorrectionDraftText = localization.GetText(LocalizationKey.ScientificCreateCorrectionDraft);
        CreateCorrectionDraftCommand = new RelayCommand(
            CreateCorrectionDraft,
            CanCreateCorrectionDraft);
        CorrectionDrafts = new ReadOnlyObservableCollection<ScientificClaimCorrectionDraft>(
            _correctionDrafts);
    }

    public IReadOnlyList<ScientificSourceBlockRowViewModel> SourceBlocks { get; }

    public IReadOnlyList<ScientificClaimRowViewModel> Claims { get; }

    public IReadOnlyList<ScientificConflictRowViewModel> Conflicts { get; }

    public IReadOnlyList<ScientificDiagnosticRowViewModel> Diagnostics { get; }

    public IReadOnlyList<string> BlockingReasons { get; }

    public ReadOnlyObservableCollection<ScientificClaimCorrectionDraft> CorrectionDrafts { get; }

    public ScientificExtractionStatus ExtractionStatus { get; }

    public ScientificUnderstandingStatus UnderstandingStatus { get; }

    public bool CanProceed { get; }

    public bool HasBlockingIssues { get; }

    public string SourceTitle { get; }

    public string UnderstandingTitle { get; }

    public string SourceBlocksTitle { get; }

    public string DiagnosticsTitle { get; }

    public string ClaimsTitle { get; }

    public string EvidenceTitle { get; }

    public string ConflictsTitle { get; }

    public string CorrectionDraftTitle { get; }

    public string ProposedStatementLabel { get; }

    public string ReviewerLabel { get; }

    public string ReasonLabel { get; }

    public string CreateCorrectionDraftText { get; }

    public IRelayCommand CreateCorrectionDraftCommand { get; }

    public ScientificClaimRowViewModel? SelectedClaim
    {
        get => _selectedClaim;
        set
        {
            if (SetProperty(ref _selectedClaim, value))
            {
                LocateSelectedEvidence();
                CreateCorrectionDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ScientificEvidenceRowViewModel? SelectedEvidence
    {
        get => _selectedEvidence;
        private set => SetProperty(ref _selectedEvidence, value);
    }

    public ScientificSourceBlockRowViewModel? SelectedSourceBlock
    {
        get => _selectedSourceBlock;
        set => SetProperty(ref _selectedSourceBlock, value);
    }

    public string ProposedStatement
    {
        get => _proposedStatement;
        set
        {
            if (SetProperty(ref _proposedStatement, value))
            {
                CreateCorrectionDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string CorrectionReviewer
    {
        get => _correctionReviewer;
        set
        {
            if (SetProperty(ref _correctionReviewer, value))
            {
                CreateCorrectionDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string CorrectionReason
    {
        get => _correctionReason;
        set
        {
            if (SetProperty(ref _correctionReason, value))
            {
                CreateCorrectionDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void SelectClaim(string claimId)
    {
        SelectedClaim = Claims.SingleOrDefault(item =>
            string.Equals(item.ClaimId, claimId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Scientific claim not found: {claimId}");
    }

    private void LocateSelectedEvidence()
    {
        SelectedEvidence = SelectedClaim?.Evidence
            .OrderByDescending(item =>
                item.ValidationState == EvidenceValidationState.Validated
                && item.Role is ClaimEvidenceRole.Support or ClaimEvidenceRole.Definition)
            .ThenBy(item => item.SourceBlockId, StringComparer.Ordinal)
            .FirstOrDefault();
        SelectedSourceBlock = SelectedEvidence is null
            ? null
            : SourceBlocks.SingleOrDefault(item =>
                string.Equals(item.BlockId, SelectedEvidence.SourceBlockId, StringComparison.Ordinal));
    }

    private bool CanCreateCorrectionDraft()
    {
        return SelectedClaim is not null
            && !string.IsNullOrWhiteSpace(ProposedStatement)
            && !string.Equals(
                ProposedStatement.Trim(),
                SelectedClaim.NormalizedStatement,
                StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(CorrectionReviewer)
            && !string.IsNullOrWhiteSpace(CorrectionReason);
    }

    private void CreateCorrectionDraft()
    {
        if (!CanCreateCorrectionDraft())
        {
            throw new InvalidOperationException("A complete, changed correction draft is required.");
        }

        _correctionDrafts.Add(new ScientificClaimCorrectionDraft(
            Guid.NewGuid(),
            SelectedClaim!.ClaimId,
            SelectedClaim.NormalizedStatement,
            ProposedStatement.Trim(),
            CorrectionReviewer.Trim(),
            CorrectionReason.Trim(),
            _clock()));
        ProposedStatement = string.Empty;
        CorrectionReason = string.Empty;
    }

    private static ScientificSourceBlockRowViewModel MapSourceBlock(ScientificSourceBlock block)
    {
        return new ScientificSourceBlockRowViewModel(
            block.BlockId,
            block.Kind,
            block.Location.PageNumber,
            block.Location.Section,
            block.OriginalText,
            block.IsRequired,
            block.RecoveryStatus);
    }

    private static ScientificClaimRowViewModel MapClaim(ScientificClaim claim)
    {
        return new ScientificClaimRowViewModel(
            claim.ClaimId,
            claim.Category,
            claim.NormalizedStatement,
            claim.SourceWording,
            claim.Confidence,
            claim.Status,
            claim.SupportingEvidence.Count == 0,
            claim.EvidenceLinks.Select(MapEvidence).ToArray());
    }

    private static ScientificEvidenceRowViewModel MapEvidence(ClaimEvidenceLink evidence)
    {
        return new ScientificEvidenceRowViewModel(
            evidence.SourceBlockId,
            evidence.Location.PageNumber,
            evidence.Location.Section,
            evidence.QuotedText,
            evidence.Role,
            evidence.Confidence,
            evidence.ValidationState);
    }

    private static ScientificConflictRowViewModel MapConflict(ScientificClaimConflict conflict)
    {
        return new ScientificConflictRowViewModel(
            conflict.ConflictId,
            conflict.FirstClaimId,
            conflict.SecondClaimId,
            conflict.Description,
            conflict.Status,
            conflict.Resolution);
    }
}

public sealed record ScientificSourceBlockRowViewModel(
    string BlockId,
    ScientificSourceBlockKind Kind,
    int PageNumber,
    string Section,
    string? OriginalText,
    bool IsRequired,
    ScientificRecoveryStatus RecoveryStatus);

public sealed record ScientificClaimRowViewModel(
    string ClaimId,
    ScientificClaimCategory Category,
    string NormalizedStatement,
    string SourceWording,
    double Confidence,
    ScientificClaimStatus Status,
    bool HasMissingEvidence,
    IReadOnlyList<ScientificEvidenceRowViewModel> Evidence);

public sealed record ScientificEvidenceRowViewModel(
    string SourceBlockId,
    int PageNumber,
    string Section,
    string QuotedText,
    ClaimEvidenceRole Role,
    double Confidence,
    EvidenceValidationState ValidationState);

public sealed record ScientificConflictRowViewModel(
    string ConflictId,
    string FirstClaimId,
    string SecondClaimId,
    string Description,
    ScientificConflictStatus Status,
    string? Resolution);

public sealed record ScientificDiagnosticRowViewModel(
    string Code,
    ScientificDiagnosticSeverity Severity,
    string Message);

public sealed record ScientificClaimCorrectionDraft(
    Guid DraftId,
    string ClaimId,
    string OriginalStatement,
    string ProposedStatement,
    string Reviewer,
    string Reason,
    DateTimeOffset CreatedAt);
