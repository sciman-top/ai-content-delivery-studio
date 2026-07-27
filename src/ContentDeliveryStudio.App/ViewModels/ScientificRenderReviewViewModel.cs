using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificRenderReviewViewModel : ObservableObject
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly HashSet<string> AllowedSvgElements =
    [
        "svg", "title", "desc", "metadata", "defs", "marker", "path", "g", "rect", "text",
    ];
    private const double MinimumZoom = 0.5;
    private const double MaximumZoom = 2;
    private const double ZoomStep = 0.1;

    private readonly Action<ScientificRepairAction>? _automaticRepairRequested;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ScientificRepairApplicationService _repairService = new();
    private readonly ObservableCollection<ScientificRepairHistoryEntry> _repairHistory;
    private ScientificRepairLoopState _repairLoopState = ScientificRepairLoopState.Start();
    private ScientificSvgAuthorityItemRowViewModel? _selectedSvgItem;
    private ScientificProvenanceTraceViewModel? _selectedProvenance;
    private ScientificRepairActionRowViewModel? _selectedRepairAction;
    private double _zoomScale = 1;

    public ScientificRenderReviewViewModel(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureSpec specification,
        SvgRenderPlan renderPlan,
        ScientificSvgArtifact svg,
        ScientificContractReviewReport contractReport,
        ScientificMachineReviewDecision machineDecision,
        ScientificRepairPlan repairPlan,
        IReadOnlyList<ScientificRepairHistoryEntry> repairHistory,
        Action<ScientificRepairAction>? automaticRepairRequested = null,
        Func<DateTimeOffset>? clock = null,
        LocalizationService? localizationService = null)
    {
        ArgumentNullException.ThrowIfNull(understanding);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(renderPlan);
        ArgumentNullException.ThrowIfNull(svg);
        ArgumentNullException.ThrowIfNull(contractReport);
        ArgumentNullException.ThrowIfNull(machineDecision);
        ArgumentNullException.ThrowIfNull(repairPlan);
        ArgumentNullException.ThrowIfNull(repairHistory);
        ValidateAuthority(understanding, specification, renderPlan, svg);

        _automaticRepairRequested = automaticRepairRequested;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        SvgDocument = svg.Svg;
        CanvasWidth = renderPlan.Canvas.Width;
        CanvasHeight = renderPlan.Canvas.Height;
        SvgItems = BuildSvgItems(understanding, specification, renderPlan);
        ContractAdvisoryScore = contractReport.AdvisoryScore;
        ContractFindings = contractReport.HardFailures.Select(finding =>
            new ScientificReviewFindingRowViewModel(
                "Contract",
                finding.Code,
                finding.ResponsibleItemId,
                finding.Evidence,
                finding.RepairLayer.ToString(),
                IsHardFailure: true)).ToArray();
        SemanticFindings = MapMachineFindings(
            machineDecision.Blockers,
            ScientificReviewLayer.Semantic);
        VisualFindings = MapMachineFindings(
            machineDecision.Blockers,
            ScientificReviewLayer.Visual);
        CanProceedToGateTwo = contractReport.Passed && machineDecision.CanProceedToGate2;
        RepairActions = repairPlan.Actions.Select(action =>
            new ScientificRepairActionRowViewModel(action)).ToArray();
        _repairLoopState = RestoreRepairLoopState(repairHistory);
        _repairHistory = new ObservableCollection<ScientificRepairHistoryEntry>(repairHistory);
        RepairHistory = new ReadOnlyObservableCollection<ScientificRepairHistoryEntry>(_repairHistory);

        var localization = localizationService ?? new LocalizationService();
        PreviewTitle = localization.GetText(LocalizationKey.ScientificSvgPreview);
        AuthorityItemsTitle = localization.GetText(LocalizationKey.ScientificAuthorityItems);
        ProvenanceTitle = localization.GetText(LocalizationKey.ScientificSpecProvenance);
        ContractFindingsTitle = localization.GetText(LocalizationKey.ScientificContractFindings);
        SemanticFindingsTitle = localization.GetText(LocalizationKey.ScientificSemanticFindings);
        VisualFindingsTitle = localization.GetText(LocalizationKey.ScientificVisualFindings);
        RepairActionsTitle = localization.GetText(LocalizationKey.ScientificRepairActions);
        RepairHistoryTitle = localization.GetText(LocalizationKey.ScientificRepairHistory);
        RunAutomaticRepairText = localization.GetText(LocalizationKey.ScientificRunAutomaticRepair);

        ZoomInCommand = new RelayCommand(() => ZoomScale += ZoomStep);
        ZoomOutCommand = new RelayCommand(() => ZoomScale -= ZoomStep);
        RunAutomaticRepairCommand = new RelayCommand(
            RunAutomaticRepair,
            CanRunAutomaticRepair);
    }

    public string SvgDocument { get; }

    public int CanvasWidth { get; }

    public int CanvasHeight { get; }

    public IReadOnlyList<ScientificSvgAuthorityItemRowViewModel> SvgItems { get; }

    public IReadOnlyList<ScientificReviewFindingRowViewModel> ContractFindings { get; }

    public IReadOnlyList<ScientificReviewFindingRowViewModel> SemanticFindings { get; }

    public IReadOnlyList<ScientificReviewFindingRowViewModel> VisualFindings { get; }

    public IReadOnlyList<ScientificRepairActionRowViewModel> RepairActions { get; }

    public ReadOnlyObservableCollection<ScientificRepairHistoryEntry> RepairHistory { get; }

    public double ContractAdvisoryScore { get; }

    public bool CanProceedToGateTwo { get; }

    public int CompletedAutomaticAttempts => _repairLoopState.CompletedAutomaticAttempts;

    public string PreviewTitle { get; }

    public string AuthorityItemsTitle { get; }

    public string ProvenanceTitle { get; }

    public string ContractFindingsTitle { get; }

    public string SemanticFindingsTitle { get; }

    public string VisualFindingsTitle { get; }

    public string RepairActionsTitle { get; }

    public string RepairHistoryTitle { get; }

    public string RunAutomaticRepairText { get; }

    public IRelayCommand ZoomInCommand { get; }

    public IRelayCommand ZoomOutCommand { get; }

    public IRelayCommand RunAutomaticRepairCommand { get; }

    public double ZoomScale
    {
        get => _zoomScale;
        set => SetProperty(ref _zoomScale, Math.Clamp(value, MinimumZoom, MaximumZoom));
    }

    public ScientificSvgAuthorityItemRowViewModel? SelectedSvgItem
    {
        get => _selectedSvgItem;
        set
        {
            if (SetProperty(ref _selectedSvgItem, value))
            {
                SelectedProvenance = value?.Provenance;
            }
        }
    }

    public ScientificProvenanceTraceViewModel? SelectedProvenance
    {
        get => _selectedProvenance;
        private set => SetProperty(ref _selectedProvenance, value);
    }

    public ScientificRepairActionRowViewModel? SelectedRepairAction
    {
        get => _selectedRepairAction;
        set
        {
            if (SetProperty(ref _selectedRepairAction, value))
            {
                RunAutomaticRepairCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void SelectSvgItem(string specificationItemId)
    {
        SelectedSvgItem = SvgItems.SingleOrDefault(item => string.Equals(
            item.SpecificationItemId,
            specificationItemId,
            StringComparison.Ordinal))
            ?? throw new KeyNotFoundException(
                $"SVG authority item was not found: {specificationItemId}");
    }

    private bool CanRunAutomaticRepair()
    {
        return _automaticRepairRequested is not null
            && SelectedRepairAction?.ExecutionMode == ScientificRepairExecutionMode.Automatic
            && _repairLoopState.CompletedAutomaticAttempts
                < ScientificRepairLoopState.MaximumAutomaticAttempts;
    }

    private static ScientificRepairLoopState RestoreRepairLoopState(
        IReadOnlyList<ScientificRepairHistoryEntry> repairHistory)
    {
        if (repairHistory.Any(entry => entry is null))
        {
            throw new ArgumentException("Repair history cannot contain null entries.", nameof(repairHistory));
        }

        var automaticEntries = repairHistory
            .Where(entry => entry.ExecutionMode == ScientificRepairExecutionMode.Automatic)
            .OrderBy(entry => entry.Attempt)
            .ToArray();
        if (!automaticEntries.Select(entry => entry.Attempt)
            .SequenceEqual(Enumerable.Range(1, automaticEntries.Length)))
        {
            throw new ArgumentException(
                "Automatic repair history requires continuous attempt numbers starting at one.",
                nameof(repairHistory));
        }

        var state = ScientificRepairLoopState.Start();
        foreach (var entry in automaticEntries)
        {
            var action = new ScientificRepairAction(
                entry.FindingCode,
                entry.ResponsibleItemId,
                "Restored authorized repair history.",
                entry.Layer,
                entry.ExecutionMode);
            state = state.RecordAutomaticAttempt(ScientificRepairPlan.Create([action]));
        }

        return state;
    }

    private void RunAutomaticRepair()
    {
        if (!CanRunAutomaticRepair())
        {
            throw new InvalidOperationException(
                "Only an authorized presentation repair can run automatically.");
        }

        var action = SelectedRepairAction!.Action;
        var plan = ScientificRepairPlan.Create([action]);
        _automaticRepairRequested!(action);
        _repairLoopState = _repairService.RecordAutomaticAttempt(plan, _repairLoopState);
        _repairHistory.Add(new ScientificRepairHistoryEntry(
            _repairLoopState.CompletedAutomaticAttempts,
            action.FindingCode,
            action.ResponsibleItemId,
            action.Layer,
            action.ExecutionMode,
            _clock()));
        OnPropertyChanged(nameof(CompletedAutomaticAttempts));
        RunAutomaticRepairCommand.NotifyCanExecuteChanged();
    }

    private static void ValidateAuthority(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureSpec specification,
        SvgRenderPlan renderPlan,
        ScientificSvgArtifact svg)
    {
        var actualHash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(svg.Svg))).ToLowerInvariant()}";
        if (understanding.UnderstandingId != specification.UnderstandingId
            || understanding.Version != specification.UnderstandingVersion
            || renderPlan.SpecificationId != specification.SpecificationId
            || renderPlan.SpecificationVersion != specification.Version
            || svg.SpecificationId != specification.SpecificationId
            || svg.SpecificationVersion != specification.Version
            || !string.Equals(svg.PlanId, renderPlan.PlanId, StringComparison.Ordinal)
            || !string.Equals(svg.Sha256, actualHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Render review requires matching understanding, specification, plan, and SVG authority.");
        }

        ValidatePreviewSvg(svg.Svg);

        SvgRenderPlanValidator.ValidateOrThrow(
            renderPlan,
            specification.Elements.Select(item => item.ElementId).ToArray(),
            specification.Relations.Select(item => item.RelationId).ToArray());
    }

    private static void ValidatePreviewSvg(string svg)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        using var textReader = new StringReader(svg);
        using var reader = XmlReader.Create(textReader, settings);
        var document = XDocument.Load(reader, LoadOptions.None);
        if (document.Root?.Name != SvgNamespace + "svg"
            || document.Descendants().Any(element =>
                element.Name.Namespace != SvgNamespace
                || !AllowedSvgElements.Contains(element.Name.LocalName)
                || element.Attributes().Any(attribute =>
                    !attribute.IsNamespaceDeclaration
                    && (attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                        || attribute.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase)
                        || attribute.Name.LocalName.Equals("src", StringComparison.OrdinalIgnoreCase)
                        || attribute.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase)))))
        {
            throw new ArgumentException(
                "SVG preview contains markup outside the deterministic renderer contract.",
                nameof(svg));
        }
    }

    private static IReadOnlyList<ScientificSvgAuthorityItemRowViewModel> BuildSvgItems(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureSpec specification,
        SvgRenderPlan renderPlan)
    {
        var elementsById = specification.Elements.ToDictionary(
            item => item.ElementId,
            StringComparer.Ordinal);
        var relationsById = specification.Relations.ToDictionary(
            item => item.RelationId,
            StringComparer.Ordinal);
        var rows = renderPlan.Elements.Select(item =>
        {
            var specificationItem = elementsById[item.SourceSpecificationItemId];
            return new ScientificSvgAuthorityItemRowViewModel(
                item.RenderElementId,
                item.SourceSpecificationItemId,
                item.Kind.ToString(),
                item.ScientificMeaning,
                item.IsCritical,
                MapProvenance(understanding, specificationItem.Provenance));
        }).ToList();
        rows.AddRange(renderPlan.Connections.Select(item =>
        {
            var specificationItem = relationsById[item.SourceSpecificationItemId];
            return new ScientificSvgAuthorityItemRowViewModel(
                item.RenderConnectionId,
                item.SourceSpecificationItemId,
                item.Kind.ToString(),
                specificationItem.ScientificMeaning,
                item.IsCritical,
                MapProvenance(understanding, specificationItem.Provenance));
        }));
        return rows;
    }

    private static ScientificProvenanceTraceViewModel? MapProvenance(
        ScientificDocumentUnderstanding understanding,
        ScientificFigureProvenance? provenance)
    {
        if (provenance is null)
        {
            return null;
        }

        var claim = provenance.ClaimId is null
            ? null
            : understanding.Claims.Single(item => string.Equals(
                item.ClaimId,
                provenance.ClaimId,
                StringComparison.Ordinal));
        return new ScientificProvenanceTraceViewModel(
            provenance.Kind,
            provenance.ClaimId,
            claim?.NormalizedStatement,
            provenance.Evidence?.SourceBlockId,
            provenance.Evidence?.QuotedText,
            provenance.Evidence?.Location.PageNumber,
            provenance.Evidence?.Location.Section,
            provenance.ConventionId,
            provenance.ConventionStatement);
    }

    private static IReadOnlyList<ScientificReviewFindingRowViewModel> MapMachineFindings(
        IReadOnlyList<ScientificReviewBlocker> blockers,
        ScientificReviewLayer layer)
    {
        return blockers.Where(blocker => blocker.Layer == layer).Select(blocker =>
            new ScientificReviewFindingRowViewModel(
                layer.ToString(),
                blocker.Code,
                blocker.ResponsibleItemId,
                blocker.Evidence,
                blocker.FindingKind?.ToString(),
                IsHardFailure: true)).ToArray();
    }
}

