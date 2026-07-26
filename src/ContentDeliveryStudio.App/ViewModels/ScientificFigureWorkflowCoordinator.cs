using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ScientificFigureWorkflowCoordinator
{
    private readonly LocalizationService _localizationService;

    public ScientificFigureWorkflowCoordinator(LocalizationService localizationService)
    {
        _localizationService = localizationService
            ?? throw new ArgumentNullException(nameof(localizationService));
    }

    public ScientificFigureWorkspaceProjection Build(ScientificFigureWorkflow? workflow)
    {
        return new ScientificFigureWorkspaceProjection(
            workflow?.Specification.SpecificationId,
            workflow?.Specification.Version,
            workflow?.State,
            BuildWorkspaces(workflow?.State));
    }

    private IReadOnlyList<ScientificWorkspaceSlot> BuildWorkspaces(
        ScientificFigureWorkflowState? state)
    {
        return
        [
            Slot(ScientificWorkspaceStage.Source, LocalizationKey.ScientificSource,
                state is null ? ScientificWorkspaceStatus.Pending : ScientificWorkspaceStatus.Complete),
            Slot(ScientificWorkspaceStage.Understanding, LocalizationKey.ScientificUnderstanding,
                state is null ? ScientificWorkspaceStatus.Pending : ScientificWorkspaceStatus.Complete),
            Slot(ScientificWorkspaceStage.FigureSpec, LocalizationKey.ScientificFigureSpec,
                FigureSpecStatus(state)),
            Slot(ScientificWorkspaceStage.RenderAndReview, LocalizationKey.ScientificRenderAndReview,
                RenderAndReviewStatus(state)),
            Slot(ScientificWorkspaceStage.Delivery, LocalizationKey.ScientificDelivery,
                state is ScientificFigureWorkflowState.ReviewPassed
                    ? ScientificWorkspaceStatus.Ready
                    : ScientificWorkspaceStatus.Pending),
        ];
    }

    private ScientificWorkspaceSlot Slot(
        ScientificWorkspaceStage stage,
        LocalizationKey titleKey,
        ScientificWorkspaceStatus status)
    {
        return new ScientificWorkspaceSlot(
            stage,
            Text(titleKey),
            status,
            Text(StatusKey(status)));
    }

    private static ScientificWorkspaceStatus FigureSpecStatus(
        ScientificFigureWorkflowState? state)
    {
        return state switch
        {
            null => ScientificWorkspaceStatus.Pending,
            ScientificFigureWorkflowState.FigureSpecDraft => ScientificWorkspaceStatus.NeedsApproval,
            _ => ScientificWorkspaceStatus.Complete,
        };
    }

    private static ScientificWorkspaceStatus RenderAndReviewStatus(
        ScientificFigureWorkflowState? state)
    {
        return state switch
        {
            ScientificFigureWorkflowState.Gate1Approved => ScientificWorkspaceStatus.Ready,
            ScientificFigureWorkflowState.Rendering => ScientificWorkspaceStatus.InProgress,
            ScientificFigureWorkflowState.ReviewPassed => ScientificWorkspaceStatus.Complete,
            _ => ScientificWorkspaceStatus.Pending,
        };
    }

    private static LocalizationKey StatusKey(ScientificWorkspaceStatus status)
    {
        return status switch
        {
            ScientificWorkspaceStatus.Pending => LocalizationKey.ScientificStatusPending,
            ScientificWorkspaceStatus.Complete => LocalizationKey.ScientificStatusComplete,
            ScientificWorkspaceStatus.NeedsApproval => LocalizationKey.ScientificStatusNeedsApproval,
            ScientificWorkspaceStatus.Ready => LocalizationKey.ScientificStatusReady,
            ScientificWorkspaceStatus.InProgress => LocalizationKey.ScientificStatusInProgress,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported workspace status."),
        };
    }

    private string Text(LocalizationKey key)
    {
        return _localizationService.GetText(key);
    }
}

public sealed record ScientificFigureWorkspaceProjection(
    Guid? SpecificationId,
    int? SpecificationVersion,
    ScientificFigureWorkflowState? AuthoritativeState,
    IReadOnlyList<ScientificWorkspaceSlot> Workspaces);

public sealed record ScientificWorkspaceSlot(
    ScientificWorkspaceStage Stage,
    string Title,
    ScientificWorkspaceStatus Status,
    string StatusText);

public enum ScientificWorkspaceStage
{
    Source = 0,
    Understanding = 1,
    FigureSpec = 2,
    RenderAndReview = 3,
    Delivery = 4,
}

public enum ScientificWorkspaceStatus
{
    Pending = 0,
    Complete = 1,
    NeedsApproval = 2,
    Ready = 3,
    InProgress = 4,
}
