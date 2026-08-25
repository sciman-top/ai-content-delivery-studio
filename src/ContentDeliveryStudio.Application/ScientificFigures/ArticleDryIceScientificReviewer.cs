using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>Deterministic reviewer for the dry-ice phase-change article profile.</summary>
public sealed class ArticleDryIceScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal)
    {
        "article-dry-ice-v1",
    };

    protected override IReadOnlySet<string> SupportedPackageIds => Packages;

    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(
        ArticleScientificFigureCandidateKind kind,
        XDocument document)
    {
        if (kind == ArticleScientificFigureCandidateKind.DryIceWaterMechanism
            && !document.Descendants(Svg + "path").Any(path => (string?)path.Attribute("data-article-connection") == "gas-carries-crystals"))
        {
            return [("dry-ice-crystal-carry-topology-invalid", "The gas path must visibly carry ice crystals out of the water.")];
        }

        return [];
    }
}
