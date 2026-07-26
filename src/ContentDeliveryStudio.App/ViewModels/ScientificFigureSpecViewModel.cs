using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificFigureSpecViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _clock;
    private ScientificFigureWorkflow _workflow;
    private ScientificFigureProposalRowViewModel? _selectedProposal;
    private string _gateOneReviewer = string.Empty;
    private string _gateOneNotes = string.Empty;

    public ScientificFigureSpecViewModel(
        ScientificFigureWorkflow workflow,
        IReadOnlyList<ScientificFigureSpecProposalDiff> proposals,
        Func<DateTimeOffset>? clock = null,
        LocalizationService? localizationService = null)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        ArgumentNullException.ThrowIfNull(proposals);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        var localization = localizationService ?? new LocalizationService();

        Elements = workflow.Specification.Elements.Select(MapElement).ToArray();
        Relations = workflow.Specification.Relations.Select(MapRelation).ToArray();
        Issues = workflow.Specification.Issues.Select(issue =>
            new ScientificFigureIssueRowViewModel(
                issue.IssueId,
                issue.Kind,
                issue.Description,
                issue.Status,
                issue.Resolution)).ToArray();
        ValidateProposals(workflow.Specification, proposals);
        Proposals = proposals.Select(proposal =>
            new ScientificFigureProposalRowViewModel(proposal, RefreshProposalState)).ToArray();

        ElementsTitle = localization.GetText(LocalizationKey.ScientificSpecElements);
        RelationsTitle = localization.GetText(LocalizationKey.ScientificSpecRelations);
        ProposalsTitle = localization.GetText(LocalizationKey.ScientificSpecProposals);
        ProvenanceTitle = localization.GetText(LocalizationKey.ScientificSpecProvenance);
        GateOneTitle = localization.GetText(LocalizationKey.ScientificGateOne);
        RiskLevelLabel = localization.GetText(LocalizationKey.ScientificRiskLevel);
        BlockingReasonsTitle = localization.GetText(LocalizationKey.ScientificBlockingReasons);
        UnderstandingVersionLabel = localization.GetText(LocalizationKey.ScientificUnderstandingVersion);
        SpecificationVersionLabel = localization.GetText(LocalizationKey.ScientificSpecificationVersion);
        FrozenVersionsTitle = localization.GetText(LocalizationKey.ScientificFrozenVersions);
        ReviewerLabel = localization.GetText(LocalizationKey.ScientificReviewer);
        NotesLabel = localization.GetText(LocalizationKey.ScientificNotes);
        AcceptProposalText = localization.GetText(LocalizationKey.ScientificAcceptProposal);
        RejectProposalText = localization.GetText(LocalizationKey.ScientificRejectProposal);
        ApproveGateOneText = localization.GetText(LocalizationKey.ScientificApproveGateOne);

        AcceptProposalCommand = new RelayCommand(AcceptSelectedProposal, HasSelectedProposal);
        RejectProposalCommand = new RelayCommand(RejectSelectedProposal, HasSelectedProposal);
        ApproveGateOneCommand = new RelayCommand(ApproveGateOne, CanExecuteGateOneApproval);
        RefreshGateOneState();
    }

    public IReadOnlyList<ScientificFigureSpecItemRowViewModel> Elements { get; }

    public IReadOnlyList<ScientificFigureSpecItemRowViewModel> Relations { get; }

    public IReadOnlyList<ScientificFigureIssueRowViewModel> Issues { get; }

    public IReadOnlyList<ScientificFigureProposalRowViewModel> Proposals { get; }

    public IReadOnlyList<string> GateOneBlockingReasons { get; private set; } = [];

    public int UnderstandingVersion => _workflow.Specification.UnderstandingVersion;

    public int SpecificationVersion => _workflow.Specification.Version;

    public ScientificFigureRiskLevel RiskLevel => _workflow.Specification.RiskLevel;

    public string CentralMessage => _workflow.Specification.CentralMessage;

    public bool IsDomainEligible =>
        _workflow.Specification.Status == ScientificFigureSpecStatus.ReadyForGate1;

    public bool IsGateOneApproved => _workflow.Gate1Approval is not null;

    public bool CanApproveGateOne { get; private set; }

    public int? FrozenUnderstandingVersion =>
        _workflow.Gate1Approval?.ApprovedUnderstandingVersion;

    public int? FrozenSpecificationVersion =>
        _workflow.Gate1Approval?.ApprovedSpecVersion;

    public DateTimeOffset? GateOneReviewedAt => _workflow.Gate1Approval?.ReviewedAt;

    public string ElementsTitle { get; }

    public string RelationsTitle { get; }

    public string ProposalsTitle { get; }

    public string ProvenanceTitle { get; }

    public string GateOneTitle { get; }

    public string RiskLevelLabel { get; }

    public string BlockingReasonsTitle { get; }

    public string UnderstandingVersionLabel { get; }

    public string SpecificationVersionLabel { get; }

    public string FrozenVersionsTitle { get; }

    public string ReviewerLabel { get; }

    public string NotesLabel { get; }

    public string AcceptProposalText { get; }

    public string RejectProposalText { get; }

    public string ApproveGateOneText { get; }

    public IRelayCommand AcceptProposalCommand { get; }

    public IRelayCommand RejectProposalCommand { get; }

    public IRelayCommand ApproveGateOneCommand { get; }

    public ScientificFigureProposalRowViewModel? SelectedProposal
    {
        get => _selectedProposal;
        set
        {
            if (SetProperty(ref _selectedProposal, value))
            {
                AcceptProposalCommand.NotifyCanExecuteChanged();
                RejectProposalCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string GateOneReviewer
    {
        get => _gateOneReviewer;
        set
        {
            if (SetProperty(ref _gateOneReviewer, value))
            {
                ApproveGateOneCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string GateOneNotes
    {
        get => _gateOneNotes;
        set
        {
            if (SetProperty(ref _gateOneNotes, value))
            {
                ApproveGateOneCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool HasSelectedProposal()
    {
        return SelectedProposal is not null;
    }

    private void AcceptSelectedProposal()
    {
        SelectedProposal!.Decide(ScientificProposalDecision.Accepted);
    }

    private void RejectSelectedProposal()
    {
        SelectedProposal!.Decide(ScientificProposalDecision.Rejected);
    }

    private void RefreshProposalState()
    {
        RefreshGateOneState();
    }

    private void RefreshGateOneState()
    {
        var reasons = _workflow.Specification.BlockingCodes.ToList();
        if (Proposals.Any(proposal => proposal.Decision == ScientificProposalDecision.Pending))
        {
            reasons.Add("proposal-decision-required");
        }

        if (Proposals.Any(proposal => proposal.Decision == ScientificProposalDecision.Accepted))
        {
            reasons.Add("accepted-proposal-requires-spec-revision");
        }

        GateOneBlockingReasons = reasons.Distinct(StringComparer.Ordinal).ToArray();
        CanApproveGateOne = IsDomainEligible
            && !IsGateOneApproved
            && GateOneBlockingReasons.Count == 0;
        OnPropertyChanged(nameof(GateOneBlockingReasons));
        OnPropertyChanged(nameof(CanApproveGateOne));
        ApproveGateOneCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteGateOneApproval()
    {
        return CanApproveGateOne
            && !string.IsNullOrWhiteSpace(GateOneReviewer)
            && !string.IsNullOrWhiteSpace(GateOneNotes);
    }

    private void ApproveGateOne()
    {
        _workflow = _workflow.ApproveGate1(
            GateOneReviewer.Trim(),
            GateOneNotes.Trim(),
            _clock());
        RefreshGateOneState();
        OnPropertyChanged(nameof(IsGateOneApproved));
        OnPropertyChanged(nameof(FrozenUnderstandingVersion));
        OnPropertyChanged(nameof(FrozenSpecificationVersion));
        OnPropertyChanged(nameof(GateOneReviewedAt));
    }

    private static void ValidateProposals(
        ScientificFigureSpec specification,
        IReadOnlyList<ScientificFigureSpecProposalDiff> proposals)
    {
        if (proposals.Any(proposal => proposal is null))
        {
            throw new ArgumentException("Proposal diffs cannot contain null entries.", nameof(proposals));
        }

        if (proposals.Any(proposal => proposal.ProposalId == Guid.Empty)
            || proposals.Select(proposal => proposal.ProposalId).Distinct().Count() != proposals.Count)
        {
            throw new ArgumentException("Proposal diffs require unique, non-empty ids.", nameof(proposals));
        }

        foreach (var proposal in proposals)
        {
            if (!Enum.IsDefined(proposal.TargetKind) || !Enum.IsDefined(proposal.Field)
                || string.IsNullOrWhiteSpace(proposal.TargetId)
                || string.IsNullOrWhiteSpace(proposal.ProposedValue)
                || string.IsNullOrWhiteSpace(proposal.Rationale))
            {
                throw new ArgumentException("Proposal diffs require a typed target and complete values.", nameof(proposals));
            }

            var currentValue = ResolveCurrentValue(specification, proposal);
            if (!string.Equals(currentValue, proposal.CurrentValue, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Proposal '{proposal.ProposalId}' does not match the current specification value.",
                    nameof(proposals));
            }
        }
    }

    private static string ResolveCurrentValue(
        ScientificFigureSpec specification,
        ScientificFigureSpecProposalDiff proposal)
    {
        return proposal.TargetKind switch
        {
            ScientificFigureSpecTargetKind.Element => ResolveElementValue(
                specification.Elements.SingleOrDefault(item =>
                    string.Equals(item.ElementId, proposal.TargetId, StringComparison.Ordinal))
                ?? throw new ArgumentException(
                    $"Proposal target element was not found: {proposal.TargetId}"),
                proposal.Field),
            ScientificFigureSpecTargetKind.Relation => ResolveRelationValue(
                specification.Relations.SingleOrDefault(item =>
                    string.Equals(item.RelationId, proposal.TargetId, StringComparison.Ordinal))
                ?? throw new ArgumentException(
                    $"Proposal target relation was not found: {proposal.TargetId}"),
                proposal.Field),
            _ => throw new ArgumentOutOfRangeException(
                nameof(proposal), proposal.TargetKind, "Unsupported proposal target kind."),
        };
    }

    private static string ResolveElementValue(
        FigureElementSpec element,
        ScientificFigureSpecField field)
    {
        return field switch
        {
            ScientificFigureSpecField.ScientificMeaning => element.ScientificMeaning,
            ScientificFigureSpecField.ExactContent => element.LabelOrFormula ?? string.Empty,
            ScientificFigureSpecField.Requirement => element.Requirement.ToString(),
            _ => throw new ArgumentException(
                $"Field '{field}' is not valid for an element proposal."),
        };
    }

    private static string ResolveRelationValue(
        FigureRelationSpec relation,
        ScientificFigureSpecField field)
    {
        return field switch
        {
            ScientificFigureSpecField.ScientificMeaning => relation.ScientificMeaning,
            ScientificFigureSpecField.ExactContent => relation.Label ?? string.Empty,
            ScientificFigureSpecField.Requirement => relation.Requirement.ToString(),
            ScientificFigureSpecField.RelationDirection => relation.Direction.ToString(),
            ScientificFigureSpecField.RepresentationConstraint => relation.RepresentationConstraint,
            _ => throw new ArgumentException(
                $"Field '{field}' is not valid for a relation proposal."),
        };
    }

    private static ScientificFigureSpecItemRowViewModel MapElement(FigureElementSpec element)
    {
        return MapItem(
            ScientificFigureSpecTargetKind.Element,
            element.ElementId,
            element.ScientificMeaning,
            element.Kind.ToString(),
            element.LabelOrFormula,
            element.Requirement,
            element.IsCritical,
            element.Provenance);
    }

    private static ScientificFigureSpecItemRowViewModel MapRelation(FigureRelationSpec relation)
    {
        return MapItem(
            ScientificFigureSpecTargetKind.Relation,
            relation.RelationId,
            relation.ScientificMeaning,
            $"{relation.Kind}: {relation.SourceElementId} -> {relation.TargetElementId}",
            relation.Label,
            relation.Requirement,
            relation.IsCritical,
            relation.Provenance);
    }

    private static ScientificFigureSpecItemRowViewModel MapItem(
        ScientificFigureSpecTargetKind targetKind,
        string itemId,
        string scientificMeaning,
        string kind,
        string? exactContent,
        FigureContentRequirement requirement,
        bool isCritical,
        ScientificFigureProvenance? provenance)
    {
        return new ScientificFigureSpecItemRowViewModel(
            targetKind,
            itemId,
            scientificMeaning,
            kind,
            exactContent,
            requirement,
            isCritical,
            provenance?.ClaimId,
            provenance?.Evidence?.SourceBlockId,
            provenance?.Evidence?.QuotedText,
            provenance?.Evidence?.Location.PageNumber,
            provenance?.Evidence?.Location.Section,
            provenance?.ConventionId,
            provenance?.ConventionStatement);
    }
}

public sealed record ScientificFigureSpecProposalDiff(
    Guid ProposalId,
    ScientificFigureSpecTargetKind TargetKind,
    string TargetId,
    ScientificFigureSpecField Field,
    string CurrentValue,
    string ProposedValue,
    string Rationale);

public sealed class ScientificFigureProposalRowViewModel : ObservableObject
{
    private readonly Action _decisionChanged;
    private ScientificProposalDecision _decision;

    public ScientificFigureProposalRowViewModel(
        ScientificFigureSpecProposalDiff proposal,
        Action decisionChanged)
    {
        Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
        _decisionChanged = decisionChanged ?? throw new ArgumentNullException(nameof(decisionChanged));
    }

    public ScientificFigureSpecProposalDiff Proposal { get; }

    public Guid ProposalId => Proposal.ProposalId;

    public ScientificFigureSpecTargetKind TargetKind => Proposal.TargetKind;

    public string TargetId => Proposal.TargetId;

    public ScientificFigureSpecField Field => Proposal.Field;

    public string CurrentValue => Proposal.CurrentValue;

    public string ProposedValue => Proposal.ProposedValue;

    public string Rationale => Proposal.Rationale;

    public ScientificProposalDecision Decision
    {
        get => _decision;
        private set => SetProperty(ref _decision, value);
    }

    public void Decide(ScientificProposalDecision decision)
    {
        if (!Enum.IsDefined(decision) || decision == ScientificProposalDecision.Pending)
        {
            throw new ArgumentOutOfRangeException(nameof(decision), decision, "A proposal requires an explicit decision.");
        }

        Decision = decision;
        _decisionChanged();
    }
}

public sealed record ScientificFigureSpecItemRowViewModel(
    ScientificFigureSpecTargetKind TargetKind,
    string ItemId,
    string ScientificMeaning,
    string Kind,
    string? ExactContent,
    FigureContentRequirement Requirement,
    bool IsCritical,
    string? ClaimId,
    string? SourceBlockId,
    string? ExactQuote,
    int? PageNumber,
    string? Section,
    string? ConventionId,
    string? ConventionStatement);

public sealed record ScientificFigureIssueRowViewModel(
    string IssueId,
    ScientificFigureIssueKind Kind,
    string Description,
    ScientificFigureIssueStatus Status,
    string? Resolution);

public enum ScientificFigureSpecTargetKind
{
    Element = 0,
    Relation = 1,
}

public enum ScientificProposalDecision
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}

public enum ScientificFigureSpecField
{
    ScientificMeaning = 0,
    ExactContent = 1,
    Requirement = 2,
    RelationDirection = 3,
    RepresentationConstraint = 4,
}
