using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed class ScientificRepairApplicationService
{
    public ScientificRepairPlan CreatePlan(
        ScientificContractReviewReport contractReport,
        ScientificMachineReviewDecision machineDecision,
        IReadOnlyList<ScientificRepairAction>? upstreamActions = null)
    {
        ArgumentNullException.ThrowIfNull(contractReport);
        ArgumentNullException.ThrowIfNull(machineDecision);
        var actions = new List<ScientificRepairAction>(upstreamActions ?? []);
        actions.AddRange(contractReport.HardFailures.Select(finding => RouteFinding(
            finding.Code,
            finding.ResponsibleItemId,
            finding.Evidence,
            MapContractLayer(finding.RepairLayer))));
        actions.AddRange(machineDecision.Blockers.Select(blocker => RouteFinding(
            blocker.Code,
            blocker.ResponsibleItemId,
            blocker.Evidence,
            MapMachineLayer(blocker))));
        return ScientificRepairPlan.Create(actions);
    }

    public ScientificRepairAction RouteFinding(
        string code,
        string responsibleItemId,
        string evidence,
        ScientificRepairLayer layer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsibleItemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence);
        if (!Enum.IsDefined(layer))
        {
            throw new ArgumentOutOfRangeException(nameof(layer));
        }

        var mode = layer is ScientificRepairLayer.LayoutStyle
            or ScientificRepairLayer.NonEvidentiaryAsset
            ? ScientificRepairExecutionMode.Automatic
            : ScientificRepairExecutionMode.HumanRequired;
        return new ScientificRepairAction(
            code.Trim(),
            responsibleItemId.Trim(),
            evidence.Trim(),
            layer,
            mode);
    }

    public ScientificRepairLoopState RecordAutomaticAttempt(
        ScientificRepairPlan plan,
        ScientificRepairLoopState state)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(state);
        return state.RecordAutomaticAttempt(plan);
    }

    public ScientificFigureWorkflow ApplyScientificRevision(
        ScientificFigureWorkflow workflow,
        ScientificDocumentUnderstanding understanding,
        string centralMessage,
        IReadOnlyList<FigureElementSpec> elements,
        IReadOnlyList<FigureRelationSpec> relations,
        IReadOnlyList<ScientificFigureIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        return workflow.ReviseScientificContent(
            understanding,
            centralMessage,
            elements,
            relations,
            issues);
    }

    private static ScientificRepairLayer MapContractLayer(
        ScientificContractRepairLayer layer)
    {
        return layer switch
        {
            ScientificContractRepairLayer.FigureSpecification =>
                ScientificRepairLayer.FigureSpecification,
            ScientificContractRepairLayer.RenderPlanCompiler =>
                ScientificRepairLayer.FigureSpecification,
            ScientificContractRepairLayer.SvgRenderer =>
                ScientificRepairLayer.SvgRenderer,
            ScientificContractRepairLayer.Exporter =>
                ScientificRepairLayer.Exporter,
            _ => throw new ArgumentOutOfRangeException(nameof(layer)),
        };
    }

    private static ScientificRepairLayer MapMachineLayer(
        ScientificReviewBlocker blocker)
    {
        return blocker.FindingKind switch
        {
            ScientificProviderFindingKind.VisualDefect =>
                ScientificRepairLayer.LayoutStyle,
            ScientificProviderFindingKind.NonEvidentiaryAssetDefect =>
                ScientificRepairLayer.NonEvidentiaryAsset,
            ScientificProviderFindingKind.ScientificMismatch =>
                ScientificRepairLayer.ScientificUnderstanding,
            ScientificProviderFindingKind.MissingElement =>
                ScientificRepairLayer.SvgRenderer,
            _ when blocker.Layer == ScientificReviewLayer.Semantic =>
                ScientificRepairLayer.ScientificUnderstanding,
            _ => ScientificRepairLayer.SvgRenderer,
        };
    }
}
