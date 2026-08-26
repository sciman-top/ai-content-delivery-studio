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
        var candidate = new ArticleScientificFigureCandidate(
            $"coverage-{kind.ToString().ToLowerInvariant()}",
            "coverage-article-title",
            kind,
            $"Coverage figure for {kind}",
            "Verify the dispatch registry has an entry for this kind.",
            "The registry must render every non-board candidate kind.",
            "coverage",
            ScientificFigureRiskLevel.High,
            [],
            [],
            ArticleScientificFigureDisposition.ReplaceExisting,
            "Registry coverage probe.",
            RequiresGateOneApproval: true,
            GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
            DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);

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
