using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal sealed record OpenAiTaskModelRoute(
    string Preset,
    string Model,
    string ReasoningEffort,
    string Reason);

internal static class OpenAiTaskModelRouter
{
    internal const int ComplexSeriesItemCount = 12;
    internal const int LargeSeriesItemCount = 6;
    internal const int ComplexSeriesInputCharacters = 3600;
    internal const int LargeSeriesInputCharacters = 2400;
    internal const int ComplexDocumentInputWeight = 4500;
    internal const int ComplexDocumentEvidenceRows = 8;
    internal const int ComplexVisionSignals = 8;
    internal const int ModerateVisionSignals = 5;

    public static IReadOnlyList<string> AutoModels { get; } =
        ["gpt-5.6-sol", "gpt-5.6-terra"];

    public static IReadOnlyList<string> ModelsForCapabilities(
        OpenAiProviderOptions options,
        string fixedModel) =>
        options.TextRoutingMode is OpenAiTextRoutingMode.Auto
            ? AutoModels
            : [fixedModel];

    public static OpenAiTaskModelRoute ForPlanning(
        OpenAiProviderOptions options,
        PlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(options.TextPlanningModel, options.ReasoningEffort);
        }

        var inputCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        if (request.ItemCount >= ComplexSeriesItemCount
            || inputCharacters >= ComplexSeriesInputCharacters)
        {
            return Preset(TextProviderModelPresets.SolMedium, "complex-series-plan");
        }

        if (request.ItemCount >= LargeSeriesItemCount
            || inputCharacters >= LargeSeriesInputCharacters)
        {
            return Preset(TextProviderModelPresets.TerraXHigh, "large-series-plan");
        }

        return Preset(TextProviderModelPresets.TerraHigh, "routine-series-plan");
    }

    public static OpenAiTaskModelRoute ForDocumentPlanning(
        OpenAiProviderOptions options,
        DocumentIllustrationPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(options.TextPlanningModel, options.ReasoningEffort);
        }

        if (request.DocumentFamily is DocumentFamily.ScholarlyDraft
            || request.StrictnessLevel is IllustrationStrictnessLevel.ScholarlyDraft)
        {
            return Preset(TextProviderModelPresets.SolMedium, "scholarly-document-plan");
        }

        var evidenceRows = request.Sections.Count
            + request.KeyClaims.Count
            + request.KnownConstraints.Count;
        if (request.DocumentFamily is DocumentFamily.Educational
            && (DocumentIllustrationExecutionPolicy.EstimateInputWeight(request) >= ComplexDocumentInputWeight
                || evidenceRows >= ComplexDocumentEvidenceRows))
        {
            return Preset(TextProviderModelPresets.TerraXHigh, "complex-educational-document-plan");
        }

        return Preset(TextProviderModelPresets.TerraHigh, "routine-document-plan");
    }

    public static OpenAiTaskModelRoute ForScientificUnderstanding(
        OpenAiProviderOptions options,
        ScientificUnderstandingChunkRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        return options.TextRoutingMode is OpenAiTextRoutingMode.Fixed
            ? Fixed(options.TextPlanningModel, options.ReasoningEffort)
            : Preset(TextProviderModelPresets.TerraXHigh, "scientific-understanding-chunk");
    }

    public static OpenAiTaskModelRoute ForScientificSemanticReview(
        OpenAiProviderOptions options,
        ScientificSemanticReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(options.VisionReviewModel, options.ReasoningEffort);
        }

        return request.Specification.RiskLevel is ScientificFigureRiskLevel.High
            ? Preset(TextProviderModelPresets.SolXHigh, "high-risk-scientific-semantic-review")
            : Preset(TextProviderModelPresets.SolMedium, "scientific-semantic-review");
    }

    public static OpenAiTaskModelRoute ForScientificVisualReview(
        OpenAiProviderOptions options,
        ScientificVisualReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        return options.TextRoutingMode is OpenAiTextRoutingMode.Fixed
            ? Fixed(options.VisionReviewModel, options.ReasoningEffort)
            : Preset(TextProviderModelPresets.SolXHigh, "full-resolution-scientific-visual-review");
    }

    public static OpenAiTaskModelRoute ForVisionReview(
        OpenAiProviderOptions options,
        VisionReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(options.VisionReviewModel, options.ReasoningEffort);
        }

        var signals = request.Rubric.Dimensions.Count
            + (request.ReviewPrep?.EvidenceSelections.Count ?? 0);
        if (signals >= ComplexVisionSignals)
        {
            return Preset(TextProviderModelPresets.SolMedium, "complex-vision-review");
        }

        if (signals >= ModerateVisionSignals)
        {
            return Preset(TextProviderModelPresets.TerraXHigh, "moderate-vision-review");
        }

        return Preset(TextProviderModelPresets.TerraHigh, "routine-vision-review");
    }

    private static OpenAiTaskModelRoute Fixed(string model, string reasoningEffort) =>
        new("fixed", model, reasoningEffort, "fixed-provider-configuration");

    private static OpenAiTaskModelRoute Preset(string preset, string reason)
    {
        if (!TextProviderModelPresets.TryResolve(preset, out var model, out var reasoningEffort))
        {
            throw new InvalidOperationException($"Text provider preset '{preset}' is not registered.");
        }

        return new OpenAiTaskModelRoute(preset, model, reasoningEffort, reason);
    }
}
