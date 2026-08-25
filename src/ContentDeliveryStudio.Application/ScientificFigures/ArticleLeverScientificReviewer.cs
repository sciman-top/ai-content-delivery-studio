using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>Deterministic reviewer for contact-force and lever-boundary figures.</summary>
public sealed class ArticleLeverScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal)
    {
        "article-lever-forces-v1",
    };

    protected override IReadOnlySet<string> SupportedPackageIds => Packages;

    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(
        ArticleScientificFigureCandidateKind kind,
        XDocument document)
    {
        if (kind == ArticleScientificFigureCandidateKind.LeverRockContact
            && !document.Descendants(Svg + "path").Any(path => (string?)path.Attribute("data-article-connection") == "lever-rock-contact"))
        {
            return [("lever-rock-contact-topology-invalid", "The resultant force must originate at the visible lever-rock contact.")];
        }

        return [];
    }
}
