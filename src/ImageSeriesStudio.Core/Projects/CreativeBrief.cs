using System.Text.Json.Serialization;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Projects;

public sealed class CreativeBrief
{
    private readonly List<PromptDirection> _promptDirections = [];
    private readonly List<DesignBlueprint> _designBlueprints = [];

    private CreativeBrief()
    {
        Goal = string.Empty;
        Audience = string.Empty;
        StyleIntent = string.Empty;
        MustInclude = [];
        MustAvoid = [];
    }

    private CreativeBrief(
        Guid id,
        Guid seriesId,
        string goal,
        string audience,
        ImageTextPolicy textPolicy,
        string styleIntent,
        IReadOnlyList<string> mustInclude,
        IReadOnlyList<string> mustAvoid,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeriesId = seriesId;
        Goal = RequireText(goal, nameof(goal));
        Audience = RequireText(audience, nameof(audience));
        TextPolicy = textPolicy;
        StyleIntent = styleIntent.Trim();
        MustInclude = NormalizeList(mustInclude);
        MustAvoid = NormalizeList(mustAvoid);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeriesId { get; private set; }

    public string Goal { get; private set; }

    public string Audience { get; private set; }

    public ImageTextPolicy TextPolicy { get; private set; }

    public string StyleIntent { get; private set; }

    public IReadOnlyList<string> MustInclude { get; private set; }

    public IReadOnlyList<string> MustAvoid { get; private set; }

    public IReadOnlyCollection<PromptDirection> PromptDirections => _promptDirections.AsReadOnly();

    public IReadOnlyCollection<DesignBlueprint> DesignBlueprints => _designBlueprints.AsReadOnly();

    public Guid? PromotedBlueprintId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CreativeBrief Create(
        Guid seriesId,
        string goal,
        string audience,
        ImageTextPolicy textPolicy,
        string styleIntent,
        IReadOnlyList<string> mustInclude,
        IReadOnlyList<string> mustAvoid,
        DateTimeOffset createdAt)
    {
        if (seriesId == Guid.Empty)
        {
            throw new ArgumentException("Series id cannot be empty.", nameof(seriesId));
        }

        return new CreativeBrief(
            Guid.NewGuid(),
            seriesId,
            goal,
            audience,
            textPolicy,
            styleIntent,
            mustInclude,
            mustAvoid,
            createdAt);
    }

    public void ReplaceDirections(IReadOnlyList<PromptDirection> directions, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(directions);

        var duplicateKey = directions
            .GroupBy(direction => direction.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateKey is not null)
        {
            throw new ArgumentException($"Duplicate prompt direction key: {duplicateKey}", nameof(directions));
        }

        _promptDirections.Clear();
        _promptDirections.AddRange(directions);
        UpdatedAt = timestamp;
    }

    public void ReplaceBlueprints(IReadOnlyList<DesignBlueprint> blueprints, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(blueprints);

        var duplicateKey = blueprints
            .GroupBy(blueprint => blueprint.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateKey is not null)
        {
            throw new ArgumentException($"Duplicate design blueprint key: {duplicateKey}", nameof(blueprints));
        }

        _designBlueprints.Clear();
        _designBlueprints.AddRange(blueprints);

        if (PromotedBlueprintId is { } promotedBlueprintId
            && _designBlueprints.All(blueprint => blueprint.Id != promotedBlueprintId))
        {
            PromotedBlueprintId = null;
        }

        UpdatedAt = timestamp;
    }

    public DesignBlueprint PromoteBlueprint(Guid blueprintId, DateTimeOffset timestamp)
    {
        if (blueprintId == Guid.Empty)
        {
            throw new ArgumentException("Blueprint id cannot be empty.", nameof(blueprintId));
        }

        var blueprint = _designBlueprints.SingleOrDefault(candidate => candidate.Id == blueprintId)
            ?? throw new InvalidOperationException($"Design blueprint not found: {blueprintId}");

        PromotedBlueprintId = blueprint.Id;
        UpdatedAt = timestamp;
        return blueprint;
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

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

public sealed class PromptDirection
{
    private PromptDirection()
    {
        Key = string.Empty;
        Name = string.Empty;
        IntendedUse = string.Empty;
        PromptText = string.Empty;
        NegativePrompt = string.Empty;
        Strength = string.Empty;
        Risk = string.Empty;
        Recommendation = null;
    }

    [JsonConstructor]
    public PromptDirection(
        Guid id,
        string key,
        string name,
        string intendedUse,
        string promptText,
        string negativePrompt,
        string strength,
        string risk,
        DateTimeOffset createdAt,
        PromptDirectionRecommendation? recommendation = null)
    {
        Id = id;
        Key = RequireText(key, nameof(key));
        Name = RequireText(name, nameof(name));
        IntendedUse = RequireText(intendedUse, nameof(intendedUse));
        PromptText = RequireText(promptText, nameof(promptText));
        NegativePrompt = negativePrompt.Trim();
        Strength = strength.Trim();
        Risk = risk.Trim();
        CreatedAt = createdAt;
        Recommendation = recommendation;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string Name { get; private set; }

    public string IntendedUse { get; private set; }

    public string PromptText { get; private set; }

    public string NegativePrompt { get; private set; }

    public string Strength { get; private set; }

    public string Risk { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public PromptDirectionRecommendation? Recommendation { get; private set; }

    public static PromptDirection Create(
        string key,
        string name,
        string intendedUse,
        string promptText,
        string negativePrompt,
        string strength,
        string risk,
        DateTimeOffset createdAt,
        PromptDirectionRecommendation? recommendation = null)
    {
        return new PromptDirection(
            Guid.NewGuid(),
            key,
            name,
            intendedUse,
            promptText,
            negativePrompt,
            strength,
            risk,
            createdAt,
            recommendation);
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
