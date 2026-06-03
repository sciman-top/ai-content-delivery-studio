using System.Text.Json.Serialization;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Projects;

public sealed class DesignBlueprint
{
    private DesignBlueprint()
    {
        Key = string.Empty;
        DisplayName = string.Empty;
        Category = string.Empty;
        Summary = string.Empty;
        IntendedUse = string.Empty;
        DefaultReviewRubricTemplateId = ReviewRubricTemplateCatalog.GeneralImage;
        ConsistencyRules = [];
        VariationRules = [];
        RiskNotes = [];
        RepairNotesJson = "[]";
    }

    [JsonConstructor]
    public DesignBlueprint(
        Guid id,
        string key,
        string displayName,
        string category,
        string summary,
        string intendedUse,
        int minimumRecommendedItemCount,
        int maximumRecommendedItemCount,
        bool supportsPanelSequence,
        ImageTextPolicy defaultTextPolicy,
        string defaultReviewRubricTemplateId,
        IReadOnlyList<string> consistencyRules,
        IReadOnlyList<string> variationRules,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string repairNotesJson)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Blueprint id cannot be empty.", nameof(id));
        }

        if (minimumRecommendedItemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumRecommendedItemCount),
                "Minimum item count must be positive.");
        }

        if (maximumRecommendedItemCount < minimumRecommendedItemCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumRecommendedItemCount),
                "Maximum item count must be greater than or equal to the minimum item count.");
        }

        Id = id;
        Key = RequireText(key, nameof(key));
        DisplayName = RequireText(displayName, nameof(displayName));
        Category = RequireText(category, nameof(category));
        Summary = RequireText(summary, nameof(summary));
        IntendedUse = RequireText(intendedUse, nameof(intendedUse));
        MinimumRecommendedItemCount = minimumRecommendedItemCount;
        MaximumRecommendedItemCount = maximumRecommendedItemCount;
        SupportsPanelSequence = supportsPanelSequence;
        DefaultTextPolicy = defaultTextPolicy;
        DefaultReviewRubricTemplateId = ValidateReviewRubricTemplate(defaultReviewRubricTemplateId);
        ConsistencyRules = NormalizeList(consistencyRules);
        VariationRules = NormalizeList(variationRules);
        RiskNotes = NormalizeList(riskNotes);
        RepairNotesJson = string.IsNullOrWhiteSpace(repairNotesJson) ? "[]" : repairNotesJson.Trim();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string DisplayName { get; private set; }

    public string Category { get; private set; }

    public string Summary { get; private set; }

    public string IntendedUse { get; private set; }

    public int MinimumRecommendedItemCount { get; private set; }

    public int MaximumRecommendedItemCount { get; private set; }

    public bool SupportsPanelSequence { get; private set; }

    public ImageTextPolicy DefaultTextPolicy { get; private set; }

    public string DefaultReviewRubricTemplateId { get; private set; }

    public IReadOnlyList<string> ConsistencyRules { get; private set; }

    public IReadOnlyList<string> VariationRules { get; private set; }

    public IReadOnlyList<string> RiskNotes { get; private set; }

    public string RepairNotesJson { get; private set; } = "[]";

    [JsonIgnore]
    public IReadOnlyList<RoutedRepairPatchApplicationNote> RepairNotes =>
        RoutedRepairPatchApplicationSerialization.DeserializeNotes(RepairNotesJson);

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static DesignBlueprint Create(
        string key,
        string displayName,
        string category,
        string summary,
        string intendedUse,
        int minimumRecommendedItemCount,
        int maximumRecommendedItemCount,
        bool supportsPanelSequence,
        ImageTextPolicy defaultTextPolicy,
        string defaultReviewRubricTemplateId,
        IReadOnlyList<string> consistencyRules,
        IReadOnlyList<string> variationRules,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset timestamp)
    {
        return new DesignBlueprint(
            Guid.NewGuid(),
            key,
            displayName,
            category,
            summary,
            intendedUse,
            minimumRecommendedItemCount,
            maximumRecommendedItemCount,
            supportsPanelSequence,
            defaultTextPolicy,
            defaultReviewRubricTemplateId,
            consistencyRules,
            variationRules,
            riskNotes,
            timestamp,
            timestamp,
            "[]");
    }

    public DesignBlueprint ApplyRoutedRepairPatch(
        IReadOnlyList<RoutedRepairPatchApplicationNote> notes,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(notes);

        if (notes.Count == 0)
        {
            throw new ArgumentException("At least one repair note is required.", nameof(notes));
        }

        if (notes.Any(note => note.TargetLayer is not ReviewOutcomeTargetLayer.Blueprint))
        {
            throw new ArgumentException("Blueprint repair notes must target the blueprint layer.", nameof(notes));
        }

        var patchId = notes[0].RepairPatchId;
        if (RepairNotes.Any(note => note.RepairPatchId == patchId))
        {
            throw new InvalidOperationException($"Blueprint repair patch already applied: {patchId}");
        }

        var combinedNotes = RepairNotes.Concat(notes).ToArray();
        return new DesignBlueprint(
            Id,
            Key,
            DisplayName,
            Category,
            Summary,
            IntendedUse,
            MinimumRecommendedItemCount,
            MaximumRecommendedItemCount,
            SupportsPanelSequence,
            DefaultTextPolicy,
            DefaultReviewRubricTemplateId,
            ConsistencyRules,
            VariationRules,
            RiskNotes,
            CreatedAt,
            timestamp,
            RoutedRepairPatchApplicationSerialization.SerializeNotes(combinedNotes));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string ValidateReviewRubricTemplate(string templateId)
    {
        var normalized = RequireText(templateId, nameof(templateId));
        _ = ReviewRubricTemplateCatalog.GetById(normalized);
        return normalized;
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
}
