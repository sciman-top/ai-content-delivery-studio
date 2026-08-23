namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Domain seam for deterministic article-science review. The set runner does not
/// know whether a candidate is optical, thermal, or another admitted domain.
/// </summary>
public interface IArticleScientificFigureReviewer
{
    ArticleScientificReviewReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board);

    IReadOnlyList<ArticleScientificVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate);
}

/// <summary>Shared review vocabulary for every admitted article-science domain.</summary>
public sealed record ArticleScientificFinding(
    string Code,
    string ResponsibleItemId,
    string Evidence);

public sealed record ArticleScientificReviewReport(
    string PackageId,
    string AuthorityBoundary,
    IReadOnlyList<ArticleScientificFinding> Findings,
    IReadOnlyList<ScientificExpectedVisualCheck> ExpectedVisualChecks)
{
    public bool Passed => Findings.Count == 0;
}

public sealed record ArticleScientificVisualRegion(
    ScientificVisualRegionKind Kind,
    ScientificPixelRegion Region,
    ScientificExpectedVisualCheck ExpectedCheck);
