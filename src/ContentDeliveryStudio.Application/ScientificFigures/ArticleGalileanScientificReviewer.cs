using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed class ArticleGalileanScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal) { "article-galilean-eyepiece-v1" };
    protected override IReadOnlySet<string> SupportedPackageIds => Packages;
    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(ArticleScientificFigureCandidateKind kind, XDocument document)
    {
        if (kind == ArticleScientificFigureCandidateKind.GalileanAfocalPath && (!HasEndpointNear(document, "objective-convergence", 810, 350, 4, 1) || !HasEndpointNear(document, "objective-convergence", 810, 400, 4, 1) || !HasEndpointNear(document, "objective-convergence", 810, 450, 4, 1) || !HasEndpointNear(document, "would-be-focus", 810, 350, 4, 1) || !HasEndpointNear(document, "would-be-focus", 810, 400, 4, 1) || !HasEndpointNear(document, "would-be-focus", 810, 450, 4, 1) || !HasEndpointNear(document, "would-be-focus", 980, 400, 4, 3) || !HasEndpointNear(document, "afocal-output", 810, 350, 4, 1) || !HasEndpointNear(document, "afocal-output", 810, 400, 4, 1) || !HasEndpointNear(document, "afocal-output", 810, 450, 4, 1) || !HasDashedPaths(document, "would-be-focus", 3)))
            return [("article-galilean-afocal-topology-invalid", "Parallel objective rays must converge to the same original focus. Solid rays must terminate at the concave eyepiece; dashed would-be-focus rays then show that common focus without implying unchanged transmission through the eyepiece.")];
        return [];
    }
}
