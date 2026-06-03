using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Application.ToolAdapters;

public interface IToolAdapter
{
    ToolAdapterDescriptor Descriptor { get; }

    Task<ToolAdapterRunResult> RunAsync(
        ToolAdapterRunRequest request,
        CancellationToken cancellationToken);
}

public sealed record ToolAdapterDescriptor(
    string Id,
    string DisplayName,
    ToolAdapterKind Kind,
    OperatorRiskLevel RiskLevel,
    bool DryRunSupported,
    bool RequiresApproval,
    IReadOnlyList<string> InputNames,
    IReadOnlyList<string> OutputNames,
    IReadOnlyList<string> SideEffects,
    TimeSpan DefaultTimeout,
    string? CleanupPath)
{
    public static ToolAdapterDescriptor Create(
        string id,
        string displayName,
        ToolAdapterKind kind,
        OperatorRiskLevel riskLevel,
        bool dryRunSupported,
        IReadOnlyList<string> inputNames,
        IReadOnlyList<string> outputNames,
        IReadOnlyList<string> sideEffects,
        TimeSpan defaultTimeout,
        string? cleanupPath)
    {
        if (!Enum.IsDefined(typeof(ToolAdapterKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Tool adapter kind is not supported.");
        }

        if (!Enum.IsDefined(typeof(OperatorRiskLevel), riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel), riskLevel, "Operator risk level is not supported.");
        }

        if (defaultTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultTimeout), defaultTimeout, "Tool adapter timeout must be greater than zero.");
        }

        return new ToolAdapterDescriptor(
            NormalizeId(id, nameof(id)),
            RequireText(displayName, nameof(displayName)),
            kind,
            riskLevel,
            dryRunSupported,
            riskLevel is OperatorRiskLevel.Medium or OperatorRiskLevel.High,
            NormalizeRequiredTextList(inputNames, nameof(inputNames)),
            NormalizeRequiredTextList(outputNames, nameof(outputNames)),
            NormalizeRequiredTextList(sideEffects, nameof(sideEffects)),
            defaultTimeout,
            NormalizeOptionalPath(cleanupPath));
    }

    private static string? NormalizeOptionalPath(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Replace('\\', '/').Trim();
    }

    internal static string NormalizeId(string value, string parameterName)
    {
        var text = RequireText(value, parameterName).ToLowerInvariant();
        var normalized = new string(text.Select(character =>
            char.IsAsciiLetterLower(character) || char.IsDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-').ToArray()).Trim('-');

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Tool adapter id cannot be empty.", parameterName);
        }

        return normalized;
    }

    internal static IReadOnlyList<string> NormalizeRequiredTextList(IReadOnlyList<string> values, string parameterName)
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

    internal static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record ToolAdapterRunRequest(
    ToolAdapterDescriptor Descriptor,
    bool DryRun,
    IReadOnlyDictionary<string, string> Inputs,
    DateTimeOffset RequestedAt)
{
    public static ToolAdapterRunRequest Create(
        ToolAdapterDescriptor descriptor,
        bool dryRun,
        IReadOnlyDictionary<string, string> inputs,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (dryRun && !descriptor.DryRunSupported)
        {
            throw new InvalidOperationException($"Tool adapter does not support dry-run: {descriptor.Id}");
        }

        var normalizedInputs = NormalizeInputs(inputs);
        var missingInput = descriptor.InputNames.FirstOrDefault(inputName => !normalizedInputs.ContainsKey(inputName));
        if (missingInput is not null)
        {
            throw new ArgumentException($"Tool adapter input is missing: {missingInput}", nameof(inputs));
        }

        return new ToolAdapterRunRequest(
            descriptor,
            dryRun,
            normalizedInputs,
            requestedAt);
    }

    private static IReadOnlyDictionary<string, string> NormalizeInputs(IReadOnlyDictionary<string, string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        return inputs
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                pair => pair.Key.Trim(),
                pair => pair.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record ToolAdapterRunResult(
    string ToolAdapterId,
    bool DryRun,
    IReadOnlyDictionary<string, string> Outputs,
    IReadOnlyList<string> Warnings,
    string Summary)
{
    public static ToolAdapterRunResult Create(
        string toolAdapterId,
        bool dryRun,
        IReadOnlyDictionary<string, string> outputs,
        IReadOnlyList<string> warnings,
        string summary)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(warnings);

        return new ToolAdapterRunResult(
            ToolAdapterDescriptor.NormalizeId(toolAdapterId, nameof(toolAdapterId)),
            dryRun,
            outputs
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => pair.Value.Trim(),
                    StringComparer.OrdinalIgnoreCase),
            warnings
                .Select(warning => warning?.Trim() ?? string.Empty)
                .Where(warning => warning.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            ToolAdapterDescriptor.RequireText(summary, nameof(summary)));
    }
}

public enum ToolAdapterKind
{
    Sdk = 0,
    LocalCli = 1,
    LocalLibrary = 2,
    BrowserAutomation = 3,
    WindowsDesktopAutomation = 4,
    ComputerUse = 5,
}
