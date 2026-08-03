using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificDocumentExtractor
{
    Task<ScientificDocumentExtraction> ExtractAsync(
        ScientificDocumentExtractionRequest request,
        CancellationToken cancellationToken);
}

public sealed record ScientificDocumentExtractionRequest(
    Guid SourceAssetId,
    string SourceSha256,
    SourceAssetKind SourceKind,
    string DisplayName,
    string SourceText,
    string? OriginalPath,
    bool IsScanned,
    bool UseOcr,
    ScientificReadingOrderStatus ReadingOrder,
    IReadOnlyList<ScientificRequiredContentKind> RequiredContent);

public enum ScientificRequiredContentKind
{
    Formula = 0,
    Table = 1,
    Caption = 2,
    Citation = 3,
}
