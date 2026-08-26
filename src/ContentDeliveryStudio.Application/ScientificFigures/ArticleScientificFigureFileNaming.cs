namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Deterministic delivery file-name prefixes for article figure candidates,
/// keyed by candidate kind. Extracted from the Tools entrypoint so an
/// enumerated coverage test can prove the map stays exhaustive as new
/// candidate domains are added.
/// </summary>
public static class ArticleScientificFigureFileNaming
{
    /// <summary>
    /// The optical-lane evidence board takes slot 06; thermal and gravity
    /// articles own a plain-figure prefix in that slot, so their board moves
    /// to 07. Deriving this from the batch's candidate kinds keeps the layout
    /// stable regardless of the article title wording.
    /// </summary>
    public static string EvidenceBoardPrefix(IReadOnlyCollection<ArticleScientificFigureCandidateKind> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        var occupiesSlot06 = kinds.Any(kind =>
            kind is not ArticleScientificFigureCandidateKind.SourceEvidenceBoard
            && (kind.ToString().StartsWith("Thermal", StringComparison.Ordinal)
                || kind.ToString().StartsWith("Gravity", StringComparison.Ordinal)));
        return occupiesSlot06 ? "07-source-evidence-board" : "06-source-evidence-board";
    }

    public static string GetFileNamePrefix(
        ArticleScientificFigureCandidate candidate,
        string evidenceBoardPrefix) => candidate.Kind switch
    {
        ArticleScientificFigureCandidateKind.Mechanism => "01-secondary-imaging",
        ArticleScientificFigureCandidateKind.LensEquationGraph => "02-lens-equation",
        ArticleScientificFigureCandidateKind.ExperimentalComparison => "03-screen-retina",
        ArticleScientificFigureCandidateKind.Comparison => "04-observation-position",
        ArticleScientificFigureCandidateKind.CorrectiveLensControl => "05-corrective-lens",
        ArticleScientificFigureCandidateKind.SourceEvidenceBoard => evidenceBoardPrefix,
        ArticleScientificFigureCandidateKind.ThermalFrontMechanism => "01-thermal-snow-front",
        ArticleScientificFigureCandidateKind.ThermalBasinException => "02-thermal-basin-exception",
        ArticleScientificFigureCandidateKind.ThermalConductivityComparison => "03-thermal-conductivity",
        ArticleScientificFigureCandidateKind.ThermalTransferModes => "04-thermal-transfer-modes",
        ArticleScientificFigureCandidateKind.ThermalHumidityClothing => "05-thermal-humidity-clothing",
        ArticleScientificFigureCandidateKind.ThermalDryWetHeat => "06-thermal-dry-wet-heat",
        ArticleScientificFigureCandidateKind.GravityTerminology => "01-gravity-terminology",
        ArticleScientificFigureCandidateKind.GravityOrbitFreeFall => "02-gravity-orbit-free-fall",
        ArticleScientificFigureCandidateKind.GravityElevatorFreeFall => "03-gravity-elevator-free-fall",
        ArticleScientificFigureCandidateKind.GravitySurfaceRotation => "04-gravity-surface-rotation",
        ArticleScientificFigureCandidateKind.GravityCaseComparison => "05-gravity-case-comparison",
        ArticleScientificFigureCandidateKind.GravityReferenceFrames => "06-gravity-reference-frames",
        ArticleScientificFigureCandidateKind.ThermistorCircuitDivider => "01-thermistor-circuit-divider",
        ArticleScientificFigureCandidateKind.ThermistorCurvature => "02-thermistor-curvature",
        ArticleScientificFigureCandidateKind.ThermistorError => "03-thermistor-error",
        ArticleScientificFigureCandidateKind.ThermistorSpecialValues => "04-thermistor-special-values",
        ArticleScientificFigureCandidateKind.ArchimedesDefinition => "01-archimedes-definition",
        ArticleScientificFigureCandidateKind.ArchimedesWaterModel => "02-archimedes-water-model",
        ArticleScientificFigureCandidateKind.ArchimedesBottomContact => "03-archimedes-bottom-contact",
        ArticleScientificFigureCandidateKind.ArchimedesDepthDependence => "04-archimedes-depth",
        ArticleScientificFigureCandidateKind.ArchimedesTopContact => "05-archimedes-top-contact",
        ArticleScientificFigureCandidateKind.ArchimedesPier => "06-archimedes-pier",
        ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat => "07-archimedes-pressure-caveat",
        ArticleScientificFigureCandidateKind.BernoulliFanEnergy => "01-bernoulli-fan-energy",
        ArticleScientificFigureCandidateKind.BernoulliFanZones => "02-bernoulli-fan-zones",
        ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary => "03-bernoulli-streamline-boundary",
        ArticleScientificFigureCandidateKind.PinholeGeometry => "01-pinhole-geometry",
        ArticleScientificFigureCandidateKind.PinholeFocusPlane => "02-pinhole-focus-plane",
        ArticleScientificFigureCandidateKind.PinholeObservation => "03-pinhole-observation",
        ArticleScientificFigureCandidateKind.SuperconductingEnergy => "01-superconducting-energy",
        ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent => "02-superconducting-persistent-current",
        ArticleScientificFigureCandidateKind.SuperconductingExcitation => "03-superconducting-excitation",
        ArticleScientificFigureCandidateKind.MeterTransientResponse => "01-meter-transient-response",
        ArticleScientificFigureCandidateKind.MeterTrialDecision => "02-meter-trial-decision",
        ArticleScientificFigureCandidateKind.MeterProtectionLayers => "03-meter-protection-layers",
        ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse => "01-boiling-pre-bubble-collapse",
        ArticleScientificFigureCandidateKind.BoilingBubbleGrowth => "02-boiling-bubble-growth",
        ArticleScientificFigureCandidateKind.BoilingPressureScale => "03-boiling-pressure-scale",
        ArticleScientificFigureCandidateKind.GalileanAfocalPath => "01-galilean-afocal-path",
        ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes => "02-galilean-virtual-object-regimes",
        ArticleScientificFigureCandidateKind.GalileanAngularMagnification => "03-galilean-angular-magnification",
        ArticleScientificFigureCandidateKind.DryIceWaterMechanism => "01-dry-ice-water-mechanism",
        ArticleScientificFigureCandidateKind.DryIceHeatTransferComparison => "02-dry-ice-heat-transfer",
        ArticleScientificFigureCandidateKind.DryIceIsolationVerification => "03-dry-ice-isolation",
        ArticleScientificFigureCandidateKind.LeverRockContact => "01-lever-rock-contact",
        ArticleScientificFigureCandidateKind.LeverSeesawFriction => "02-lever-seesaw-friction",
        ArticleScientificFigureCandidateKind.LeverTwoForceMember => "03-lever-two-force-member",
        ArticleScientificFigureCandidateKind.RestIntervalDefinition => "01-rest-interval-definition",
        ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint => "02-rest-zero-velocity",
        ArticleScientificFigureCandidateKind.RestStateComparison => "03-rest-state-comparison",
        _ => throw new ArgumentOutOfRangeException(nameof(candidate), candidate.Kind, null),
    };
}
