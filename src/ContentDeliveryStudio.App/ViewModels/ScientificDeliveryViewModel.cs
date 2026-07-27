using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificDeliveryViewModel : ObservableObject
{
    private readonly ScientificFigureDeliveryService _deliveryService;
    private readonly ScientificFigureDeliveryRequest _requestTemplate;
    private readonly Action<byte[]>? _packageExportRequested;
    private readonly Func<DateTimeOffset> _clock;
    private ScientificDeliveryArtifactRowViewModel? _selectedArtifact;
    private string _gateTwoReviewer = string.Empty;
    private string _gateTwoNotes = string.Empty;
    private bool _isGateTwoApproved;
    private bool _isGateTwoRejected;
    private ScientificGateTwoApproval? _gateTwoApproval;
    private byte[]? _packageBytes;
    private IReadOnlyList<ScientificClaimEvidenceItemMap> _evidenceItems;
    private IReadOnlyList<ScientificRepairActionRowViewModel> _rejectionRepairActions = [];

    public ScientificDeliveryViewModel(
        ScientificFigureDeliveryService deliveryService,
        ScientificFigureDeliveryRequest requestTemplate,
        Action<byte[]>? packageExportRequested = null,
        Func<DateTimeOffset>? clock = null,
        LocalizationService? localizationService = null)
    {
        _deliveryService = deliveryService
            ?? throw new ArgumentNullException(nameof(deliveryService));
        _requestTemplate = requestTemplate
            ?? throw new ArgumentNullException(nameof(requestTemplate));
        _packageExportRequested = packageExportRequested;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);

        Artifacts = BuildArtifacts(requestTemplate);
        SelectedArtifact = Artifacts.FirstOrDefault();
        Providers = requestTemplate.Providers.ToArray();
        Repairs = requestTemplate.Repairs.ToArray();
        _evidenceItems = BuildEvidenceMap(requestTemplate.Workflow.Specification);
        GateOneApproval = requestTemplate.Workflow.Gate1Approval;
        ContractReviewPassed = requestTemplate.ContractReview.Passed;
        MachineReviewPassed = requestTemplate.MachineReview.CanProceedToGate2;
        var unresolvedIssues = BuildUnresolvedIssues(requestTemplate);
        IsDomainEligible = ValidateReadiness(requestTemplate, unresolvedIssues);
        UnresolvedIssues = unresolvedIssues.ToArray();
        CanApproveGateTwo = IsDomainEligible;

        var localization = localizationService ?? new LocalizationService();
        FormatsTitle = localization.GetText(LocalizationKey.ScientificDeliveryFormats);
        EvidenceChainTitle = localization.GetText(LocalizationKey.ScientificEvidenceChain);
        ReviewStatusTitle = localization.GetText(LocalizationKey.ScientificReviewStatus);
        GateTwoTitle = localization.GetText(LocalizationKey.ScientificGateTwo);
        ReviewerLabel = localization.GetText(LocalizationKey.ScientificReviewer);
        NotesLabel = localization.GetText(LocalizationKey.ScientificNotes);
        ApproveGateTwoText = localization.GetText(LocalizationKey.ScientificApproveGateTwo);
        RejectGateTwoText = localization.GetText(LocalizationKey.ScientificRejectGateTwo);
        ExportPackageText = localization.GetText(LocalizationKey.ScientificExportPackage);
        ContractStatusLabel = localization.GetText(LocalizationKey.ScientificContractStatus);
        MachineStatusLabel = localization.GetText(LocalizationKey.ScientificMachineStatus);
        RepairCountLabel = localization.GetText(LocalizationKey.ScientificRepairCount);

        ApproveGateTwoCommand = new RelayCommand(
            ApproveGateTwo,
            CanExecuteGateTwoDecision);
        RejectGateTwoCommand = new RelayCommand(
            RejectGateTwo,
            CanExecuteGateTwoDecision);
        ExportPackageCommand = new RelayCommand(
            ExportPackage,
            CanExportPackage);
    }

    public IReadOnlyList<ScientificDeliveryArtifactRowViewModel> Artifacts { get; }

    public IReadOnlyList<ScientificDeliveryProviderMetadata> Providers { get; }

    public IReadOnlyList<ScientificRepairRecord> Repairs { get; }

    public IReadOnlyList<string> UnresolvedIssues { get; }

    public IReadOnlyList<ScientificClaimEvidenceItemMap> EvidenceItems
    {
        get => _evidenceItems;
        private set => SetProperty(ref _evidenceItems, value);
    }

    public IReadOnlyList<ScientificRepairActionRowViewModel> RejectionRepairActions
    {
        get => _rejectionRepairActions;
        private set => SetProperty(ref _rejectionRepairActions, value);
    }

    public ScientificGate1Approval? GateOneApproval { get; }

    public ScientificGateTwoApproval? GateTwoApproval
    {
        get => _gateTwoApproval;
        private set => SetProperty(ref _gateTwoApproval, value);
    }

    public bool ContractReviewPassed { get; }

    public bool MachineReviewPassed { get; }

    public bool IsDomainEligible { get; }

    public bool CanApproveGateTwo { get; private set; }

    public bool IsGateTwoApproved
    {
        get => _isGateTwoApproved;
        private set => SetProperty(ref _isGateTwoApproved, value);
    }

    public bool IsGateTwoRejected
    {
        get => _isGateTwoRejected;
        private set => SetProperty(ref _isGateTwoRejected, value);
    }

    public byte[]? PackageBytes => _packageBytes?.ToArray();

    public int RepairCount => Repairs.Count;

    public string FormatsTitle { get; }

    public string EvidenceChainTitle { get; }

    public string ReviewStatusTitle { get; }

    public string GateTwoTitle { get; }

    public string ReviewerLabel { get; }

    public string NotesLabel { get; }

    public string ApproveGateTwoText { get; }

    public string RejectGateTwoText { get; }

    public string ExportPackageText { get; }

    public string ContractStatusLabel { get; }

    public string MachineStatusLabel { get; }

    public string RepairCountLabel { get; }

    public IRelayCommand ApproveGateTwoCommand { get; }

    public IRelayCommand RejectGateTwoCommand { get; }

    public IRelayCommand ExportPackageCommand { get; }

    public ScientificDeliveryArtifactRowViewModel? SelectedArtifact
    {
        get => _selectedArtifact;
        set => SetProperty(ref _selectedArtifact, value);
    }

    public string GateTwoReviewer
    {
        get => _gateTwoReviewer;
        set
        {
            if (SetProperty(ref _gateTwoReviewer, value))
            {
                NotifyDecisionCommands();
            }
        }
    }

    public string GateTwoNotes
    {
        get => _gateTwoNotes;
        set
        {
            if (SetProperty(ref _gateTwoNotes, value))
            {
                NotifyDecisionCommands();
            }
        }
    }

    private bool CanExecuteGateTwoDecision()
    {
        return CanApproveGateTwo
            && !string.IsNullOrWhiteSpace(GateTwoReviewer)
            && !string.IsNullOrWhiteSpace(GateTwoNotes);
    }

    private void ApproveGateTwo()
    {
        var result = _deliveryService.DecideGateTwo(BuildRequest(approved: true));
        _packageBytes = result.PackageBytes?.ToArray()
            ?? throw new InvalidOperationException("Approved Gate 2 returned no package bytes.");
        GateTwoApproval = result.GateTwoApproval
            ?? throw new InvalidOperationException("Approved Gate 2 returned no approval record.");
        EvidenceItems = result.Package?.ClaimEvidenceItemMap.ToArray()
            ?? throw new InvalidOperationException("Approved Gate 2 returned no delivery package.");
        IsGateTwoApproved = true;
        CompleteDecision();
        OnPropertyChanged(nameof(PackageBytes));
        ExportPackageCommand.NotifyCanExecuteChanged();
    }

    private void RejectGateTwo()
    {
        var result = _deliveryService.DecideGateTwo(BuildRequest(approved: false));
        RejectionRepairActions = result.RejectionRepairPlan?.Actions
            .Select(action => new ScientificRepairActionRowViewModel(action))
            .ToArray() ?? [];
        IsGateTwoRejected = true;
        CompleteDecision();
    }

    private ScientificFigureDeliveryRequest BuildRequest(bool approved)
    {
        return _requestTemplate with
        {
            HumanDecision = new ScientificGateTwoDecision(
                approved,
                GateTwoReviewer.Trim(),
                GateTwoNotes.Trim(),
                _clock()),
        };
    }

    private void CompleteDecision()
    {
        CanApproveGateTwo = false;
        OnPropertyChanged(nameof(CanApproveGateTwo));
        NotifyDecisionCommands();
    }

    private void NotifyDecisionCommands()
    {
        ApproveGateTwoCommand.NotifyCanExecuteChanged();
        RejectGateTwoCommand.NotifyCanExecuteChanged();
    }

    private bool CanExportPackage()
    {
        return _packageExportRequested is not null && _packageBytes is { Length: > 0 };
    }

    private void ExportPackage()
    {
        if (!CanExportPackage())
        {
            throw new InvalidOperationException(
                "An approved scientific delivery package is required before export.");
        }

        _packageExportRequested!(_packageBytes!.ToArray());
    }

    private static IReadOnlyList<ScientificDeliveryArtifactRowViewModel> BuildArtifacts(
        ScientificFigureDeliveryRequest request)
    {
        var rows = new List<ScientificDeliveryArtifactRowViewModel>
        {
            new(
                "svg",
                "image/svg+xml",
                Encoding.UTF8.GetBytes(request.Svg.Svg),
                request.Svg.Sha256,
                request.Svg.Sha256,
                request.Exports.SemanticSha256),
        };
        rows.AddRange(request.Exports.Artifacts.Select(artifact =>
            new ScientificDeliveryArtifactRowViewModel(
                artifact.Format,
                artifact.MimeType,
                artifact.Bytes,
                artifact.Sha256,
                artifact.SourceSvgSha256,
                artifact.SemanticSha256)));
        return rows;
    }

    private static IReadOnlyList<ScientificClaimEvidenceItemMap> BuildEvidenceMap(
        ScientificFigureSpec specification)
    {
        return specification.Elements
            .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
            .Select(item => MapEvidence(item.ElementId, item.Kind.ToString(), item.Provenance))
            .Concat(specification.Relations
                .Where(item => item.Requirement != FigureContentRequirement.Forbidden)
                .Select(item => MapEvidence(item.RelationId, "Relation", item.Provenance)))
            .ToArray();
    }

    private static ScientificClaimEvidenceItemMap MapEvidence(
        string itemId,
        string itemKind,
        ScientificFigureProvenance? provenance)
    {
        return new ScientificClaimEvidenceItemMap(
            itemId,
            itemKind,
            provenance?.ClaimId,
            provenance?.Evidence?.SourceBlockId,
            provenance?.Evidence?.QuotedText,
            provenance?.ConventionId);
    }

    private static List<string> BuildUnresolvedIssues(
        ScientificFigureDeliveryRequest request)
    {
        var issues = request.Workflow.Specification.Issues
            .Where(item => item.Status == ScientificFigureIssueStatus.Unresolved)
            .Select(item => $"{item.IssueId}: {item.Description}")
            .ToList();
        issues.AddRange(request.ContractReview.HardFailures.Select(item =>
            $"contract:{item.Code}: {item.Evidence}"));
        issues.AddRange(request.MachineReview.Blockers.Select(item =>
            $"{item.Layer}:{item.Code}: {item.Evidence}"));
        issues.AddRange(request.Repairs
            .Where(item => item.Status == ScientificRepairRecordStatus.Unresolved)
            .Select(item => $"repair:{item.Action.FindingCode}: {item.Action.Evidence}"));
        return issues;
    }

    private static bool ValidateReadiness(
        ScientificFigureDeliveryRequest request,
        ICollection<string> unresolvedIssues)
    {
        try
        {
            ScientificFigureDeliveryService.ValidateGateTwoReadiness(request);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            if (!unresolvedIssues.Contains(exception.Message, StringComparer.Ordinal))
            {
                unresolvedIssues.Add(exception.Message);
            }

            return false;
        }
    }
}

public sealed class ScientificDeliveryArtifactRowViewModel
{
    private readonly byte[] _bytes;

    public ScientificDeliveryArtifactRowViewModel(
        string format,
        string mimeType,
        byte[] bytes,
        string sha256,
        string sourceSvgSha256,
        string semanticSha256)
    {
        Format = format;
        MimeType = mimeType;
        _bytes = bytes.ToArray();
        Sha256 = sha256;
        SourceSvgSha256 = sourceSvgSha256;
        SemanticSha256 = semanticSha256;
    }

    public string Format { get; }

    public string MimeType { get; }

    public int ByteCount => _bytes.Length;

    public string Sha256 { get; }

    public string SourceSvgSha256 { get; }

    public string SemanticSha256 { get; }

    public bool IsSvg => string.Equals(Format, "svg", StringComparison.OrdinalIgnoreCase);

    public bool IsPng => string.Equals(Format, "png", StringComparison.OrdinalIgnoreCase);

    public bool IsPdf => string.Equals(Format, "pdf", StringComparison.OrdinalIgnoreCase);

    internal byte[] GetBytes() => _bytes.ToArray();
}
