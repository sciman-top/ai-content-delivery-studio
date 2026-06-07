using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.App.ViewModels;

public sealed class PlanningWorkflowCoordinator
{
    private readonly ProjectApplicationService _projectService;
    private readonly LocalizationService _localizationService;

    public PlanningWorkflowCoordinator(
        ProjectApplicationService projectService,
        LocalizationService localizationService)
    {
        _projectService = projectService;
        _localizationService = localizationService;
    }

    public async Task<PlanningWorkflowResult> RunFakePlanningAsync(
        Guid projectId,
        string planningGoal,
        string planningAudience,
        int itemCount,
        string planningStyleBrief,
        CancellationToken cancellationToken)
    {
        var series = await _projectService.CreatePlanWithProviderAsync(
            projectId,
            new PlanningRequest(
                planningGoal.Trim(),
                planningAudience.Trim(),
                itemCount,
                planningStyleBrief.Trim()),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return new PlanningWorkflowResult(series.Id);
    }

    public async Task<DocumentPlanningWorkflowResult> RunFakeDocumentPlanningAsync(
        Guid projectId,
        string projectName,
        string sourceText,
        string? audience,
        IllustrationStrictnessLevel strictness,
        string defaultDocumentAudience,
        CancellationToken cancellationToken)
    {
        var resolvedAudience = string.IsNullOrWhiteSpace(audience)
            ? defaultDocumentAudience
            : audience.Trim();

        var result = await _projectService.CreateDocumentIllustrationPlanWithProviderAsync(
            projectId,
            new DocumentIllustrationPlanningRequest(
                projectName,
                sourceText.Trim(),
                resolvedAudience,
                MapDocumentFamily(strictness),
                strictness,
                [_localizationService.GetText(LocalizationKey.DocumentPastedSourceSection)],
                [sourceText.Trim()],
                [_localizationService.GetText(LocalizationKey.DocumentReadableTextConstraint)]),
            approveAllTargets: true,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var resultSummary = string.Format(
            _localizationService.GetText(LocalizationKey.DocumentPlanningResultTemplate),
            result.ApprovedTargetCount);

        return new DocumentPlanningWorkflowResult(
            result.SeriesId,
            resultSummary);
    }

    private static DocumentFamily MapDocumentFamily(IllustrationStrictnessLevel strictness)
    {
        return strictness switch
        {
            IllustrationStrictnessLevel.Editorial => DocumentFamily.Editorial,
            IllustrationStrictnessLevel.ScholarlyDraft => DocumentFamily.ScholarlyDraft,
            _ => DocumentFamily.Educational,
        };
    }
}

public sealed record PlanningWorkflowResult(Guid SeriesId);

public sealed record DocumentPlanningWorkflowResult(
    Guid? SeriesId,
    string ResultSummary);
