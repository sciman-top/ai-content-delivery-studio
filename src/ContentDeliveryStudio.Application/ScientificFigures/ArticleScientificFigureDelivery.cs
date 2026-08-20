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

public enum ArticleScientificFigureReviewRoute
{
    Blocked = 0,
    AuthorizedAgentAccept = 1,
    IndependentHumanExpertRequired = 2,
}

public sealed record ArticleScientificFigureReviewAutomationRequest(
    string ReviewReadyDirectory,
    string Reviewer,
    string AuthorizationReference,
    string Notes,
    bool ConfirmEveryCandidateVisuallyInspected,
    bool RequireIndependentHumanExpertCertification,
    DateTimeOffset ReviewedAt);

public sealed record ArticleScientificFigureReviewedFile(
    string RelativePath,
    long Bytes,
    string Sha256);

public sealed record ArticleScientificFigureReviewedCandidate(
    string CandidateId,
    string Kind,
    string RiskLevel,
    IReadOnlyList<ArticleScientificFigureReviewedFile> Files);

public sealed record ArticleScientificFigureAuthorizedAgentReceipt(
    int SchemaVersion,
    string Reviewer,
    string AuthorizationReference,
    string Notes,
    DateTimeOffset ReviewedAt,
    string ReviewMethod,
    bool EveryCandidateVisuallyInspected,
    bool LiveProviderAccepted,
    IReadOnlyList<ArticleScientificFigureReviewedFile> AuthorityFiles,
    IReadOnlyList<ArticleScientificFigureReviewedCandidate> Candidates);

public sealed record ArticleScientificFigureReviewAutomationAssessment(
    int SchemaVersion,
    ArticleScientificFigureReviewRoute Route,
    bool EligibleForPromotion,
    bool RequiresHumanOnsiteReview,
    bool RequiresPerCandidateUserReview,
    bool RequiresIndependentHumanExpert,
    bool EligibleForFutureStandingAutomation,
    int CandidateCount,
    string MaximumRiskLevel,
    string VisualReviewProvider,
    IReadOnlyList<string> Reasons);

public sealed record ArticleScientificFigureReviewAutomationResult(
    string ReceiptPath,
    string AssessmentPath,
    ArticleScientificFigureAuthorizedAgentReceipt Receipt,
    ArticleScientificFigureReviewAutomationAssessment Assessment);
