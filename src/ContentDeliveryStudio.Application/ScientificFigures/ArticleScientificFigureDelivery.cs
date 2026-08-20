namespace ContentDeliveryStudio.Application.ScientificFigures;

public enum ArticleScientificFigureApprovalActor
{
    Human = 0,
    AuthorizedAgent = 1,
}

public sealed record ArticleScientificFigureDeliveryPromotionRequest(
    string ReviewReadyDirectory,
    string DeliveryRoot,
    string ArticleSlug,
    string PackageId,
    string Reviewer,
    ArticleScientificFigureApprovalActor Actor,
    string? AuthorizationReference,
    bool GateOneApproved,
    string GateOneNotes,
    bool GateTwoApproved,
    string GateTwoNotes,
    DateTimeOffset ApprovedAt);

public sealed record ArticleScientificFigureDeliveryPromotionResult(
    string PackageDirectory,
    string ManifestPath,
    string ManifestSha256,
    int FigureAssetCount,
    int EvidenceAssetCount,
    int ReviewCount,
    int MetadataCount);
