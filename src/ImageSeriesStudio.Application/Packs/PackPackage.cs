using System.Text.Json.Serialization;
using ImageSeriesStudio.Core.Packs;

namespace ImageSeriesStudio.Application.Packs;

public sealed record PackPackage(
    string SchemaVersion,
    string Name,
    DateTimeOffset ExportedAt,
    IReadOnlyList<WorkflowPack> WorkflowPacks,
    IReadOnlyList<BlueprintPack> BlueprintPacks,
    IReadOnlyList<IndustryPack> IndustryPacks,
    IReadOnlyList<RendererPack> RendererPacks,
    IReadOnlyList<ReviewRubricPack> ReviewRubricPacks)
{
    public const string CurrentSchemaVersion = "pack-package.v1";

    [JsonIgnore]
    public IReadOnlyList<IPackDefinition> Packs
    {
        get
        {
            var packs = new List<IPackDefinition>();
            packs.AddRange(WorkflowPacks);
            packs.AddRange(BlueprintPacks);
            packs.AddRange(IndustryPacks);
            packs.AddRange(RendererPacks);
            packs.AddRange(ReviewRubricPacks);
            return packs;
        }
    }

    public static PackPackage FromRegistry(
        string name,
        DateTimeOffset exportedAt,
        PackRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return Create(
            name,
            exportedAt,
            registry.Packs.OfType<WorkflowPack>().ToArray(),
            registry.Packs.OfType<BlueprintPack>().ToArray(),
            registry.Packs.OfType<IndustryPack>().ToArray(),
            registry.Packs.OfType<RendererPack>().ToArray(),
            registry.Packs.OfType<ReviewRubricPack>().ToArray(),
            registry.AppVersion.ToString());
    }

    public static PackPackage Create(
        string name,
        DateTimeOffset exportedAt,
        IReadOnlyList<WorkflowPack> workflowPacks,
        IReadOnlyList<BlueprintPack> blueprintPacks,
        IReadOnlyList<IndustryPack> industryPacks,
        IReadOnlyList<RendererPack> rendererPacks,
        IReadOnlyList<ReviewRubricPack> reviewRubricPacks,
        string appVersion)
    {
        ArgumentNullException.ThrowIfNull(workflowPacks);
        ArgumentNullException.ThrowIfNull(blueprintPacks);
        ArgumentNullException.ThrowIfNull(industryPacks);
        ArgumentNullException.ThrowIfNull(rendererPacks);
        ArgumentNullException.ThrowIfNull(reviewRubricPacks);

        var package = new PackPackage(
            CurrentSchemaVersion,
            RequireText(name, nameof(name)),
            exportedAt,
            workflowPacks.Select(NormalizeWorkflowPack).ToArray(),
            blueprintPacks.Select(NormalizeBlueprintPack).ToArray(),
            industryPacks.Select(NormalizeIndustryPack).ToArray(),
            rendererPacks.Select(NormalizeRendererPack).ToArray(),
            reviewRubricPacks.Select(NormalizeReviewRubricPack).ToArray());

        _ = package.CreateRegistry(appVersion);
        return package;
    }

    public PackPackage ValidateForAppVersion(string appVersion)
    {
        return Create(
            Name,
            ExportedAt,
            WorkflowPacks,
            BlueprintPacks,
            IndustryPacks,
            RendererPacks,
            ReviewRubricPacks,
            appVersion);
    }

    public PackPackage ValidateForExport()
    {
        return ValidateForAppVersion(SelectExportValidationAppVersion(Packs));
    }

    public PackRegistry CreateRegistry(string appVersion)
    {
        return PackRegistry.Create(appVersion, Packs);
    }

    private static WorkflowPack NormalizeWorkflowPack(WorkflowPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return WorkflowPack.CreateWithStagesAndUiDefaults(
            pack.Metadata.Id,
            pack.Metadata.DisplayName,
            pack.Metadata.Version.ToString(),
            NormalizeCompatibility(pack.Metadata.Compatibility),
            pack.StageDefinitions.Select(NormalizeStageDefinition).ToArray(),
            pack.BlueprintPackIds,
            NormalizeUiDefaults(pack.UiDefaults),
            pack.Metadata.LifecycleState,
            pack.Metadata.MigrationNotes,
            pack.Metadata.CreatedAt);
    }

    private static WorkflowStageDefinition NormalizeStageDefinition(WorkflowStageDefinition stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return WorkflowStageDefinition.Create(
            stage.Id,
            stage.DisplayName,
            stage.CompletionCriteria,
            stage.Required);
    }

    private static WorkflowPackUiDefaults NormalizeUiDefaults(WorkflowPackUiDefaults uiDefaults)
    {
        ArgumentNullException.ThrowIfNull(uiDefaults);

        return WorkflowPackUiDefaults.Create(
            uiDefaults.DefaultStageId,
            uiDefaults.ViewSlots
                .Select(slot => WorkflowViewSlotDefault.Create(
                    slot.SlotId,
                    slot.StageId,
                    slot.VisibleByDefault,
                    slot.Order))
                .ToArray());
    }

    private static BlueprintPack NormalizeBlueprintPack(BlueprintPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return BlueprintPack.Create(
            pack.Metadata.Id,
            pack.Metadata.DisplayName,
            pack.Metadata.Version.ToString(),
            NormalizeCompatibility(pack.Metadata.Compatibility),
            pack.BlueprintIds,
            pack.Metadata.LifecycleState,
            pack.Metadata.MigrationNotes,
            pack.Metadata.CreatedAt);
    }

    private static IndustryPack NormalizeIndustryPack(IndustryPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return IndustryPack.Create(
            pack.Metadata.Id,
            pack.Metadata.DisplayName,
            pack.Metadata.Version.ToString(),
            NormalizeCompatibility(pack.Metadata.Compatibility),
            pack.AudienceTags,
            pack.WorkflowPackIds,
            pack.Metadata.LifecycleState,
            pack.Metadata.MigrationNotes,
            pack.Metadata.CreatedAt);
    }

    private static RendererPack NormalizeRendererPack(RendererPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return RendererPack.Create(
            pack.Metadata.Id,
            pack.Metadata.DisplayName,
            pack.Metadata.Version.ToString(),
            NormalizeCompatibility(pack.Metadata.Compatibility),
            pack.OutputFormats,
            pack.Metadata.LifecycleState,
            pack.Metadata.MigrationNotes,
            pack.Metadata.CreatedAt);
    }

    private static ReviewRubricPack NormalizeReviewRubricPack(ReviewRubricPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return ReviewRubricPack.Create(
            pack.Metadata.Id,
            pack.Metadata.DisplayName,
            pack.Metadata.Version.ToString(),
            NormalizeCompatibility(pack.Metadata.Compatibility),
            pack.RubricTemplateIds,
            pack.Metadata.LifecycleState,
            pack.Metadata.MigrationNotes,
            pack.Metadata.CreatedAt);
    }

    private static PackCompatibilityRange NormalizeCompatibility(PackCompatibilityRange compatibility)
    {
        ArgumentNullException.ThrowIfNull(compatibility);

        return PackCompatibilityRange.Create(
            compatibility.MinimumAppVersion.ToString(),
            compatibility.MaximumAppVersion?.ToString());
    }

    private static string SelectExportValidationAppVersion(IReadOnlyList<IPackDefinition> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);

        var maximumMinimum = packs
            .Select(pack => pack.Metadata.Compatibility.MinimumAppVersion)
            .OrderByDescending(version => version.Major)
            .ThenByDescending(version => version.Minor)
            .ThenByDescending(version => version.Patch)
            .FirstOrDefault();

        return (maximumMinimum ?? PackVersion.Parse("1.0.0")).ToString();
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
