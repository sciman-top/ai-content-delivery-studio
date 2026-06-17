namespace ContentDeliveryStudio.Core.Packs;

public sealed record PackVersion(int Major, int Minor, int Patch)
{
    public static PackVersion Parse(string value)
    {
        var text = RequireText(value, nameof(value));
        var parts = text.Split('.', StringSplitOptions.TrimEntries);

        if (parts.Length != 3
            || !int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || !int.TryParse(parts[2], out var patch)
            || major < 0
            || minor < 0
            || patch < 0)
        {
            throw new ArgumentException("Pack version must use semantic version format major.minor.patch.", nameof(value));
        }

        return new PackVersion(major, minor, patch);
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    internal int CompareTo(PackVersion other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0)
        {
            return major;
        }

        var minor = Minor.CompareTo(other.Minor);
        return minor != 0 ? minor : Patch.CompareTo(other.Patch);
    }

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record PackCompatibilityRange(
    PackVersion MinimumAppVersion,
    PackVersion? MaximumAppVersion)
{
    public static PackCompatibilityRange Create(string minimumAppVersion, string? maximumAppVersion)
    {
        var minimum = PackVersion.Parse(minimumAppVersion);
        var maximum = string.IsNullOrWhiteSpace(maximumAppVersion)
            ? null
            : PackVersion.Parse(maximumAppVersion);

        if (maximum is not null && maximum.CompareTo(minimum) < 0)
        {
            throw new ArgumentException("Maximum app version cannot be lower than minimum app version.", nameof(maximumAppVersion));
        }

        return new PackCompatibilityRange(minimum, maximum);
    }
}

public enum PackLifecycleState
{
    Active = 0,
    Deprecated = 1,
    Retired = 2,
}

public sealed record PackMetadata(
    string Id,
    string DisplayName,
    PackVersion Version,
    PackCompatibilityRange Compatibility,
    PackLifecycleState LifecycleState,
    IReadOnlyList<string> MigrationNotes,
    DateTimeOffset CreatedAt)
{
    public static PackMetadata Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(compatibility);

        var normalizedMigrationNotes = NormalizeOptionalTextList(migrationNotes);
        if (lifecycleState is PackLifecycleState.Deprecated or PackLifecycleState.Retired
            && normalizedMigrationNotes.Count == 0)
        {
            throw new ArgumentException("Deprecated or retired packs must include migration notes.", nameof(migrationNotes));
        }

        return new PackMetadata(
            NormalizeId(id, nameof(id)),
            PackVersion.RequireText(displayName, nameof(displayName)),
            PackVersion.Parse(version),
            compatibility,
            RequireDefinedLifecycleState(lifecycleState),
            normalizedMigrationNotes,
            createdAt);
    }

    internal static string NormalizeId(string value, string parameterName)
    {
        var text = PackVersion.RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Pack id cannot be empty.", parameterName);
        }

        return normalized;
    }

    internal static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeOptionalTextList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    internal static IReadOnlyList<string> NormalizeOptionalTextList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PackLifecycleState RequireDefinedLifecycleState(PackLifecycleState state)
    {
        return Enum.IsDefined(typeof(PackLifecycleState), state)
            ? state
            : throw new ArgumentOutOfRangeException(nameof(state), state, "Pack lifecycle state is not supported.");
    }
}

public interface IPackDefinition
{
    PackMetadata Metadata { get; }
}

public static class WorkflowViewSlotIds
{
    public const string SourceList = "SourceList";

    public const string StageWorkspace = "StageWorkspace";

    public const string Inspector = "Inspector";

    public const string ActivityPanel = "ActivityPanel";

    public const string ApprovalPanel = "ApprovalPanel";

    public const string ArtifactPreview = "ArtifactPreview";

    public static IReadOnlySet<string> AllowedIds { get; } = new HashSet<string>(
        [
            SourceList,
            StageWorkspace,
            Inspector,
            ActivityPanel,
            ApprovalPanel,
            ArtifactPreview,
        ],
        StringComparer.OrdinalIgnoreCase);

