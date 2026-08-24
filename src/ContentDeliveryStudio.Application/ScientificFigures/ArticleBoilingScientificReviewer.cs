using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed class ArticleBoilingScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal) { "article-boiling-bubbles-v1" };
    protected override IReadOnlySet<string> SupportedPackageIds => Packages;
    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(ArticleScientificFigureCandidateKind kind, XDocument document) => [];
}
