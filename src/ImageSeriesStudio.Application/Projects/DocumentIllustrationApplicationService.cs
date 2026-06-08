using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Application.Projects;

public sealed class DocumentIllustrationApplicationService
{
    private readonly IProjectRepository _repository;
    private readonly ITextPlanningProvider? _textPlanningProvider;

    public DocumentIllustrationApplicationService(
        IProjectRepository repository,
        ITextPlanningProvider? textPlanningProvider)
    {
        _repository = repository;
        _textPlanningProvider = textPlanningProvider;
    }

    public async Task<DocumentIllustrationWorkflowResult> CreateDocumentIllustrationPlanWithProviderAsync(
        Guid projectId,
        DocumentIllustrationPlanningRequest request,
        bool approveAllTargets,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        ValidatePlanningRequest(request);

        if (_textPlanningProvider is null)
        {
            throw new InvalidOperationException("Text planning provider is not registered.");
        }

        if (!_textPlanningProvider.Capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Real document illustration planning requires explicit approval.");
        }

        if (request.DocumentFamily is DocumentFamily.ScholarlyDraft
            && request.KnownConstraints.Any(value => value.Contains("fake evidence", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Scholarly draft planning cannot request fake evidence imagery.");
        }

        var project = await RequireProjectAsync(projectId, cancellationToken);
        var providerResult = await _textPlanningProvider.CreateDocumentIllustrationPlanAsync(request, cancellationToken);
        var brief = RebindDocumentBrief(project.Id, providerResult.Brief, timestamp);
        var plan = RebindIllustrationPlan(project.Id, brief.Id, providerResult.Plan, timestamp);

        if (approveAllTargets)
        {
            foreach (var target in plan.Targets)
            {
                plan = plan.ApproveTarget(target.Id, timestamp);
            }
        }

        project.AddDocumentBrief(brief, timestamp);
        project.AddIllustrationPlan(plan, timestamp);

        var approvedTargets = plan.ApprovedTargets;
        ImageSeries? series = null;
        if (approvedTargets.Count > 0)
        {
            var providerProfile = ResolveProviderProfile(project, timestamp);
            series = project.AddSeries(
                $"Document illustrations: {brief.Title}",
                plan.Summary,
                timestamp);

            foreach (var target in approvedTargets)
            {
                var item = series.AddItem(
                    target.Title,
                    BuildDocumentTargetBrief(target),
                    timestamp);
                item.AddPromptVersion(
                    BuildDocumentTargetPrompt(target),
                    CreateDefaultGenerationSettings(),
                    providerProfile.Id,
                    timestamp);
            }
        }

        await _repository.SaveAsync(project, cancellationToken);
        return new DocumentIllustrationWorkflowResult(
            brief.Id,
            plan.Id,
            series?.Id,
            approvedTargets.Count);
    }

    private async Task<ImageProject> RequireProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _repository.LoadAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project not found: {projectId}");
    }

    private static DocumentBrief RebindDocumentBrief(
        Guid projectId,
        DocumentBrief source,
        DateTimeOffset timestamp)
    {
        return DocumentBrief.Create(
            projectId,
            source.SourceKind,
            source.SourceDisplayName,
            source.Title,
            source.DocumentFamily,
            source.Audience,
            source.Sections,
            source.KeyClaims,
            source.VisualOpportunities,
            source.KnownConstraints,
            source.StrictnessLevel,
            timestamp);
    }

    private static IllustrationPlan RebindIllustrationPlan(
        Guid projectId,
        Guid documentBriefId,
        IllustrationPlan source,
        DateTimeOffset timestamp)
    {
        var targets = source.Targets
            .Select(target => IllustrationTarget.Create(
                documentBriefId,
                target.Title,
                target.DocumentLocation,
                target.Purpose,
                target.MustShow,
                target.MustNotShow,
                target.SourceEvidence,
                target.SuggestedImageTypePresetId,
                target.SuggestedReviewRubricTemplateId,
                target.TextPolicy,
                target.StrictnessNotes,
                timestamp))
            .ToArray();

        return IllustrationPlan.Create(
            projectId,
            documentBriefId,
            source.Summary,
            targets,
            source.CoverageNotes,
            source.RiskNotes,
            timestamp);
    }

    private static ProviderProfile ResolveProviderProfile(ImageProject project, DateTimeOffset timestamp)
    {
        var existing = project.ProviderProfiles.FirstOrDefault(profile => profile.Kind == ProviderKind.Fake);
        return existing ?? project.AddProviderProfile("Fake provider", ProviderKind.Fake, timestamp);
    }

    private static GenerationSettings CreateDefaultGenerationSettings()
    {
        return new GenerationSettings(1024, 1024, "standard", "png");
    }

    private static string BuildDocumentTargetBrief(IllustrationTarget target)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Purpose: {target.Purpose}",
                $"Location: {target.DocumentLocation}",
                "Must show:",
                FormatList(target.MustShow),
                "Must not show:",
                FormatList(target.MustNotShow),
                "Source evidence:",
                FormatList(target.SourceEvidence),
                "Strictness:",
                FormatList(target.StrictnessNotes),
                $"Text policy: {target.TextPolicy}",
            ]);
    }

    private static string BuildDocumentTargetPrompt(IllustrationTarget target)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Create a document illustration titled \"{target.Title}\" for {target.DocumentLocation}.",
                $"Purpose: {target.Purpose}.",
                "Must show:",
                FormatList(target.MustShow),
                "Must not show:",
                FormatList(target.MustNotShow),
                "Source evidence:",
                FormatList(target.SourceEvidence),
                $"Text policy: {target.TextPolicy}. Use deterministic post-render text when required for labels, captions, or callouts.",
                "Do not imply real experimental, clinical, archival, or field evidence unless the user provided that evidence explicitly.",
            ]);
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count == 0
            ? "- None specified."
            : string.Join(Environment.NewLine, values.Select(value => $"- {value}"));
    }

    private static void ValidatePlanningRequest(DocumentIllustrationPlanningRequest request)
    {
        if (request.SourceText.Length > DocumentIllustrationExecutionPolicy.DefaultMaxSourceTextCharacters)
        {
            throw new InvalidOperationException(
                $"Document illustration planning exceeds the bounded local-direct default of {DocumentIllustrationExecutionPolicy.DefaultMaxSourceTextCharacters} source-text characters. Summarize or chunk the source locally before dispatch.");
        }

        var evidenceRowCount = request.Sections.Count + request.KeyClaims.Count + request.KnownConstraints.Count;
        if (evidenceRowCount > DocumentIllustrationExecutionPolicy.DefaultMaxEvidenceRows)
        {
            throw new InvalidOperationException(
                $"Document illustration planning contains {evidenceRowCount} evidence rows, which exceeds the local review-prep default of {DocumentIllustrationExecutionPolicy.DefaultMaxEvidenceRows}. Summarize or select a smaller source-evidence subset before dispatch.");
        }
    }
}