    internal static string Normalize(string value, string parameterName)
    {
        var text = PackVersion.RequireText(value, parameterName);
        var canonical = AllowedIds.FirstOrDefault(id => string.Equals(id, text, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
        {
            throw new ArgumentException(
                $"Workflow view slot id must be one of: {string.Join(", ", AllowedIds)}.",
                parameterName);
        }

        return canonical;
    }
}

public sealed record WorkflowViewSlotDefault(
    string SlotId,
    string StageId,
    bool VisibleByDefault,
    int Order)
{
    public static WorkflowViewSlotDefault Create(
        string slotId,
        string stageId,
        bool visibleByDefault,
        int order)
    {
        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "View slot order cannot be negative.");
        }

        return new WorkflowViewSlotDefault(
            WorkflowViewSlotIds.Normalize(slotId, nameof(slotId)),
            PackVersion.RequireText(stageId, nameof(stageId)),
            visibleByDefault,
            order);
    }
}

public sealed record WorkflowPackUiDefaults(
    string DefaultStageId,
    IReadOnlyList<WorkflowViewSlotDefault> ViewSlots)
{
    public static WorkflowPackUiDefaults Create(
        string defaultStageId,
        IReadOnlyList<WorkflowViewSlotDefault> viewSlots)
    {
        ArgumentNullException.ThrowIfNull(viewSlots);

        if (viewSlots.Count == 0)
        {
            throw new ArgumentException("At least one workflow view slot is required.", nameof(viewSlots));
        }

        var normalizedViewSlots = viewSlots
            .OrderBy(slot => slot.Order)
            .ThenBy(slot => slot.SlotId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicate = normalizedViewSlots
            .GroupBy(slot => (slot.SlotId, slot.StageId), new WorkflowViewSlotDefaultKeyComparer())
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate workflow view slot default: {duplicate.Value.SlotId} -> {duplicate.Value.StageId}",
                nameof(viewSlots));
        }

        return new WorkflowPackUiDefaults(
            PackVersion.RequireText(defaultStageId, nameof(defaultStageId)),
            normalizedViewSlots);
    }

    internal static WorkflowPackUiDefaults CreateDefault(IReadOnlyList<WorkflowStageDefinition> stageDefinitions)
    {
        ArgumentNullException.ThrowIfNull(stageDefinitions);

        var firstStageId = stageDefinitions.First().Id;
        var reviewStageId = stageDefinitions.FirstOrDefault(
            stage => string.Equals(stage.Id, "Review", StringComparison.OrdinalIgnoreCase))?.Id ?? firstStageId;
        var deliverStageId = stageDefinitions.FirstOrDefault(
            stage => string.Equals(stage.Id, "Deliver", StringComparison.OrdinalIgnoreCase))?.Id ?? firstStageId;

        return Create(
            firstStageId,
            [
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.SourceList, firstStageId, visibleByDefault: true, order: 0),
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.StageWorkspace, firstStageId, visibleByDefault: true, order: 10),
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.Inspector, reviewStageId, visibleByDefault: true, order: 20),
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.ActivityPanel, firstStageId, visibleByDefault: true, order: 30),
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.ApprovalPanel, reviewStageId, visibleByDefault: false, order: 40),
                WorkflowViewSlotDefault.Create(WorkflowViewSlotIds.ArtifactPreview, deliverStageId, visibleByDefault: false, order: 50),
            ]);
    }

    private sealed class WorkflowViewSlotDefaultKeyComparer : IEqualityComparer<(string SlotId, string StageId)>
    {
        public bool Equals((string SlotId, string StageId) x, (string SlotId, string StageId) y)
        {
            return string.Equals(x.SlotId, y.SlotId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.StageId, y.StageId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string SlotId, string StageId) obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SlotId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.StageId));
        }
    }
}

