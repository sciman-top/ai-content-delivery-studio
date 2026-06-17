namespace ContentDeliveryStudio.Core.Packs;

public sealed class PackRegistry
{
    private readonly Dictionary<string, IPackDefinition> _packsById;

    private PackRegistry(PackVersion appVersion, IReadOnlyList<IPackDefinition> packs)
    {
        AppVersion = appVersion;
        Packs = packs;
        _packsById = packs.ToDictionary(pack => pack.Metadata.Id, StringComparer.OrdinalIgnoreCase);
    }

    public PackVersion AppVersion { get; }

    public IReadOnlyList<IPackDefinition> Packs { get; }

    public static PackRegistry Create(string appVersion, IReadOnlyList<IPackDefinition> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);

        var parsedAppVersion = PackVersion.Parse(appVersion);
        var normalizedPacks = packs.ToArray();
        ValidateUniqueIds(normalizedPacks);
        ValidateCompatibility(parsedAppVersion, normalizedPacks);
        ValidateReferences(normalizedPacks);
        ValidateWorkflowUiDefaults(normalizedPacks);

        return new PackRegistry(parsedAppVersion, normalizedPacks);
    }

    public TPack GetRequired<TPack>(string packId)
        where TPack : class, IPackDefinition
    {
        var normalizedId = PackMetadata.NormalizeId(packId, nameof(packId));
        if (!_packsById.TryGetValue(normalizedId, out var pack))
        {
            throw new InvalidOperationException($"Pack not found: {normalizedId}");
        }

        return pack as TPack
            ?? throw new InvalidOperationException($"Pack has unexpected type: {normalizedId}");
    }

    private static void ValidateUniqueIds(IReadOnlyList<IPackDefinition> packs)
    {
        var duplicate = packs
            .GroupBy(pack => pack.Metadata.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate pack id: {duplicate}", nameof(packs));
        }
    }

    private static void ValidateCompatibility(
        PackVersion appVersion,
        IReadOnlyList<IPackDefinition> packs)
    {
        foreach (var pack in packs)
        {
            var compatibility = pack.Metadata.Compatibility;
            var isBelowMinimum = appVersion.CompareTo(compatibility.MinimumAppVersion) < 0;
            var isAboveMaximum = compatibility.MaximumAppVersion is { } maximum
                && appVersion.CompareTo(maximum) > 0;
            if (isBelowMinimum || isAboveMaximum)
            {
                throw new InvalidOperationException(
                    $"Pack is not compatible with app version {appVersion}: {pack.Metadata.Id}");
            }
        }
    }

    private static void ValidateReferences(IReadOnlyList<IPackDefinition> packs)
    {
        var blueprintPackIds = packs
            .OfType<BlueprintPack>()
            .Select(pack => pack.Metadata.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var workflow in packs.OfType<WorkflowPack>())
        {
            foreach (var blueprintPackId in workflow.BlueprintPackIds)
            {
                if (!blueprintPackIds.Contains(blueprintPackId))
                {
                    throw new InvalidOperationException(
                        $"Workflow pack references missing blueprint pack: {workflow.Metadata.Id} -> {blueprintPackId}");
                }
            }
        }
    }

    private static void ValidateWorkflowUiDefaults(IReadOnlyList<IPackDefinition> packs)
    {
        foreach (var workflow in packs.OfType<WorkflowPack>())
        {
            var stageIds = workflow.StageIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!stageIds.Contains(workflow.UiDefaults.DefaultStageId))
            {
                throw new InvalidOperationException(
                    $"Workflow pack UI defaults reference missing default stage: {workflow.Metadata.Id} -> {workflow.UiDefaults.DefaultStageId}");
            }

            foreach (var slot in workflow.UiDefaults.ViewSlots)
            {
                if (!stageIds.Contains(slot.StageId))
                {
                    throw new InvalidOperationException(
                        $"Workflow pack UI defaults reference missing stage: {workflow.Metadata.Id} -> {slot.SlotId} -> {slot.StageId}");
                }
            }
        }
    }
}
