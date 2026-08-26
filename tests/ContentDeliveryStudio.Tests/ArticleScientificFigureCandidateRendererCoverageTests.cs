using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

/// <summary>
/// Guards the renderer dispatch registry against drift: after the domain
/// renderer split, a candidate kind added to the enum (or to a planner)
/// without a registry entry fails only at production render time, and the
/// Tools prefix switch's discard arm suppresses compile-time exhaustiveness.
/// Enumerating the whole kind list keeps the registry provably complete.
/// </summary>
public sealed class ArticleScientificFigureCandidateRendererCoverageTests
{
    [Theory]
    [MemberData(nameof(NonBoardCandidateKinds))]
    public void Render_SupportsEveryNonBoardCandidateKind(ArticleScientificFigureCandidateKind kind)
    {
        var candidate = CreateCandidate(kind);

        var renderer = new ArticleScientificFigureCandidateRenderer();
        foreach (var presentationAttempt in new[] { 1, 2, 3 })
        {
            var artifact = renderer.Render(candidate, presentationAttempt);
            Assert.False(string.IsNullOrWhiteSpace(artifact.Svg));
            Assert.NotEqual(string.Empty, artifact.Sha256);
        }
    }

    [Fact]
    public void Render_StillRejectsSourceEvidenceBoard()
    {
        var candidate = new ArticleScientificFigureCandidate(
            "coverage-source-evidence-board",
            "coverage-article-title",
            ArticleScientificFigureCandidateKind.SourceEvidenceBoard,
            "Coverage evidence board",
            "The board must keep source pixels.",
            "Boards cannot use the vector renderer.",
            "coverage",
            ScientificFigureRiskLevel.High,
            [],
            [],
            ArticleScientificFigureDisposition.ConsolidateSourceEvidence,
            "Registry coverage probe.",
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);

        Assert.Throws<InvalidOperationException>(
            () => new ArticleScientificFigureCandidateRenderer().Render(candidate, 1));
    }

    [Theory]
    [MemberData(nameof(NonBoardCandidateKinds))]
    public void FileNamePrefix_ResolvesEveryNonBoardCandidateKind(ArticleScientificFigureCandidateKind kind)
    {
        var candidate = CreateCandidate(kind);
        var prefix = ArticleScientificFigureFileNaming.GetFileNamePrefix(candidate, evidenceBoardPrefix: "06-source-evidence-board");

        Assert.False(string.IsNullOrWhiteSpace(prefix));
    }

    [Fact]
    public void FileNamePrefix_PassesEvidenceBoardSlotThroughAndStaysGloballyDistinct()
    {
        var boardPrefix = "07-source-evidence-board";
        var board = ArticleScientificFigureFileNaming.GetFileNamePrefix(
            CreateCandidate(ArticleScientificFigureCandidateKind.SourceEvidenceBoard), boardPrefix);
        Assert.Equal(boardPrefix, board);

        // Delivery files are named "<prefix>.<ext>", so two kinds colliding on
        // one prefix would silently overwrite each other's artifacts.
        var prefixes = Enum.GetValues<ArticleScientificFigureCandidateKind>()
            .Select(kind => ArticleScientificFigureFileNaming.GetFileNamePrefix(
                CreateCandidate(kind),
                EvidenceBoardPrefixForBoardKinds()))
            .ToArray();
        Assert.Equal(prefixes.Length, prefixes.Distinct().Count());
    }

    private static string EvidenceBoardPrefixForBoardKinds() => "06-source-evidence-board";

    private static ArticleScientificFigureCandidate CreateCandidate(ArticleScientificFigureCandidateKind kind) => new(
        $"coverage-{kind.ToString().ToLowerInvariant()}",
        "coverage-article-title",
        kind,
        $"Coverage figure for {kind}",
        "Verify the naming map has an entry for this kind.",
        "The naming map must resolve every candidate kind.",
        "coverage",
        ScientificFigureRiskLevel.High,
        [],
        [],
        ArticleScientificFigureDisposition.ReplaceExisting,
        "Registry coverage probe.",
        RequiresGateOneApproval: true,
        GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
        DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);

    public static TheoryData<ArticleScientificFigureCandidateKind> NonBoardCandidateKinds
    {
        get
        {
            var kinds = new TheoryData<ArticleScientificFigureCandidateKind>();
            foreach (var kind in Enum.GetValues<ArticleScientificFigureCandidateKind>())
            {
                if (kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }
    }
}
