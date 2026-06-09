using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Application.ToolAdapters;

public sealed class LocalToolRegistry
{
    private readonly Dictionary<string, ToolAdapterDescriptor> _toolsById;

    private LocalToolRegistry(IReadOnlyList<ToolAdapterDescriptor> tools)
    {
        Tools = tools;
        _toolsById = tools.ToDictionary(tool => tool.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ToolAdapterDescriptor> Tools { get; }

    public static LocalToolRegistry Create(IReadOnlyList<ToolAdapterDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        var duplicate = tools
            .GroupBy(tool => tool.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate local tool id: {duplicate}", nameof(tools));
        }

        return new LocalToolRegistry(tools.ToArray());
    }

    public static LocalToolRegistry CreateBuiltIn()
    {
        return Create(
            [
                ToolAdapterDescriptor.Create(
                    "document-extraction",
                    "Document Extraction",
                    ToolAdapterKind.LocalLibrary,
                    OperatorRiskLevel.Low,
                    dryRunSupported: true,
                    inputNames: ["sourcePath", "sourceKind"],
                    outputNames: ["extractedContentPath", "evidenceAnchorsPath"],
                    sideEffects: ["Reads a workspace source file and writes extracted text/evidence artifacts."],
                    defaultTimeout: TimeSpan.FromMinutes(1),
                    cleanupPath: null),
                ToolAdapterDescriptor.Create(
                    "document-conversion",
                    "Document Conversion",
                    ToolAdapterKind.LocalCli,
                    OperatorRiskLevel.Medium,
                    dryRunSupported: true,
                    inputNames: ["sourcePath", "targetFormat"],
                    outputNames: ["convertedArtifactPath"],
                    sideEffects: ["Writes a converted document artifact under the workspace outputs folder."],
                    defaultTimeout: TimeSpan.FromMinutes(2),
                    cleanupPath: "workspace/outputs/.conversion-tmp"),
                ToolAdapterDescriptor.Create(
                    "ocr-extraction",
                    "OCR Extraction",
                    ToolAdapterKind.LocalCli,
                    OperatorRiskLevel.Low,
                    dryRunSupported: true,
                    inputNames: ["imagePath"],
                    outputNames: ["ocrTextPath"],
                    sideEffects: ["Reads an image and writes OCR text under the workspace extraction folder."],
                    defaultTimeout: TimeSpan.FromMinutes(2),
                    cleanupPath: "workspace/extraction/.ocr-tmp"),
                ToolAdapterDescriptor.Create(
                    "imagemagick-processing",
                    "ImageMagick Processing",
                    ToolAdapterKind.LocalCli,
                    OperatorRiskLevel.Medium,
                    dryRunSupported: true,
                    inputNames: ["imagePath", "operationSpecPath"],
                    outputNames: ["processedImagePath"],
                    sideEffects: ["Writes resized, composed, or converted image derivatives under workspace outputs."],
                    defaultTimeout: TimeSpan.FromMinutes(2),
                    cleanupPath: "workspace/outputs/.imagemagick-tmp"),
                ToolAdapterDescriptor.Create(
                    "ffmpeg-processing",
                    "FFmpeg Processing",
                    ToolAdapterKind.LocalCli,
                    OperatorRiskLevel.Medium,
                    dryRunSupported: true,
                    inputNames: ["mediaPath", "operationSpecPath"],
                    outputNames: ["processedMediaPath"],
                    sideEffects: ["Writes generated media derivatives under workspace outputs."],
                    defaultTimeout: TimeSpan.FromMinutes(5),
                    cleanupPath: "workspace/outputs/.ffmpeg-tmp"),
                ToolAdapterDescriptor.Create(
                    "deterministic-text-composition",
                    "Deterministic Text Composition",
                    ToolAdapterKind.LocalLibrary,
                    OperatorRiskLevel.Medium,
                    dryRunSupported: true,
                    inputNames: ["backgroundPath", "labelSpecPath"],
                    outputNames: ["composedImagePath", "layoutReportPath"],
                    sideEffects: ["Writes SkiaSharp-composed image and text layout report under workspace outputs."],
                    defaultTimeout: TimeSpan.FromMinutes(2),
                    cleanupPath: "workspace/outputs/.composition-tmp"),
                ToolAdapterDescriptor.Create(
                    "artifact-validation",
                    "Artifact Validation",
                    ToolAdapterKind.LocalLibrary,
                    OperatorRiskLevel.Low,
                    dryRunSupported: true,
                    inputNames: ["manifestPath"],
                    outputNames: ["validationReportPath"],
                    sideEffects: ["Reads delivery artifacts and writes an additive validation report."],
                    defaultTimeout: TimeSpan.FromSeconds(30),
                    cleanupPath: null),
                ToolAdapterDescriptor.Create(
                    "openai-launch-preflight",
                    "OpenAI Launch Preflight",
                    ToolAdapterKind.LocalLibrary,
                    OperatorRiskLevel.Low,
                    dryRunSupported: true,
                    inputNames: ["envPath"],
                    outputNames: ["preflightJsonPath", "preflightMarkdownPath"],
                    sideEffects: ["Reads provider configuration and secret readiness, writes an OpenAI launch preflight report."],
                    defaultTimeout: TimeSpan.FromSeconds(30),
                    cleanupPath: null),
            ]);
    }

    public ToolAdapterDescriptor GetRequired(string toolId)
    {
        var normalizedId = ToolAdapterDescriptor.NormalizeId(toolId, nameof(toolId));
        if (!_toolsById.TryGetValue(normalizedId, out var descriptor))
        {
            throw new InvalidOperationException($"Local tool not found: {normalizedId}");
        }

        return descriptor;
    }
}
