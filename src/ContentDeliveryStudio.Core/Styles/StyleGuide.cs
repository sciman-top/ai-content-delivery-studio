namespace ContentDeliveryStudio.Core.Styles;

public sealed record StyleGuide
{
    private StyleGuide(
        Guid id,
        Guid seriesId,
        string name,
        IReadOnlyList<string> visualPrinciples,
        IReadOnlyList<string> palette,
        IReadOnlyList<string> lighting,
        IReadOnlyList<string> compositionRules,
        IReadOnlyList<string> lineOrTextureRules,
        IReadOnlyList<string> negativeConstraints,
        IReadOnlyList<Guid> referenceImageSetIds,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        SeriesId = seriesId;
        Name = name;
        VisualPrinciples = visualPrinciples;
        Palette = palette;
        Lighting = lighting;
        CompositionRules = compositionRules;
        LineOrTextureRules = lineOrTextureRules;
        NegativeConstraints = negativeConstraints;
        ReferenceImageSetIds = referenceImageSetIds;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid SeriesId { get; }

    public string Name { get; }

    public IReadOnlyList<string> VisualPrinciples { get; }

    public IReadOnlyList<string> Palette { get; }

    public IReadOnlyList<string> Lighting { get; }

    public IReadOnlyList<string> CompositionRules { get; }

    public IReadOnlyList<string> LineOrTextureRules { get; }

    public IReadOnlyList<string> NegativeConstraints { get; }

    public IReadOnlyList<Guid> ReferenceImageSetIds { get; }

    public int Version { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static StyleGuide Create(
        Guid seriesId,
        string name,
        IReadOnlyList<string> visualPrinciples,
        IReadOnlyList<string> palette,
        IReadOnlyList<string> lighting,
        IReadOnlyList<string> compositionRules,
        IReadOnlyList<string> lineOrTextureRules,
        IReadOnlyList<string> negativeConstraints,
        IReadOnlyList<Guid> referenceImageSetIds,
        DateTimeOffset timestamp)
    {
        return CreateVersion(
            Guid.NewGuid(),
            seriesId,
            name,
            visualPrinciples,
            palette,
            lighting,
            compositionRules,
            lineOrTextureRules,
            negativeConstraints,
            referenceImageSetIds,
            version: 1,
            createdAt: timestamp,
            updatedAt: timestamp);
    }

    public StyleGuide Revise(
        string name,
        IReadOnlyList<string> visualPrinciples,
        IReadOnlyList<string> palette,
        IReadOnlyList<string> lighting,
        IReadOnlyList<string> compositionRules,
        IReadOnlyList<string> lineOrTextureRules,
        IReadOnlyList<string> negativeConstraints,
        IReadOnlyList<Guid> referenceImageSetIds,
        DateTimeOffset timestamp)
    {
        return CreateVersion(
            Id,
            SeriesId,
            name,
            visualPrinciples,
            palette,
            lighting,
            compositionRules,
            lineOrTextureRules,
            negativeConstraints,
            referenceImageSetIds,
            Version + 1,
            CreatedAt,
            timestamp);
    }

    private static StyleGuide CreateVersion(
        Guid id,
        Guid seriesId,
        string name,
        IReadOnlyList<string> visualPrinciples,
        IReadOnlyList<string> palette,
        IReadOnlyList<string> lighting,
        IReadOnlyList<string> compositionRules,
        IReadOnlyList<string> lineOrTextureRules,
        IReadOnlyList<string> negativeConstraints,
        IReadOnlyList<Guid> referenceImageSetIds,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var normalizedVisualPrinciples = NormalizeRequiredList(visualPrinciples, nameof(visualPrinciples));

        return new StyleGuide(
            id,
            seriesId,
            RequireText(name, nameof(name)),
            normalizedVisualPrinciples,
            NormalizeOptionalList(palette),
            NormalizeOptionalList(lighting),
            NormalizeOptionalList(compositionRules),
            NormalizeOptionalList(lineOrTextureRules),
            NormalizeOptionalList(negativeConstraints),
            NormalizeReferenceIds(referenceImageSetIds),
            version,
            createdAt,
            updatedAt);
    }

    private static IReadOnlyList<string> NormalizeRequiredList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeOptionalList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeOptionalList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<Guid> NormalizeReferenceIds(IReadOnlyList<Guid> referenceImageSetIds)
    {
        return referenceImageSetIds
            .Where(id => id != Guid.Empty)
            .Distinct()
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
