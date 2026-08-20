namespace ContentDeliveryStudio.Application.ScientificFigures;

public static class ArticleScientificFigureReviewerFactory
{
    private static readonly HashSet<ArticleScientificFigureCandidateKind> OpticalKinds =
    [
        ArticleScientificFigureCandidateKind.Mechanism,
        ArticleScientificFigureCandidateKind.LensEquationGraph,
        ArticleScientificFigureCandidateKind.ExperimentalComparison,
        ArticleScientificFigureCandidateKind.Comparison,
        ArticleScientificFigureCandidateKind.CorrectiveLensControl,
    ];

    private static readonly HashSet<ArticleScientificFigureCandidateKind> ThermalKinds =
    [
        ArticleScientificFigureCandidateKind.ThermalFrontMechanism,
        ArticleScientificFigureCandidateKind.ThermalBasinException,
        ArticleScientificFigureCandidateKind.ThermalConductivityComparison,
        ArticleScientificFigureCandidateKind.ThermalTransferModes,
        ArticleScientificFigureCandidateKind.ThermalHumidityClothing,
        ArticleScientificFigureCandidateKind.ThermalDryWetHeat,
    ];

    private static readonly HashSet<ArticleScientificFigureCandidateKind> GravityKinds =
    [
        ArticleScientificFigureCandidateKind.GravityTerminology,
        ArticleScientificFigureCandidateKind.GravityOrbitFreeFall,
        ArticleScientificFigureCandidateKind.GravityElevatorFreeFall,
        ArticleScientificFigureCandidateKind.GravitySurfaceRotation,
        ArticleScientificFigureCandidateKind.GravityCaseComparison,
        ArticleScientificFigureCandidateKind.GravityReferenceFrames,
    ];

    public static IArticleScientificFigureReviewer CreateFor(
        IReadOnlyCollection<ArticleScientificFigureCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one article candidate is required.", nameof(candidates));
        }

        var domainKinds = candidates
            .Where(candidate => candidate.Kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
            .Select(candidate => candidate.Kind)
            .Distinct()
            .ToArray();
        var hasThermal = domainKinds.Any(ThermalKinds.Contains);
        var hasGravity = domainKinds.Any(GravityKinds.Contains);
        if (hasThermal && hasGravity)
        {
            throw new InvalidOperationException("An article figure set cannot mix thermal and gravity review profiles.");
        }

        if (hasGravity)
        {
            EnsureAllKindsBelongTo(domainKinds, GravityKinds, "gravity");
            return new ArticleGravityScientificReviewer();
        }

        if (hasThermal)
        {
            EnsureAllKindsBelongTo(domainKinds, ThermalKinds, "thermal");
            return new ArticleThermalScientificReviewer();
        }

        if (domainKinds.Length > 0 && domainKinds.All(OpticalKinds.Contains))
        {
            return new ArticleOpticalScientificReviewer();
        }

        throw new InvalidOperationException(
            "No supported article scientific review profile matches the candidate set.");
    }

    private static void EnsureAllKindsBelongTo(
        IReadOnlyCollection<ArticleScientificFigureCandidateKind> actualKinds,
        IReadOnlySet<ArticleScientificFigureCandidateKind> allowedKinds,
        string profile)
    {
        var unsupported = actualKinds.Where(kind => !allowedKinds.Contains(kind)).ToArray();
        if (unsupported.Length > 0)
        {
            throw new InvalidOperationException(
                $"The {profile} review profile cannot validate candidate kinds: {string.Join(", ", unsupported)}.");
        }
    }
}
