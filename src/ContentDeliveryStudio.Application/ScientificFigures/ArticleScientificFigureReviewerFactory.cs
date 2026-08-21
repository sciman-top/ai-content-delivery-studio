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

    private static readonly HashSet<ArticleScientificFigureCandidateKind> ThermistorKinds =
    [
        ArticleScientificFigureCandidateKind.ThermistorCircuitDivider,
        ArticleScientificFigureCandidateKind.ThermistorCurvature,
        ArticleScientificFigureCandidateKind.ThermistorError,
        ArticleScientificFigureCandidateKind.ThermistorSpecialValues,
    ];

    private static readonly HashSet<ArticleScientificFigureCandidateKind> ArchimedesKinds =
    [
        ArticleScientificFigureCandidateKind.ArchimedesDefinition,
        ArticleScientificFigureCandidateKind.ArchimedesWaterModel,
        ArticleScientificFigureCandidateKind.ArchimedesBottomContact,
        ArticleScientificFigureCandidateKind.ArchimedesDepthDependence,
        ArticleScientificFigureCandidateKind.ArchimedesTopContact,
        ArticleScientificFigureCandidateKind.ArchimedesPier,
        ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> ExtendedKinds =
    [
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy, ArticleScientificFigureCandidateKind.BernoulliFanZones, ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary,
        ArticleScientificFigureCandidateKind.PinholeGeometry, ArticleScientificFigureCandidateKind.PinholeFocusPlane, ArticleScientificFigureCandidateKind.PinholeObservation,
        ArticleScientificFigureCandidateKind.SuperconductingEnergy, ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent, ArticleScientificFigureCandidateKind.SuperconductingExcitation,
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
        var hasThermistor = domainKinds.Any(ThermistorKinds.Contains);
        var hasArchimedes = domainKinds.Any(ArchimedesKinds.Contains);
        var hasExtended = domainKinds.Any(ExtendedKinds.Contains);
        if (new[] { hasThermal, hasGravity, hasThermistor, hasArchimedes, hasExtended }.Count(value => value) > 1)
        {
            throw new InvalidOperationException("An article figure set cannot mix scientific review profiles.");
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

        if (hasThermistor)
        {
            EnsureAllKindsBelongTo(domainKinds, ThermistorKinds, "thermistor");
            return new ArticleThermistorScientificReviewer();
        }

        if (hasArchimedes)
        {
            EnsureAllKindsBelongTo(domainKinds, ArchimedesKinds, "archimedes");
            return new ArticleArchimedesScientificReviewer();
        }
        if (hasExtended)
        {
            EnsureAllKindsBelongTo(domainKinds, ExtendedKinds, "extended article");
            return new ArticleMechanicsScientificReviewer();
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
