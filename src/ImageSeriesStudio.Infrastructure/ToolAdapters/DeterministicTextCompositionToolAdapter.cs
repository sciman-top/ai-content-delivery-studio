using System.Text.Json;
using ImageSeriesStudio.Application.Composition;
using ImageSeriesStudio.Application.ToolAdapters;

namespace ImageSeriesStudio.Infrastructure.ToolAdapters;

public sealed class DeterministicTextCompositionToolAdapter : IToolAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ToolAdapterDescriptor BuiltInDescriptor =
        LocalToolRegistry.CreateBuiltIn().GetRequired("deterministic-text-composition");
    private readonly IDeterministicTextComposer _composer;

    public DeterministicTextCompositionToolAdapter(IDeterministicTextComposer composer)
    {
        _composer = composer;
    }

    public ToolAdapterDescriptor Descriptor => BuiltInDescriptor;

    public async Task<ToolAdapterRunResult> RunAsync(
        ToolAdapterRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var backgroundPath = GetRequiredInput(request, "backgroundPath");
        var labelSpecPath = GetRequiredInput(request, "labelSpecPath");
        if (!File.Exists(labelSpecPath))
        {
            throw new FileNotFoundException("Label spec file was not found.", labelSpecPath);
        }

        var specJson = await File.ReadAllTextAsync(labelSpecPath, cancellationToken);
        var spec = JsonSerializer.Deserialize<DeterministicTextCompositionLabelSpec>(specJson, JsonOptions)
            ?? throw new InvalidOperationException($"Label spec could not be deserialized: {labelSpecPath}");
        var labelSpecDirectory = Path.GetDirectoryName(labelSpecPath) ?? Directory.GetCurrentDirectory();
        var compositionRequest = new DeterministicTextCompositionRequest(
            backgroundPath,
            ResolveOutputPath(spec.ComposedImagePath, labelSpecDirectory),
            ResolveOutputPath(spec.LayoutReportPath, labelSpecDirectory),
            spec.Overlays.Select(overlay => new DeterministicTextOverlay(
                overlay.Text,
                overlay.X,
                overlay.Y,
                overlay.FontSize,
                overlay.HexColor)).ToArray());
        if (request.DryRun)
        {
            return ToolAdapterRunResult.Create(
                Descriptor.Id,
                dryRun: true,
                new Dictionary<string, string>
                {
                    ["composedImagePath"] = compositionRequest.ComposedImagePath,
                    ["layoutReportPath"] = compositionRequest.LayoutReportPath,
                },
                [],
                $"Deterministic text composition dry-run validated {compositionRequest.Overlays.Count} overlay(s).");
        }

        var compositionResult = await _composer.ComposeAsync(compositionRequest, cancellationToken);

        return ToolAdapterRunResult.Create(
            Descriptor.Id,
            dryRun: false,
            new Dictionary<string, string>
            {
                ["composedImagePath"] = compositionResult.ComposedImagePath,
                ["layoutReportPath"] = compositionResult.LayoutReportPath,
            },
            compositionResult.Warnings,
            $"Deterministic text composition succeeded with {compositionResult.OverlayCount} overlay(s).");
    }

    private static string GetRequiredInput(ToolAdapterRunRequest request, string inputName)
    {
        if (!request.Inputs.TryGetValue(inputName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Tool adapter input is missing: {inputName}");
        }

        return value.Trim();
    }

    private static string ResolveOutputPath(string value, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(value));
        }

        return Path.IsPathRooted(value)
            ? value.Trim()
            : Path.GetFullPath(Path.Combine(baseDirectory, value.Trim()));
    }
}

internal sealed record DeterministicTextCompositionLabelSpec(
    string ComposedImagePath,
    string LayoutReportPath,
    IReadOnlyList<DeterministicTextCompositionLabelSpecOverlay> Overlays);

internal sealed record DeterministicTextCompositionLabelSpecOverlay(
    string Text,
    float X,
    float Y,
    float FontSize,
    string HexColor);
