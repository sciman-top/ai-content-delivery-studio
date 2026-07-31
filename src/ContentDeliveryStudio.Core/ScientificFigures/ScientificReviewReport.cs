namespace ContentDeliveryStudio.Core.ScientificFigures;

public enum ScientificContractInvariant
{
    AuthorityIdentity = 0,
    RequiredElementCoverage = 1,
    NoExtraScientificContent = 2,
    ExactScientificContent = 3,
    RelationSemantics = 4,
    RelationDirection = 5,
    SvgAuthority = 6,
    ExportEquivalence = 7,
    VisualReadability = 8,
}

public enum ScientificContractRepairLayer
{
    FigureSpecification = 0,
    RenderPlanCompiler = 1,
    SvgRenderer = 2,
    Exporter = 3,
}

public sealed record ScientificContractFinding(
    string Code,
    ScientificContractInvariant Invariant,
    string ResponsibleItemId,
    string Evidence,
    ScientificContractRepairLayer RepairLayer);

public sealed record ScientificContractReviewReport
{
    private ScientificContractReviewReport(
        double advisoryScore,
        IReadOnlyList<ScientificContractFinding> hardFailures)
    {
        AdvisoryScore = advisoryScore;
        HardFailures = hardFailures;
    }

    public double AdvisoryScore { get; }

    public IReadOnlyList<ScientificContractFinding> HardFailures { get; }

    public bool Passed => HardFailures.Count == 0;

    public static ScientificContractReviewReport Create(
        double advisoryScore,
        IReadOnlyList<ScientificContractFinding> hardFailures)
    {
        if (!double.IsFinite(advisoryScore) || advisoryScore is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(advisoryScore),
                "Advisory score must be between zero and one.");
        }

        ArgumentNullException.ThrowIfNull(hardFailures);
        if (hardFailures.Any(finding => finding is null))
        {
            throw new ArgumentException(
                "Scientific contract findings cannot contain null entries.",
                nameof(hardFailures));
        }

        return new ScientificContractReviewReport(
            advisoryScore,
            Array.AsReadOnly(hardFailures.ToArray()));
    }
}
