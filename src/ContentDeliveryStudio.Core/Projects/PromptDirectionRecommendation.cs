using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Core.Projects;

public sealed record PromptDirectionRecommendation(
    string ImageTypePresetId,
    ImageTextPolicy TextPolicy,
    string StyleIntent,
    AspectRatio AspectRatio,
    int Width,
    int Height,
    string QualityBand,
    string OutputFormat,
    ImageBackgroundMode BackgroundMode,
    string ReviewRubricTemplateId,
    int DraftCount,
    int FinalCount,
    string RecommendationReason,
    double Confidence,
    IReadOnlyList<string> CapabilityWarnings,
    IReadOnlyList<string> NonExecutableSuggestions)
{
    public static PromptDirectionRecommendation Create(
        string imageTypePresetId,
        ImageTextPolicy textPolicy,
        string styleIntent,
        AspectRatio aspectRatio,
        int width,
        int height,
        string qualityBand,
        string outputFormat,
        ImageBackgroundMode backgroundMode,
        string reviewRubricTemplateId,
        int draftCount,
        int finalCount,
        string recommendationReason,
        double confidence,
        IReadOnlyList<string> capabilityWarnings,
        IReadOnlyList<string> nonExecutableSuggestions)
    {
        ArgumentNullException.ThrowIfNull(aspectRatio);
        _ = ImageTypePresetCatalog.GetById(RequireText(imageTypePresetId, nameof(imageTypePresetId)));
        _ = ReviewRubricTemplateCatalog.GetById(RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)));

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        if (draftCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(draftCount), "Draft count cannot be negative.");
        }

        if (finalCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(finalCount), "Final count must be greater than zero.");
        }

        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        return new PromptDirectionRecommendation(
            RequireText(imageTypePresetId, nameof(imageTypePresetId)),
            textPolicy,
            styleIntent.Trim(),
            aspectRatio,
            width,
            height,
            RequireText(qualityBand, nameof(qualityBand)).ToLowerInvariant(),
            RequireText(outputFormat, nameof(outputFormat)).ToLowerInvariant(),
            backgroundMode,
            RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)),
            draftCount,
            finalCount,
            RequireText(recommendationReason, nameof(recommendationReason)),
            confidence,
            NormalizeList(capabilityWarnings),
            NormalizeList(nonExecutableSuggestions));
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
