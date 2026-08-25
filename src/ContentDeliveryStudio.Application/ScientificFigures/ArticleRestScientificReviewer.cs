using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>Deterministic reviewer for interval-based rest and motion figures.</summary>
public sealed class ArticleRestScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal)
    {
        "article-rest-definition-v1",
    };

    protected override IReadOnlySet<string> SupportedPackageIds => Packages;

    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(
        ArticleScientificFigureCandidateKind kind,
        XDocument document)
    {
        if (kind == ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint
            && !document.Descendants(Svg + "path").Any(path => (string?)path.Attribute("data-article-connection") == "gravity-downward"))
        {
            return [("rest-gravity-direction-invalid", "The turning-point figure must show downward acceleration independently of v=0.")];
        }

        return [];
    }
}
