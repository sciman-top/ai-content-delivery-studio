using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

/// <summary>
/// Guards text measurement for SVG labels. Mixed Chinese text with arrows and
/// subscripts has no single installed typeface covering every glyph on some
/// hosts (the CI runner), so measurement must compose per-run fallbacks rather
/// than fail, while a character no font can draw must still be rejected.
/// </summary>
public sealed class ArticleScientificFigureTextMeasurementTests
{
    [Fact]
    public void Render_MeasuresMixedScriptTextThatNoSingleFontCovers()
    {
        var candidate = CreateCandidate("干冰 → CO₂(g)");

        var artifact = new ArticleScientificFigureCandidateRenderer().Render(candidate, 1);

        Assert.Contains("data-text-bounds=", artifact.Svg);
        Assert.Contains("CO₂(g)", artifact.Svg);
    }

    [Fact]
    public void Render_StillRejectsTextWithCharacterNoInstalledFontCanDraw()
    {
        // U+0378 is unassigned, so no installed font carries a glyph for it.
        var candidate = CreateCandidate("干冰 ͸");

        Assert.Throws<InvalidOperationException>(
            () => new ArticleScientificFigureCandidateRenderer().Render(candidate, 1));
    }

    private static ArticleScientificFigureCandidate CreateCandidate(string title) => new(
        "text-measurement-probe",
        "text-measurement-article",
        Enum.GetValues<ArticleScientificFigureCandidateKind>()
            .First(kind => kind != ArticleScientificFigureCandidateKind.SourceEvidenceBoard),
        title,
        "Verify label text measurement for mixed-script titles.",
        "Measurement must not depend on a single installed typeface.",
        "text-measurement",
        ScientificFigureRiskLevel.High,
        [],
        [],
        ArticleScientificFigureDisposition.ReplaceExisting,
        "Text measurement probe.",
        RequiresGateOneApproval: true,
        GateOneStatus: ArticleScientificFigureGateStatus.PendingHumanApproval,
        DeliveryStatus: ArticleScientificFigureDeliveryStatus.NotCreated);
}
