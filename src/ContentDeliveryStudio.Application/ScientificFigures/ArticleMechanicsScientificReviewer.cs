using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>Reviewer for the three pre-existing extended corpus profiles.</summary>
public sealed class ArticleMechanicsScientificReviewer : ArticleHighStandardScientificReviewerBase
{
    private static readonly IReadOnlySet<string> Packages = new HashSet<string>(StringComparer.Ordinal)
    {
        "article-bernoulli-v1", "article-pinhole-v1", "article-superconducting-v1",
    };

    protected override IReadOnlySet<string> SupportedPackageIds => Packages;

    protected override IReadOnlyList<(string Code, string Evidence)> ValidateProfileGeometry(
        ArticleScientificFigureCandidateKind kind,
        XDocument document)
    {
        if (kind == ArticleScientificFigureCandidateKind.PinholeGeometry
            && (!HasEndpointNear(document, "principal-ray", 520, 360, 4, 4)
                || !HasEndpointNear(document, "principal-ray", 1030, 260, 4, 1)
                || !HasEndpointNear(document, "principal-ray", 1030, 610, 4, 1)))
            return [("article-pinhole-ray-topology-invalid", "Principal rays must visibly converge at the aperture and reach both image endpoints.")];
        if (kind == ArticleScientificFigureCandidateKind.PinholeFocusPlane
            && (!HasEndpointNear(document, "ray", 490, 365, 4, 4)
                || !HasEndpointNear(document, "camera-input-ray", 765, 260, 4, 2)
                || !HasEndpointNear(document, "camera-focused-ray", 1060, 365, 4, 2)))
            return [("article-focus-ray-topology-invalid", "Focus-plane rays must join at the aperture, image position, lens input, and sensor target.")];
        if (kind == ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary
            && (!HasEndpointNear(document, "same-streamline", 330, 330, 4, 2)
                || !HasEndpointNear(document, "throat-section", 330, 275, 4, 1)
                || !HasEndpointNear(document, "throat-section", 330, 385, 4, 1)))
            return [("article-bernoulli-throat-topology-invalid", "The converging duct, throat section, and same-streamline paths must visibly share the throat boundary.")];
        if (kind == ArticleScientificFigureCandidateKind.PinholeObservation
            && (!HasEndpointNear(document, "near-field", 280, 390, 4, 3)
                || !HasEndpointNear(document, "far-field", 840, 390, 4, 3)))
            return [("article-observation-comparison-topology-invalid", "Near and far comparison paths must visibly join their respective apertures.")];
        if (kind == ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent
            && (!HasEndpointNear(document, "charging-loop", 225, 350, 4, 1)
                || !HasEndpointNear(document, "charging-loop", 225, 420, 4, 1)
                || !HasEndpointNear(document, "charging-loop", 495, 465, 4, 1)
                || !HasEndpointNear(document, "charging-loop", 325, 465, 4, 1)))
            return [("article-persistent-current-topology-invalid", "The charging loop must visibly connect the excitation source to both sides of the coil.")];
        if (kind == ArticleScientificFigureCandidateKind.SuperconductingEnergy
            && (!HasEndpointNear(document, "circuit", 300, 350, 4, 1)
                || !HasEndpointNear(document, "circuit", 300, 450, 4, 1)
                || !HasEndpointNear(document, "circuit", 468, 400, 4, 1)
                || !HasEndpointNear(document, "circuit", 832, 400, 4, 1)
                || !HasEndpointNear(document, "switch", 385, 350, 4, 1)
                || !HasEndpointNear(document, "switch", 435, 350, 4, 1)))
            return [("article-superconducting-energy-topology-invalid", "The excitation diagram must show a source-connected closed circuit through the switch and both sides of the coil.")];
        if (kind == ArticleScientificFigureCandidateKind.SuperconductingExcitation
            && (!HasEndpointNear(document, "excitation-circuit", 285, 315, 4, 1)
                || !HasEndpointNear(document, "excitation-circuit", 285, 370, 4, 1)
                || !HasEndpointNear(document, "heater-circuit", 685, 205, 4, 1)
                || !HasEndpointNear(document, "heater-circuit", 685, 220, 4, 1)
                || !HasEndpointNear(document, "thermal-coupling", 635, 225, 4, 1)
                || !HasEndpointNear(document, "thermal-coupling", 635, 235, 4, 1)))
            return [("article-superconducting-heater-topology-invalid", "The heater circuit must terminate at the heater and the thermal-coupling marker must reach the switch branch without becoming a main-loop wire.")];
        return [];
    }
}
