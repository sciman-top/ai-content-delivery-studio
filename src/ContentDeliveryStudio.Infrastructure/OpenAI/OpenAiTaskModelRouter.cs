using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal sealed record OpenAiTaskModelRoute(
    string Preset,
    string Model,
    string ReasoningEffort,
    string Reason,
    OpenAiExecutionQualityTier QualityTier = OpenAiExecutionQualityTier.Balanced,
    string PresetSet = "");

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
        ["gpt-5.6-sol", "gpt-5.6-terra", "gpt-5.6-luna"];

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
        var inputCharacters = TextPlanningExecutionPolicy.EstimateInputCharacters(request);
        var isComplex = request.ItemCount >= ComplexSeriesItemCount
            || inputCharacters >= ComplexSeriesInputCharacters;
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(
                options.TextPlanningModel,
                options.ReasoningEffort,
                "complex-series-plan",
                isComplex);
        }

        if (isComplex)
        {
            return Preset(TextProviderModelPresets.SolHigh, "complex-series-plan");
        }

        if (request.ItemCount >= LargeSeriesItemCount
            || inputCharacters >= LargeSeriesInputCharacters)
        {
            return Preset(TextProviderModelPresets.SolMedium, "large-series-plan");
        }

        return Preset(TextProviderModelPresets.SolLow, "routine-series-plan");
    }

    public static OpenAiTaskModelRoute ForDocumentPlanning(
        OpenAiProviderOptions options,
        DocumentIllustrationPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        var isQualityFirst = request.DocumentFamily is DocumentFamily.ScholarlyDraft
            || request.StrictnessLevel is IllustrationStrictnessLevel.ScholarlyDraft;
        var evidenceRows = request.Sections.Count
            + request.KeyClaims.Count
            + request.KnownConstraints.Count;
        var isComplexEducational = request.DocumentFamily is DocumentFamily.Educational
            && (DocumentIllustrationExecutionPolicy.EstimateInputWeight(request) >= ComplexDocumentInputWeight
                || evidenceRows >= ComplexDocumentEvidenceRows);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(
                options.TextPlanningModel,
                options.ReasoningEffort,
                "quality-first-document-plan",
                isQualityFirst || isComplexEducational);
        }

        if (isQualityFirst)
        {
            return Preset(TextProviderModelPresets.SolHigh, "scholarly-document-plan");
        }

        if (isComplexEducational)
        {
            return Preset(TextProviderModelPresets.SolHigh, "complex-educational-document-plan");
        }

        return Preset(TextProviderModelPresets.SolLow, "routine-document-plan");
    }

    public static OpenAiTaskModelRoute ForScientificUnderstanding(
        OpenAiProviderOptions options,
        ScientificUnderstandingChunkRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        return options.TextRoutingMode is OpenAiTextRoutingMode.Fixed
            ? Fixed(
                options.TextPlanningModel,
                options.ReasoningEffort,
                "scientific-understanding",
                qualityFirst: true)
            : Preset(TextProviderModelPresets.SolHigh, "scientific-understanding-chunk");
    }

    public static OpenAiTaskModelRoute ForScientificSemanticReview(
        OpenAiProviderOptions options,
        ScientificSemanticReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(
                options.VisionReviewModel,
                options.ReasoningEffort,
                "scientific-semantic-review",
                qualityFirst: true);
        }

        return request.Specification.RiskLevel is ScientificFigureRiskLevel.High
            ? Preset(TextProviderModelPresets.SolHigh, "high-risk-scientific-semantic-review")
            : Preset(TextProviderModelPresets.SolHigh, "scientific-semantic-review");
    }

    public static OpenAiTaskModelRoute ForScientificVisualReview(
        OpenAiProviderOptions options,
        ScientificVisualReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        return options.TextRoutingMode is OpenAiTextRoutingMode.Fixed
            ? Fixed(
                options.VisionReviewModel,
                options.ReasoningEffort,
                "scientific-visual-review",
                qualityFirst: true)
            : Preset(TextProviderModelPresets.SolHigh, "full-resolution-scientific-visual-review");
    }

    public static OpenAiTaskModelRoute ForVisionReview(
        OpenAiProviderOptions options,
        VisionReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);
        var signals = request.Rubric.Dimensions.Count
            + (request.ReviewPrep?.EvidenceSelections.Count ?? 0);
        if (options.TextRoutingMode is OpenAiTextRoutingMode.Fixed)
        {
            return Fixed(
                options.VisionReviewModel,
                options.ReasoningEffort,
                "complex-vision-review",
                signals >= ComplexVisionSignals);
        }

        if (signals >= ComplexVisionSignals)
        {
            return Preset(TextProviderModelPresets.SolHigh, "complex-vision-review");
        }

        if (signals >= ModerateVisionSignals)
        {
            return Preset(TextProviderModelPresets.SolMedium, "moderate-vision-review");
        }

        return Preset(TextProviderModelPresets.SolLow, "routine-vision-review");
    }

    private static OpenAiTaskModelRoute Fixed(
        string model,
        string reasoningEffort,
        string workload,
        bool qualityFirst = false)
    {
        var isSolHigh = string.Equals(model, "gpt-5.6-sol", StringComparison.Ordinal)
            && string.Equals(reasoningEffort, "high", StringComparison.Ordinal);
        var reason = qualityFirst && !isSolHigh
            ? $"fixed-operator-override-{workload}"
            : "fixed-provider-configuration";
        var qualityTier = TextProviderModelPresets.TryGetQualityTierForModel(model, reasoningEffort, out var resolvedTier)
            ? resolvedTier
            : OpenAiExecutionQualityTier.Balanced;
        var presetSet = TextProviderModelPresets.TryGetFamily(model, out var family)
            && TextProviderModelPresetSets.TryGetPresetSetForFamily(family, out var resolvedPresetSet)
            ? resolvedPresetSet
            : string.Empty;
        return new OpenAiTaskModelRoute("fixed", model, reasoningEffort, reason, qualityTier, presetSet);
    }

    private static OpenAiTaskModelRoute Preset(string preset, string reason)
    {
        if (!TextProviderModelPresets.TryResolve(preset, out var model, out var reasoningEffort))
        {
            throw new InvalidOperationException($"Text provider preset '{preset}' is not registered.");
        }

        var qualityTier = TextProviderModelPresets.TryGetQualityTier(preset, out var resolvedTier)
            ? resolvedTier
            : OpenAiExecutionQualityTier.Balanced;
        var presetSet = TextProviderModelPresets.TryGetPresetSet(preset, out var resolvedPresetSet)
            ? resolvedPresetSet
            : string.Empty;
        return new OpenAiTaskModelRoute(preset, model, reasoningEffort, reason, qualityTier, presetSet);
    }
}
