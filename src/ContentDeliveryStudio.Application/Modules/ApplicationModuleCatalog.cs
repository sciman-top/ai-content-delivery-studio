using ContentDeliveryStudio.Core.Packs;

namespace ContentDeliveryStudio.Application.Modules;

public sealed record ApplicationModuleDefinition(
    string Id,
    string DisplayName,
    string ApplicationFolder,
    string? CoreFolder,
    string? InfrastructureFolder,
    IReadOnlyList<string> UseCaseNames)
{
    public static ApplicationModuleDefinition Create(
        string id,
        string displayName,
        string applicationFolder,
        string? coreFolder,
        string? infrastructureFolder,
        IReadOnlyList<string> useCaseNames)
    {
        return new ApplicationModuleDefinition(
            NormalizeId(id, nameof(id)),
            RequireText(displayName, nameof(displayName)),
            NormalizeFolder(applicationFolder, nameof(applicationFolder)),
            NormalizeOptionalFolder(coreFolder, nameof(coreFolder)),
            NormalizeOptionalFolder(infrastructureFolder, nameof(infrastructureFolder)),
            NormalizeRequiredTextList(useCaseNames, nameof(useCaseNames)));
    }

    private static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Module id cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalFolder(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeFolder(value, parameterName);
    }

    private static string NormalizeFolder(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).Replace('\\', '/').Trim('/');
        if (Path.IsPathRooted(text) || Uri.TryCreate(text, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Module folders must be repository-relative.", parameterName);
        }

        var segments = text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Module folders cannot escape the repository.", parameterName);
        }

        return string.Join("/", segments);
    }

    private static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one use case name is required.", parameterName);
        }

        return normalized;
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

public sealed record FeatureViewModuleDefinition(
    string Id,
    string ApplicationModuleId,
    string ViewSlotId,
    string ViewTypeName,
    string ViewModelTypeName,
    IReadOnlyList<string> LocalizationKeys,
    IReadOnlyList<string> CommandNames,
    IReadOnlyList<string> FakeServiceTestNames)
{
    public static FeatureViewModuleDefinition Create(
        string id,
        string applicationModuleId,
        string viewSlotId,
        string viewTypeName,
        string viewModelTypeName,
        IReadOnlyList<string> localizationKeys,
        IReadOnlyList<string> commandNames,
        IReadOnlyList<string> fakeServiceTestNames)
    {
        var applicationModule = ApplicationModuleCatalog.GetRequired(applicationModuleId);

        return new FeatureViewModuleDefinition(
            NormalizeId(id, nameof(id)),
            applicationModule.Id,
            NormalizeViewSlotId(viewSlotId, nameof(viewSlotId)),
            RequireText(viewTypeName, nameof(viewTypeName)),
            RequireText(viewModelTypeName, nameof(viewModelTypeName)),
            NormalizeRequiredTextList(localizationKeys, nameof(localizationKeys)),
            NormalizeRequiredTextList(commandNames, nameof(commandNames)),
            NormalizeRequiredTextList(fakeServiceTestNames, nameof(fakeServiceTestNames)));
    }

    private static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Feature view module id cannot be empty.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeViewSlotId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName);
        var canonical = WorkflowViewSlotIds.AllowedIds.FirstOrDefault(id =>
            string.Equals(id, text, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
        {
            throw new ArgumentException(
                $"Workflow view slot id must be one of: {string.Join(", ", WorkflowViewSlotIds.AllowedIds)}.",
                parameterName);
        }

        return canonical;
    }

    private static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalized = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
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

public static class ApplicationModuleCatalog
{
    public static IReadOnlyList<ApplicationModuleDefinition> BuiltInModules { get; } = Create(
        [
            ApplicationModuleDefinition.Create(
                "source-ingestion",
                "Source Ingestion",
                "src/ContentDeliveryStudio.Application/Sources",
                "src/ContentDeliveryStudio.Core/Sources",
                "src/ContentDeliveryStudio.Infrastructure/Sources",
                ["SourceIngestion", "DocumentExtraction"]),
            ApplicationModuleDefinition.Create(
                "artifact-planning",
                "Artifact Planning",
                "src/ContentDeliveryStudio.Application/Artifacts",
                "src/ContentDeliveryStudio.Core/Artifacts",
                "src/ContentDeliveryStudio.Infrastructure/Artifacts",
                ["ArtifactPlanning"]),
            ApplicationModuleDefinition.Create(
                "pack-registry",
                "Pack Registry",
                "src/ContentDeliveryStudio.Application/Packs",
                "src/ContentDeliveryStudio.Core/Packs",
                "src/ContentDeliveryStudio.Infrastructure/Packs",
                ["PackPackage", "PackRegistry"]),
            ApplicationModuleDefinition.Create(
                "repair-routing",
                "Repair Routing",
                "src/ContentDeliveryStudio.Application/RepairRouting",
                "src/ContentDeliveryStudio.Core/Projects",
                null,
                [
                    "ReviewOutcomeRouting",
                    "PromptRepairSuggestion",
                    "RoutedRepairPatchProposal",
                    "RoutedRepairPatchApplication",
                ]),
            ApplicationModuleDefinition.Create(
                "tool-adapters",
                "Tool Adapters",
                "src/ContentDeliveryStudio.Application/ToolAdapters",
                null,
                null,
                ["ToolAdapterRegistry"]),
            ApplicationModuleDefinition.Create(
                "remote-workflows",
                "Remote Workflows",
                "src/ContentDeliveryStudio.Application/RemoteWorkflows",
                null,
                "src/ContentDeliveryStudio.Infrastructure/RemoteWorkflows",
                ["RemoteWorkflowEngineAdapter"]),
        ]);

    public static IReadOnlyList<ApplicationModuleDefinition> Create(
        IReadOnlyList<ApplicationModuleDefinition> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        var duplicate = modules
            .GroupBy(module => module.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate application module id: {duplicate}", nameof(modules));
        }

        return modules.ToArray();
    }

    public static ApplicationModuleDefinition GetRequired(string moduleId)
    {
        var normalizedId = ApplicationModuleDefinition.Create(
            moduleId,
            "Temporary",
            "src",
            null,
            null,
            ["Temporary"]).Id;

        return BuiltInModules.SingleOrDefault(module => string.Equals(module.Id, normalizedId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Application module not found: {normalizedId}");
    }
}
