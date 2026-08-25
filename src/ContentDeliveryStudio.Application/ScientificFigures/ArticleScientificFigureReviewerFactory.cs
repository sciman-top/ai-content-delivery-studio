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
    private static readonly HashSet<ArticleScientificFigureCandidateKind> ExtendedMechanicsKinds =
    [
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy, ArticleScientificFigureCandidateKind.BernoulliFanZones, ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary,
        ArticleScientificFigureCandidateKind.PinholeGeometry, ArticleScientificFigureCandidateKind.PinholeFocusPlane, ArticleScientificFigureCandidateKind.PinholeObservation,
        ArticleScientificFigureCandidateKind.SuperconductingEnergy, ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent, ArticleScientificFigureCandidateKind.SuperconductingExcitation,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> MeterKinds =
    [
        ArticleScientificFigureCandidateKind.MeterTransientResponse, ArticleScientificFigureCandidateKind.MeterTrialDecision, ArticleScientificFigureCandidateKind.MeterProtectionLayers,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> BoilingKinds =
    [
        ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse, ArticleScientificFigureCandidateKind.BoilingBubbleGrowth, ArticleScientificFigureCandidateKind.BoilingPressureScale,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> GalileanKinds =
    [
        ArticleScientificFigureCandidateKind.GalileanAfocalPath, ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes, ArticleScientificFigureCandidateKind.GalileanAngularMagnification,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> DryIceKinds =
    [
        ArticleScientificFigureCandidateKind.DryIceWaterMechanism,
        ArticleScientificFigureCandidateKind.DryIceHeatTransferComparison,
        ArticleScientificFigureCandidateKind.DryIceIsolationVerification,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> LeverKinds =
    [
        ArticleScientificFigureCandidateKind.LeverRockContact,
        ArticleScientificFigureCandidateKind.LeverSeesawFriction,
        ArticleScientificFigureCandidateKind.LeverTwoForceMember,
    ];
    private static readonly HashSet<ArticleScientificFigureCandidateKind> RestKinds =
    [
        ArticleScientificFigureCandidateKind.RestIntervalDefinition,
        ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint,
        ArticleScientificFigureCandidateKind.RestStateComparison,
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
        var hasExtendedMechanics = domainKinds.Any(ExtendedMechanicsKinds.Contains);
        var hasMeter = domainKinds.Any(MeterKinds.Contains);
        var hasBoiling = domainKinds.Any(BoilingKinds.Contains);
        var hasGalilean = domainKinds.Any(GalileanKinds.Contains);
        var hasDryIce = domainKinds.Any(DryIceKinds.Contains);
        var hasLever = domainKinds.Any(LeverKinds.Contains);
        var hasRest = domainKinds.Any(RestKinds.Contains);
        if (new[] { hasThermal, hasGravity, hasThermistor, hasArchimedes, hasExtendedMechanics, hasMeter, hasBoiling, hasGalilean, hasDryIce, hasLever, hasRest }.Count(value => value) > 1)
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
        if (hasExtendedMechanics)
        {
            EnsureAllKindsBelongTo(domainKinds, ExtendedMechanicsKinds, "extended article");
            return new ArticleMechanicsScientificReviewer();
        }

        if (hasMeter)
        {
            EnsureAllKindsBelongTo(domainKinds, MeterKinds, "meter trial");
            return new ArticleMeterScientificReviewer();
        }

        if (hasBoiling)
        {
            EnsureAllKindsBelongTo(domainKinds, BoilingKinds, "boiling bubbles");
            return new ArticleBoilingScientificReviewer();
        }

        if (hasGalilean)
        {
            EnsureAllKindsBelongTo(domainKinds, GalileanKinds, "galilean eyepiece");
            return new ArticleGalileanScientificReviewer();
        }

        if (hasDryIce)
        {
            EnsureAllKindsBelongTo(domainKinds, DryIceKinds, "dry ice");
            return new ArticleDryIceScientificReviewer();
        }

        if (hasLever)
        {
            EnsureAllKindsBelongTo(domainKinds, LeverKinds, "lever forces");
            return new ArticleLeverScientificReviewer();
        }

        if (hasRest)
        {
            EnsureAllKindsBelongTo(domainKinds, RestKinds, "rest definition");
            return new ArticleRestScientificReviewer();
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
