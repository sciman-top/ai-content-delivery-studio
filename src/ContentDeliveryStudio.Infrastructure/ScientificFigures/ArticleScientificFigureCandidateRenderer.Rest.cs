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
    private static void RenderRestIntervalDefinition(XElement group)
    {
        group.Add(Text("静止判断的是一段时间内的位置关系", 600, 132, 22, Ink, "middle"));
        group.Add(ArticleRect(70, 180, 500, 420, "#EFF6FF", "#93C5FD", 2, "reference-frame"));
        group.Add(ArticleRect(630, 180, 500, 420, "#FFF7ED", "#FDBA74", 2, "reference-frame"));
        group.Add(Text("同一参考系：静止", 320, 225, 24, Blue, "middle"));
        group.Add(Text("同一参考系：运动", 880, 225, 24, Amber, "middle"));
        group.Add(ArticleLine(115, 500, 520, 500, Ink, 3, "time-axis", true));
        group.Add(ArticleLine(675, 500, 1080, 500, Ink, 3, "time-axis", true));
        foreach (var x in new[] { 160d, 250d, 340d, 430d })
            group.Add(ArticleLine(x, 492, x, 508, Muted, 2, "time-axis"));
        foreach (var x in new[] { 720d, 810d, 900d, 990d })
            group.Add(ArticleLine(x, 492, x, 508, Muted, 2, "time-axis"));
        foreach (var x in new[] { 160d, 250d, 340d, 430d })
            group.Add(ArticleRect(x, 380, 34, 60, "#2563EB", Blue, 2, "rest-positions"));
        foreach (var (x, y) in new[] { (720d, 410d), (810d, 370d), (900d, 330d), (990d, 290d) })
            group.Add(ArticleRect(x, y, 34, 60, "#F59E0B", Amber, 2, "motion-positions"));
        foreach (var (x1, x2, role, connection) in new[] { (160d, 464d, "interval-bracket", "rest-zero-displacement"), (720d, 1024d, "interval-bracket", "motion-position-change") })
        {
            var bracket = ArticleLine(x1, 545, x2, 545, role == "interval-bracket" && x1 < 500 ? Blue : Amber, 4, role, true);
            bracket.SetAttributeValue("data-article-connection", connection);
            group.Add(bracket);
        }
        group.Add(Text("位置不变，x(t)=x0，Δx=0", 320, 330, 20, Blue, "middle"));
        group.Add(Text("位置随时间变化，Δx≠0", 880, 330, 20, Amber, "middle"));
        group.Add(Text("时间区间", 600, 650, 18, Muted, "middle"));
        group.Add(Text("静止不是某一个瞬时点的标签", 600, 690, 19, Magenta, "middle"));
    }
    private static void RenderRestZeroVelocityTurningPoint(XElement group)
    {
        group.Add(Text("上抛顶点：v=0，但运动过程没有中断", 600, 132, 22, Ink, "middle"));
        group.Add(ArticleLine(135, 560, 1080, 560, Muted, 2, "time-sequence", true));
        group.Add(ArticleLine(170, 475, 360, 330, Blue, 4, "trajectory"));
        group.Add(ArticleLine(360, 330, 550, 260, Blue, 4, "trajectory"));
        group.Add(ArticleLine(550, 260, 740, 330, Blue, 4, "trajectory"));
        group.Add(ArticleLine(740, 330, 930, 475, Blue, 4, "trajectory"));
        group.Add(ArticleRect(330, 395, 34, 50, "#60A5FA", Blue, 2, "object-before"));
        group.Add(ArticleRect(533, 235, 34, 50, "#F0ABFC", Magenta, 2, "object-apex"));
        group.Add(ArticleRect(913, 395, 34, 50, "#60A5FA", Blue, 2, "object-after"));
        group.Add(Text("上抛物体", 550, 180, 20, Blue, "middle"));
        group.Add(Text("前", 347, 470, 18, Blue, "middle"));
        group.Add(Text("最高点", 550, 220, 18, Magenta, "middle"));
        group.Add(Text("后", 930, 470, 18, Blue, "middle"));
        group.Add(ArticleLine(347, 395, 347, 320, Blue, 4, "velocity-arrow", true));
        group.Add(ArticleLine(930, 445, 930, 520, Blue, 4, "velocity-arrow", true));
        group.Add(Text("v≠0", 370, 340, 18, Blue));
        group.Add(Text("v=0", 580, 300, 20, Magenta));
        var gravity = ArticleLine(550, 285, 550, 385, Amber, 5, "acceleration-arrow", true);
        gravity.SetAttributeValue("data-article-connection", "gravity-downward");
        group.Add(gravity);
        group.Add(Text("a向下", 570, 380, 19, Amber));
        var positions = ArticleLine(347, 600, 930, 600, Green, 4, "time-sequence", true);
        positions.SetAttributeValue("data-article-connection", "positions-change");
        group.Add(positions);
        group.Add(Text("前后位置不同：Δx≠0", 600, 665, 20, Green, "middle"));
        var zero = ArticleLine(535, 300, 565, 300, Magenta, 4, "velocity-arrow");
        zero.SetAttributeValue("data-article-connection", "apex-velocity-zero");
        group.Add(zero);
    }
    private static void RenderRestStateComparison(XElement group)
    {
        group.Add(Text("三种状态的边界：静止是匀速的特例", 600, 132, 22, Ink, "middle"));
        var lanes = new[] { (Y: 210d, Title: "静止", Role: "rest-lane", Color: Blue), (Y: 350d, Title: "匀速直线运动", Role: "uniform-lane", Color: Green), (Y: 490d, Title: "变速运动", Role: "accelerated-lane", Color: Magenta) };
        foreach (var lane in lanes)
        {
            group.Add(ArticleRect(70, lane.Y, 1060, 105, "#FFFFFF", "#CBD5E1", 2, "state-lane"));
            group.Add(ArticleLine(250, lane.Y + 10, 1030, lane.Y + 10, lane.Color, 2, lane.Role));
            group.Add(Text(lane.Title, 120, lane.Y + 42, 21, lane.Color));
            group.Add(ArticleLine(250, lane.Y + 68, 1030, lane.Y + 68, lane.Color, 3, "position-trace", true));
            group.Add(ArticleRect(310, lane.Y + 49, 28, 38, lane.Color, lane.Color, 1, "position-trace"));
            group.Add(ArticleRect(lane.Role == "accelerated-lane" ? 860 : 710, lane.Y + 49, 28, 38, lane.Color, lane.Color, 1, "position-trace"));
            group.Add(ArticleRect(250, lane.Y + 92, 780, 1, "none", lane.Color, 1, "derivative-note"));
        }
        group.Add(Text("x(t)=x0；v=0；所有阶导数为零", 700, 246, 18, Blue, "middle"));
        group.Add(Text("x(t)=x0+vt；a及以上为零", 700, 386, 18, Green, "middle"));
        group.Add(Text("即使某时刻 v=0，位置仍会继续变化", 700, 526, 18, Magenta, "middle"));
        group.Add(Text("高阶导数决定是否持续变速", 650, 608, 17, Magenta, "middle"));
        var special = ArticleLine(338, 535, 860, 535, Magenta, 4, "state-link", true);
        special.SetAttributeValue("data-article-connection", "zero-velocity-not-rest");
        group.Add(special);
        var caseLink = ArticleLine(250, 300, 250, 350, Blue, 4, "state-link", true);
        caseLink.SetAttributeValue("data-article-connection", "rest-special-case");
        group.Add(caseLink);
        group.Add(Text("静止 = v=0 的匀速直线运动特例（按时间过程定义）", 600, 690, 19, Ink, "middle"));
    }
}
