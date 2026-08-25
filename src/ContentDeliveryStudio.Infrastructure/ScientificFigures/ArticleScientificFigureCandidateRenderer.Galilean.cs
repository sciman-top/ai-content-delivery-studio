using System.Xml.Linq;
using ContentDeliveryStudio.Core.ScientificFigures;
using SkiaSharp;
using FormulaPiece = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.FormulaPiece;
using MathRun = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.MathRun;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

// Domain slice of the article candidate renderer; the kind registry and shared
// drawing primitives live in ArticleScientificFigureCandidateRenderer.cs.

public sealed partial class ArticleScientificFigureCandidateRenderer
{
    private static void RenderGalileanAfocalPath(XElement g)
    {
        DrawGalileanTelescopeContext(g, 300, 810, 400, 55, 1107);
        DrawLens(g, 300, 180, 610, "物镜（凸）", Blue);
        DrawConcaveLens(g, 810, 255, 535, "目镜（凹）", Magenta);
        g.Add(ArticleLine(100, 200, 300, 200, Blue, 3, "incoming-ray", true));
        g.Add(ArticleLine(100, 400, 300, 400, Blue, 3, "incoming-ray", true));
        g.Add(ArticleLine(100, 600, 300, 600, Blue, 3, "incoming-ray", true));
        // Solid objective rays stop at the eyepiece.  The dashed continuation
        // shows the focus that would occur without it, avoiding the false
        // visual implication that the green rays pass through the eyepiece.
        g.Add(ArticleLine(300, 200, 810, 350, Green, 4, "objective-convergence", true));
        g.Add(ArticleLine(300, 400, 810, 400, Green, 4, "objective-convergence", true));
        g.Add(ArticleLine(300, 600, 810, 450, Green, 4, "objective-convergence", true));
        g.Add(DashedArticleLine(810, 350, 980, 400, Green, 3, "would-be-focus"));
        g.Add(DashedArticleLine(810, 400, 980, 400, Green, 3, "would-be-focus"));
        g.Add(DashedArticleLine(810, 450, 980, 400, Green, 3, "would-be-focus"));
        g.Add(ArticleLine(810, 350, 1080, 350, Magenta, 4, "afocal-output", true));
        g.Add(ArticleLine(810, 400, 1080, 400, Magenta, 4, "afocal-output", true));
        g.Add(ArticleLine(810, 450, 1080, 450, Magenta, 4, "afocal-output", true));
        g.Add(ArticleLine(980, 285, 980, 515, Amber, 2, "common-focal-plane"));
        DrawArticleCircle(g, 980, 400, 8, Amber, 3, "would-be-focus-point");
        g.Add(Text("物镜原本会聚的焦点（在该焦平面上）", 980, 610, 18, Amber, "middle"));
        g.Add(DrawObserverEye(1107, 400, "eye"));
        g.Add(Text("眼", 1107, 540, 18, Green, "middle"));
        g.Add(Text("远物近似平行光", 150, 155, 18, Blue));
        g.Add(Text("目镜在原会聚点之前截获光束；虚线表示未放入目镜时的会聚延长", 680, 210, 18, Magenta, "middle"));
        g.Add(Text("正常调焦：出射近似平行 → 眼在视网膜成实像；最终视野正立", 600, 700, 22, Ink, "middle"));
    }
    private static void RenderGalileanVirtualObjectRegimes(XElement g)
    {
        var panels = new[] { (55d, "s < F", "右侧实像", Blue), (425d, "s = F", "平行出射", Green), (795d, "s > F", "左侧虚像", Magenta) };
        foreach (var (x, condition, result, color) in panels)
        {
            g.Add(ArticleRect(x, 160, 350, 470, "#F8FAFC", color, 3, "regime-panel"));
            DrawGalileanTelescopeContext(g, x + 85, x + 175, 400, x + 25, x + 320);
            g.Add(Text(condition, x + 175, 205, 25, color, "middle"));
            DrawConcaveLens(g, x + 175, 260, 520, "凹目镜", color);
            g.Add(ArticleLine(x + 35, 300, x + 175, 355, Blue, 3, "converging-input", true));
            g.Add(ArticleLine(x + 35, 480, x + 175, 425, Blue, 3, "converging-input", true));
            if (condition == "s < F")
            {
                g.Add(ArticleLine(x + 175, 355, x + 315, 385, color, 3, "regime-output", true));
                g.Add(ArticleLine(x + 175, 425, x + 315, 385, color, 3, "regime-output", true));
                DrawArticleCircle(g, x + 315, 385, 8, color, 3, "image-point");
            }
            else if (condition == "s = F")
            {
                g.Add(ArticleLine(x + 175, 355, x + 320, 355, color, 3, "regime-output", true));
                g.Add(ArticleLine(x + 175, 425, x + 320, 425, color, 3, "regime-output", true));
            }
            else
            {
                g.Add(ArticleLine(x + 175, 355, x + 320, 330, color, 3, "regime-output", true));
                g.Add(ArticleLine(x + 175, 425, x + 320, 450, color, 3, "regime-output", true));
                g.Add(ArticleLine(x + 175, 355, x + 70, 373, color, 2, "virtual-extension"));
                g.Add(ArticleLine(x + 175, 425, x + 70, 407, color, 2, "virtual-extension"));
                DrawArticleCircle(g, x + 70, 390, 8, color, 3, "image-point");
            }
            g.Add(Text(result, x + 175, 585, 21, color, "middle"));
        }
        g.Add(Text("s：目镜到原会聚点的距离；F：凹目镜焦距的绝对值", 600, 675, 20, Ink, "middle"));
        g.Add(Text("望远镜正常无焦工作对应中间情形，不应把其他区间混作整机放大结论", 600, 720, 18, Amber, "middle"));
    }
    private static void RenderGalileanAngularMagnification(XElement g)
    {
        DrawGalileanTelescopeContext(g, 260, 800, 400, 55, 1105);
        g.Add(ArticleLine(110, 400, 1080, 400, Ink, 3, "optical-axis"));
        DrawLens(g, 260, 220, 580, "物镜", Blue);
        DrawConcaveLens(g, 800, 275, 525, "目镜", Magenta);
        g.Add(ArticleLine(260, 300, 930, 380, Blue, 3, "angle-ray", true));
        g.Add(ArticleLine(260, 500, 930, 420, Blue, 3, "angle-ray", true));
        g.Add(ArticleLine(800, 365, 1080, 365, Green, 4, "afocal-ray", true));
        g.Add(ArticleLine(800, 435, 1080, 435, Green, 4, "afocal-ray", true));
        g.Add(ArticleLine(260, 620, 800, 620, Amber, 4, "tube-length"));
        g.Add(ArticleLine(260, 600, 260, 640, Amber, 4, "tube-length"));
        g.Add(ArticleLine(800, 600, 800, 640, Amber, 4, "tube-length"));
        g.Add(Text("镜筒长度约为：物镜焦距 − 目镜焦距绝对值", 530, 665, 20, Amber, "middle"));
        g.Add(ArticleRect(830, 160, 300, 115, "#ECFDF5", Green, 3, "magnification-card"));
        g.Add(Text("角放大率（正立）", 980, 205, 20, Green, "middle"));
        g.Add(Text("物镜焦距 ÷ 目镜焦距绝对值", 980, 245, 18, Ink, "middle"));
        g.Add(Text("焦面匹配 → 近似平行出射 → 眼睛放松观察", 600, 710, 21, Ink, "middle"));
        g.Add(Text("“焦点重合”是无焦条件；放大率仍由焦距比决定", 600, 755, 18, Magenta, "middle"));
    }
    private static void DrawGalileanTelescopeContext(
        XElement group,
        double objectiveX,
        double eyepieceX,
        double axisY,
        double distantObjectX,
        double eyeX)
    {
        var tubeTop = axisY - 105;
        group.Add(ArticleRect(objectiveX - 18, tubeTop, Math.Max(60, eyepieceX - objectiveX + 36), 210, "#F8FAFC", "#94A3B8", 3, "telescope-tube"));
        group.Add(ArticleRect(objectiveX - 22, axisY - 120, 44, 240, "#EFF6FF", Blue, 3, "objective-mount"));
        group.Add(ArticleRect(eyepieceX - 18, axisY - 95, 36, 190, "#FDF2F8", Magenta, 3, "eyepiece-mount"));
        var tubeConnection = ArticleLine(objectiveX, axisY + 112, eyepieceX, axisY + 112, "#64748B", 4, "tube-link");
        tubeConnection.SetAttributeValue("data-article-connection", "objective-eyepiece-tube");
        group.Add(tubeConnection);
        group.Add(ArticleLine(distantObjectX, axisY + 90, distantObjectX, axisY - 55, Green, 5, "distant-object"));
        group.Add(ArticleLine(distantObjectX, axisY - 45, distantObjectX - 28, axisY - 5, Green, 4, "distant-object"));
        group.Add(ArticleLine(distantObjectX, axisY - 45, distantObjectX + 28, axisY - 5, Green, 4, "distant-object"));
        group.Add(DrawObserverEye(eyeX, axisY, "observer-eye"));
        var eyePath = ArticleLine(eyepieceX, axisY, eyeX - 18, axisY, Green, 3, "viewing-axis", true);
        eyePath.SetAttributeValue("data-article-connection", "eyepiece-eye-path");
        group.Add(eyePath);
    }
}
