using ImageSeriesStudio.Application.Sources;
using ImageSeriesStudio.Core.Sources;

namespace ImageSeriesStudio.Infrastructure.Sources;

public sealed class SupportMatrixSourceIngestionProvider : ISourceIngestionProvider
{
    private readonly IDocumentExtractionProvider _localBinaryDocumentExtractionProvider;
    private readonly ISourceIngestionProvider _fallbackProvider;

    public SupportMatrixSourceIngestionProvider(
        IDocumentExtractionProvider localBinaryDocumentExtractionProvider,
        ISourceIngestionProvider fallbackProvider)
    {
        _localBinaryDocumentExtractionProvider = localBinaryDocumentExtractionProvider;
        _fallbackProvider = fallbackProvider;
    }

    public SourceIngestionProviderCapabilities Capabilities { get; } = new(
        "support-matrix-source-ingestion",
        "Support Matrix Source Ingestion Provider");

    public async Task<SourceIngestionProviderResult> IngestAsync(
        SourceIngestionProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryCreateBinaryExtractionRequest(request.Source, out var extractionRequest) && extractionRequest is not null)
        {
            var extraction = await _localBinaryDocumentExtractionProvider.ExtractAsync(extractionRequest, cancellationToken);
            return BuildBinaryResult(request, extraction);
        }

        return await _fallbackProvider.IngestAsync(request, cancellationToken);
    }

    private static bool TryCreateBinaryExtractionRequest(
        SourceIngestionRequest source,
        out DocumentExtractionRequest? extractionRequest)
    {
        extractionRequest = null;

        if (source.Kind is not (SourceAssetKind.Pdf or SourceAssetKind.Docx))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(source.OriginalPath) || !File.Exists(source.OriginalPath))
        {
            return false;
        }

        extractionRequest = new DocumentExtractionRequest(
            source.Kind,
            source.DisplayName,
            source.SourceText,
            source.OriginalPath,
            UseOcr: false);
        return true;
    }

    private static SourceIngestionProviderResult BuildBinaryResult(
        SourceIngestionProviderRequest request,
        DocumentExtractionResult extraction)
    {
        var asset = SourceAsset.Create(
            request.ProjectId,
            request.Source.Kind,
            request.Source.DisplayName,
            request.Source.OriginalPath,
            request.Source.MimeType,
            request.Source.SizeBytes,
            request.Source.Sha256,
            request.Timestamp);

        foreach (var content in extraction.ExtractedContents)
        {
            var extracted = asset.AddExtractedContent(
                content.Kind,
                content.Text,
                content.LocationHint,
                content.PageNumber,
                content.StartOffset,
                content.EndOffset,
                request.Timestamp);

            var anchor = extraction.EvidenceAnchors.FirstOrDefault(evidence => evidence.ExtractedContentIndex == asset.ExtractedContents.Count - 1);
            if (anchor is not null)
            {
                asset.AddEvidenceAnchor(
                    extracted.Id,
                    anchor.Label,
                    anchor.Quote,
                    anchor.LocationHint,
                    request.Timestamp);
            }
        }

        return new SourceIngestionProviderResult(asset, extraction.ProviderTraceId);
    }
}