public sealed record ScientificSvgAuthorityItemRowViewModel(
    string RenderItemId,
    string SpecificationItemId,
    string Kind,
    string ScientificMeaning,
    bool IsCritical,
    ScientificProvenanceTraceViewModel? Provenance);

public sealed record ScientificProvenanceTraceViewModel(
    ScientificProvenanceKind Kind,
    string? ClaimId,
    string? ClaimStatement,
    string? SourceBlockId,
    string? ExactQuote,
    int? PageNumber,
    string? Section,
    string? ConventionId,
    string? ConventionStatement);

public sealed record ScientificReviewFindingRowViewModel(
    string Layer,
    string Code,
    string ResponsibleItemId,
    string Evidence,
    string? RepairLayer,
    bool IsHardFailure);

public sealed class ScientificRepairActionRowViewModel
{
    public ScientificRepairActionRowViewModel(ScientificRepairAction action)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    internal ScientificRepairAction Action { get; }

    public string FindingCode => Action.FindingCode;

    public string ResponsibleItemId => Action.ResponsibleItemId;

    public string Evidence => Action.Evidence;

    public ScientificRepairLayer Layer => Action.Layer;

    public ScientificRepairExecutionMode ExecutionMode => Action.ExecutionMode;
}

public sealed record ScientificRepairHistoryEntry(
    int Attempt,
    string FindingCode,
    string ResponsibleItemId,
    ScientificRepairLayer Layer,
    ScientificRepairExecutionMode ExecutionMode,
    DateTimeOffset ExecutedAt);
