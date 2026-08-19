namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Domain seam for deterministic article-science review. The set runner does not
/// know whether a candidate is optical, thermal, or another admitted domain.
/// </summary>
public interface IArticleScientificFigureReviewer
{
    ArticleOpticalScientificReviewReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board);

    IReadOnlyList<ArticleOpticalVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate);
}
