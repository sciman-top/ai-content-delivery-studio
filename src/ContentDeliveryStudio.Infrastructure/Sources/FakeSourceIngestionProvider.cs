using ContentDeliveryStudio.Application.Sources;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Infrastructure.Sources;

public sealed class FakeSourceIngestionProvider : ISourceIngestionProvider
{
    public SourceIngestionProviderCapabilities Capabilities { get; } = new(
        "fake-source",
        "Fake Source Ingestion Provider");

    public Task<SourceIngestionProviderResult> IngestAsync(
        SourceIngestionProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = request.Source;
        var text = EnsureText(source.SourceText, $"No extractable text supplied for {source.DisplayName}.");
        var asset = SourceAsset.Create(
            request.ProjectId,
            source.Kind,
            source.DisplayName,
            source.OriginalPath,
            source.MimeType,
            source.SizeBytes,
            source.Sha256,
            request.Timestamp);
        var extracted = asset.AddExtractedContent(
            ExtractedContentKind.PlainText,
            text,
            "fake extraction",
            pageNumber: null,
            startOffset: 0,
            endOffset: text.Length,
            request.Timestamp);
        asset.AddEvidenceAnchor(
            extracted.Id,
            "fake-source-anchor",
            SelectAnchorQuote(text),
            extracted.LocationHint,
            request.Timestamp);

        return Task.FromResult(new SourceIngestionProviderResult(
            asset,
            "fake-source-ingestion"));
    }

    private static string EnsureText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string SelectAnchorQuote(string text)
    {
        var firstLine = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        var quote = string.IsNullOrWhiteSpace(firstLine) ? text.Trim() : firstLine.Trim();

        return quote.Length <= 160 ? quote : quote[..160].Trim();
    }
}
