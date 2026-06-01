namespace ImageSeriesStudio.Core.Styles;

public sealed record GenerationRecipe
{
    private GenerationRecipe(
        Guid id,
        Guid providerProfileId,
        string modelId,
        string imageTypePresetId,
        int width,
        int height,
        string quality,
        string outputFormat,
        ImageBackgroundMode background,
        int? compression,
        ImageModerationMode moderation,
        int? seed,
        IReadOnlyList<string> capabilityWarnings)
    {
        Id = id;
        ProviderProfileId = providerProfileId;
        ModelId = modelId;
        ImageTypePresetId = imageTypePresetId;
        Width = width;
        Height = height;
        Quality = quality;
        OutputFormat = outputFormat;
        Background = background;
        Compression = compression;
        Moderation = moderation;
        Seed = seed;
        CapabilityWarnings = capabilityWarnings;
    }

    public Guid Id { get; }

    public Guid ProviderProfileId { get; }

    public string ModelId { get; }

    public string ImageTypePresetId { get; }

    public int Width { get; }

    public int Height { get; }

    public string Quality { get; }

    public string OutputFormat { get; }

    public ImageBackgroundMode Background { get; }

    public int? Compression { get; }

    public ImageModerationMode Moderation { get; }

    public int? Seed { get; }

    public IReadOnlyList<string> CapabilityWarnings { get; }

    public static GenerationRecipe Create(
        Guid providerProfileId,
        string modelId,
        string imageTypePresetId,
        int width,
        int height,
        string quality,
        string outputFormat,
        ImageBackgroundMode background,
        int? compression,
        ImageModerationMode moderation,
        int? seed,
        IReadOnlyList<string> capabilityWarnings)
    {
        if (providerProfileId == Guid.Empty)
        {
            throw new ArgumentException("Provider profile id cannot be empty.", nameof(providerProfileId));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        if (compression is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(compression), "Compression must be between 0 and 100.");
        }

        return new GenerationRecipe(
            Guid.NewGuid(),
            providerProfileId,
            RequireText(modelId, nameof(modelId)),
            RequireText(imageTypePresetId, nameof(imageTypePresetId)),
            width,
            height,
            RequireText(quality, nameof(quality)).ToLowerInvariant(),
            RequireText(outputFormat, nameof(outputFormat)).ToLowerInvariant(),
            background,
            compression,
            moderation,
            seed,
            NormalizeWarnings(capabilityWarnings));
    }

    private static IReadOnlyList<string> NormalizeWarnings(IReadOnlyList<string> capabilityWarnings)
    {
        return capabilityWarnings
            .Select(warning => warning.Trim())
            .Where(warning => warning.Length > 0)
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

public enum ImageBackgroundMode
{
    Auto = 0,
    Opaque = 1,
    Transparent = 2,
}

public enum ImageModerationMode
{
    Auto = 0,
    Low = 1,
    Strict = 2,
}
