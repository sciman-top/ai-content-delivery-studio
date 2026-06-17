using System.Text.Json;

namespace ContentDeliveryStudio.Core.Projects;

public sealed record RoutedRepairPatchApplicationNote(
    Guid RepairPatchId,
    Guid CandidateImageId,
    int Order,
    ReviewOutcomeTargetLayer TargetLayer,
    RepairSeverity Severity,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ProposedChanges,
    DateTimeOffset AppliedAt)
{
    public static RoutedRepairPatchApplicationNote FromPatchItem(
        RoutedRepairPatch patch,
        RoutedRepairPatchItem item,
        DateTimeOffset appliedAt)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(item);

        return new RoutedRepairPatchApplicationNote(
            patch.Id,
            patch.CandidateImageId,
            item.Order,
            item.TargetLayer,
            item.Severity,
            item.Evidence,
            item.ProposedChanges,
            appliedAt);
    }
}

public sealed record RoutedRepairPatchApplicationResult(
    Guid RoutedRepairPatchId,
    Guid CreativeBriefId,
    Guid? DesignBlueprintId,
    int BriefNoteCount,
    int BlueprintNoteCount)
{
    public bool HasChanges => BriefNoteCount > 0 || BlueprintNoteCount > 0;
}

internal static class RoutedRepairPatchApplicationSerialization
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string SerializeNotes(IReadOnlyList<RoutedRepairPatchApplicationNote> notes)
    {
        return JsonSerializer.Serialize(notes, JsonOptions);
    }

    public static IReadOnlyList<RoutedRepairPatchApplicationNote> DeserializeNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<RoutedRepairPatchApplicationNote>>(json, JsonOptions) ?? [];
    }
}
