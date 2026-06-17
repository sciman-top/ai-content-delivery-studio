using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Application.Sources;

public interface IDocumentExtractionProvider
{
    DocumentExtractionProviderCapabilities Capabilities { get; }

    Task<DocumentExtractionResult> ExtractAsync(
        DocumentExtractionRequest request,
        CancellationToken cancellationToken);
}

public sealed record DocumentExtractionProviderCapabilities(
    string ProviderId,
    string DisplayName,
    IReadOnlyList<SourceAssetKind> SupportedSourceKinds,
    bool SupportsOcr,
    bool RequiresExplicitApproval = false);

public sealed record DocumentExtractionRequest(
    SourceAssetKind SourceKind,
    string DisplayName,
    string FixtureText,
    string? OriginalPath = null,
    bool UseOcr = false);

public sealed record DocumentExtractionResult(
    IReadOnlyList<ExtractedContentDraft> ExtractedContents,
    IReadOnlyList<EvidenceAnchorDraft> EvidenceAnchors,
    string ProviderTraceId);

public sealed record ExtractedContentDraft(
    ExtractedContentKind Kind,
    string Text,
    string LocationHint,
    int? PageNumber,
    int? StartOffset,
    int? EndOffset);

public sealed record EvidenceAnchorDraft(
    int ExtractedContentIndex,
    string Label,
    string Quote,
    string LocationHint);