public sealed record WorkflowStageDefinition(
    string Id,
    string DisplayName,
    IReadOnlyList<string> CompletionCriteria,
    bool Required)
{
    public static WorkflowStageDefinition Create(
        string id,
        string displayName,
        IReadOnlyList<string> completionCriteria,
        bool required)
    {
        return new WorkflowStageDefinition(
            NormalizeStageId(id, nameof(id)),
            PackVersion.RequireText(displayName, nameof(displayName)),
            PackMetadata.NormalizeRequiredTextList(completionCriteria, nameof(completionCriteria)),
            required);
    }

    private static string NormalizeStageId(string value, string parameterName)
    {
        return PackVersion.RequireText(value, parameterName);
    }
}

public sealed record WorkflowPack(
    PackMetadata Metadata,
    IReadOnlyList<WorkflowStageDefinition> StageDefinitions,
    IReadOnlyList<string> BlueprintPackIds,
    IReadOnlyList<string> ScenarioIds,
    IReadOnlyList<string> IndustryPackIds,
    IReadOnlyList<string> RendererPackIds,
    IReadOnlyList<string> ReviewRubricPackIds,
    WorkflowPackUiDefaults UiDefaults) : IPackDefinition
{
    public IReadOnlyList<string> StageIds => StageDefinitions.Select(stage => stage.Id).ToArray();

    public static WorkflowPack Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<string> stageIds,
        IReadOnlyList<string> blueprintPackIds,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt,
        IReadOnlyList<string>? scenarioIds = null,
        IReadOnlyList<string>? industryPackIds = null,
        IReadOnlyList<string>? rendererPackIds = null,
        IReadOnlyList<string>? reviewRubricPackIds = null)
    {
        var stageDefinitions = CreateDefaultStageDefinitions(stageIds);

        return new WorkflowPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            stageDefinitions,
            NormalizePackIds(blueprintPackIds),
            NormalizeScenarioIds(id, scenarioIds),
            NormalizePackIds(industryPackIds ?? []),
            NormalizePackIds(rendererPackIds ?? []),
            NormalizePackIds(reviewRubricPackIds ?? []),
            WorkflowPackUiDefaults.CreateDefault(stageDefinitions));
    }

    public static WorkflowPack CreateWithStages(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<WorkflowStageDefinition> stageDefinitions,
        IReadOnlyList<string> blueprintPackIds,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt,
        IReadOnlyList<string>? scenarioIds = null,
        IReadOnlyList<string>? industryPackIds = null,
        IReadOnlyList<string>? rendererPackIds = null,
        IReadOnlyList<string>? reviewRubricPackIds = null)
    {
        var normalizedStageDefinitions = NormalizeStageDefinitions(stageDefinitions);

        return new WorkflowPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            normalizedStageDefinitions,
            NormalizePackIds(blueprintPackIds),
            NormalizeScenarioIds(id, scenarioIds),
            NormalizePackIds(industryPackIds ?? []),
            NormalizePackIds(rendererPackIds ?? []),
            NormalizePackIds(reviewRubricPackIds ?? []),
            WorkflowPackUiDefaults.CreateDefault(normalizedStageDefinitions));
    }

    public static WorkflowPack CreateWithStagesAndUiDefaults(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<WorkflowStageDefinition> stageDefinitions,
        IReadOnlyList<string> blueprintPackIds,
        WorkflowPackUiDefaults uiDefaults,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt,
        IReadOnlyList<string>? scenarioIds = null,
        IReadOnlyList<string>? industryPackIds = null,
        IReadOnlyList<string>? rendererPackIds = null,
        IReadOnlyList<string>? reviewRubricPackIds = null)
    {
        ArgumentNullException.ThrowIfNull(uiDefaults);

        return new WorkflowPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            NormalizeStageDefinitions(stageDefinitions),
            NormalizePackIds(blueprintPackIds),
            NormalizeScenarioIds(id, scenarioIds),
            NormalizePackIds(industryPackIds ?? []),
            NormalizePackIds(rendererPackIds ?? []),
            NormalizePackIds(reviewRubricPackIds ?? []),
            uiDefaults);
    }

    private static IReadOnlyList<WorkflowStageDefinition> CreateDefaultStageDefinitions(IReadOnlyList<string> stageIds)
    {
        return PackMetadata.NormalizeRequiredTextList(stageIds, nameof(stageIds))
            .Select(stageId => WorkflowStageDefinition.Create(
                stageId,
                stageId,
                [$"{stageId} stage is complete."],
                required: true))
            .ToArray();
    }

    private static IReadOnlyList<WorkflowStageDefinition> NormalizeStageDefinitions(IReadOnlyList<WorkflowStageDefinition> stageDefinitions)
    {
        ArgumentNullException.ThrowIfNull(stageDefinitions);

        if (stageDefinitions.Count == 0)
        {
            throw new ArgumentException("At least one workflow stage is required.", nameof(stageDefinitions));
        }

        var duplicate = stageDefinitions
            .GroupBy(stage => stage.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate workflow stage id: {duplicate}", nameof(stageDefinitions));
        }

        return stageDefinitions.ToArray();
    }

    private static IReadOnlyList<string> NormalizePackIds(IReadOnlyList<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return ids
            .Select(id => string.IsNullOrWhiteSpace(id) ? string.Empty : PackMetadata.NormalizeId(id, nameof(ids)))
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> NormalizeScenarioIds(string workflowId, IReadOnlyList<string>? scenarioIds)
    {
        if (scenarioIds is null || scenarioIds.Count == 0)
        {
            return [PackMetadata.NormalizeId(workflowId, nameof(workflowId))];
        }

        return NormalizePackIds(scenarioIds);
    }
}

public sealed record BlueprintPack(
    PackMetadata Metadata,
    IReadOnlyList<string> BlueprintIds) : IPackDefinition
{
    public static BlueprintPack Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<string> blueprintIds,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt)
    {
        return new BlueprintPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            PackMetadata.NormalizeRequiredTextList(blueprintIds, nameof(blueprintIds)));
    }
}

public sealed record IndustryPack(
    PackMetadata Metadata,
    IReadOnlyList<string> AudienceTags,
    IReadOnlyList<string> WorkflowPackIds) : IPackDefinition
{
    public static IndustryPack Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<string> audienceTags,
        IReadOnlyList<string> workflowPackIds,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt)
    {
        return new IndustryPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            PackMetadata.NormalizeRequiredTextList(audienceTags, nameof(audienceTags)),
            PackMetadata.NormalizeRequiredTextList(workflowPackIds, nameof(workflowPackIds))
                .Select(id => PackMetadata.NormalizeId(id, nameof(workflowPackIds)))
                .ToArray());
    }
}

public sealed record RendererPack(
    PackMetadata Metadata,
    IReadOnlyList<string> OutputFormats) : IPackDefinition
{
    public static RendererPack Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<string> outputFormats,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt)
    {
        return new RendererPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            PackMetadata.NormalizeRequiredTextList(outputFormats, nameof(outputFormats))
                .Select(format => format.ToLowerInvariant())
                .ToArray());
    }
}

public sealed record ReviewRubricPack(
    PackMetadata Metadata,
    IReadOnlyList<string> RubricTemplateIds) : IPackDefinition
{
    public static ReviewRubricPack Create(
        string id,
        string displayName,
        string version,
        PackCompatibilityRange compatibility,
        IReadOnlyList<string> rubricTemplateIds,
        PackLifecycleState lifecycleState,
        IReadOnlyList<string> migrationNotes,
        DateTimeOffset createdAt)
    {
        return new ReviewRubricPack(
            PackMetadata.Create(id, displayName, version, compatibility, lifecycleState, migrationNotes, createdAt),
            PackMetadata.NormalizeRequiredTextList(rubricTemplateIds, nameof(rubricTemplateIds)));
    }
}
