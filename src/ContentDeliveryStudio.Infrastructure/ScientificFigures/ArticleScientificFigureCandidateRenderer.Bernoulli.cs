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
    private static void RenderBernoulliFanEnergy(XElement g)
    {
        g.Add(Rect(70, 175, 1060, 420, "#F8FAFC", "#CBD5E1", 2));
        g.Add(Text("进气", 125, 245, 22, Blue));
        for (var y = 285; y <= 465; y += 60) g.Add(ArticleLine(100, y, 300, y, Blue, 4, "intake-flow", true));
        g.Add(ArticleRect(300, 230, 250, 290, "#EFF6FF", Blue, 3, "fan-body"));
        g.Add(Text("电动风机", 425, 275, 25, Blue, "middle"));
        DrawCircle(g, 425, 380, 76, Blue, 4);
        for (var angle = 0; angle < 360; angle += 60)
        {
            var radians = angle * Math.PI / 180;
            g.Add(ArticleLine(425, 380, 425 + 62 * Math.Cos(radians), 380 + 62 * Math.Sin(radians), Blue, 8, "fan-blade"));
        }
        g.Add(ArticleLine(425, 180, 425, 230, Amber, 5, "electrical-work", true));
        g.Add(Text("电功输入", 425, 165, 20, Amber, "middle"));
        g.Add(ArticleRect(550, 260, 470, 240, "#ECFEFF", Green, 3, "outlet-channel"));
        g.Add(Text("出风通道", 785, 300, 22, Green, "middle"));
        for (var y = 345; y <= 435; y += 45) g.Add(ArticleLine(580, y, 980, y, Green, 5, "outlet-flow", true));
        g.Add(Text("风机做功使气流总能跨风机上升", 600, 645, 24, Ink, "middle"));
        g.Add(Text("同一无功流段才可比较静压、动能与高度项", 600, 695, 19, Amber, "middle"));
    }
    private static void RenderBernoulliFanZones(XElement g)
    {
        g.Add(Rect(75, 180, 1050, 390, "#F8FAFC", "#CBD5E1", 2));
        g.Add(ArticleLine(100, 260, 1000, 260, Ink, 5, "duct-wall"));
        g.Add(ArticleLine(100, 500, 1000, 500, Ink, 5, "duct-wall"));
        g.Add(Text("外界大气", 80, 620, 18, Muted));
        g.Add(ArticleRect(420, 260, 150, 240, "#FFF7ED", Amber, 3, "fan-body"));
        DrawArticleCircle(g, 495, 380, 60, Amber, 4, "fan-blade");
        g.Add(ArticleLine(445, 330, 545, 430, Amber, 5, "fan-blade"));
        g.Add(ArticleLine(545, 330, 445, 430, Amber, 5, "fan-blade"));
        g.Add(Text("风机", 495, 470, 22, Amber, "middle"));
        for (var y = 315; y <= 445; y += 65)
        {
            g.Add(ArticleLine(125, y, 390, y, Blue, 4, "suction-flow", true));
            g.Add(ArticleLine(600, y, 950, y, Magenta, 5, "compression-flow", true));
        }
        g.Add(Text("吸风区：静压低于大气压", 255, 220, 23, Blue, "middle"));
        g.Add(Text("压缩区：静压高于大气压", 790, 220, 23, Magenta, "middle"));
        g.Add(Text("风机位置决定 b 点属于低压侧还是高压侧", 600, 640, 23, Ink, "middle"));
        g.Add(Text("题图未给风机位置，因此不能仅由速度排序确定最大静压", 600, 690, 19, Amber, "middle"));
    }
    private static void RenderBernoulliStreamlineBoundary(XElement g)
    {
        g.Add(Rect(70, 155, 1060, 490, "#F8FAFC", "#CBD5E1", 2));
        g.Add(Text("同一流线内（无风机做功的流段）", 330, 205, 21, Blue, "middle"));
        // Draw the actual converging/diverging duct boundary so the area change is
        // visible even when all explanatory labels are hidden.
        g.Add(ArticleLine(100, 250, 330, 275, Ink, 4, "duct-wall"));
        g.Add(ArticleLine(330, 275, 585, 250, Ink, 4, "duct-wall"));
        g.Add(ArticleLine(100, 440, 330, 385, Ink, 4, "duct-wall"));
        g.Add(ArticleLine(330, 385, 585, 440, Ink, 4, "duct-wall"));
        g.Add(Text("A₁", 115, 238, 18, Ink, "middle"));
        g.Add(Text("A₂（喉部）", 330, 420, 18, Ink, "middle"));
        g.Add(Text("A₃", 570, 238, 18, Ink, "middle"));
        for (var offset = -70; offset <= 70; offset += 70)
        {
            g.Add(ArticleLine(105, 345 + offset, 330, 330 + offset / 2, Blue, 3, "same-streamline"));
            g.Add(ArticleLine(330, 330 + offset / 2, 585, 345 + offset, Blue, 3, "same-streamline", true));
        }
        g.Add(Text("截面变窄：速度增大、静压降低", 345, 475, 19, Blue, "middle"));
        g.Add(ArticleLine(330, 275, 330, 385, Amber, 2, "throat-section"));
        g.Add(ArticleLine(650, 275, 1080, 275, Green, 4, "free-jet", true));
        g.Add(ArticleLine(650, 415, 1080, 415, Green, 4, "free-jet", true));
        g.Add(Text("自由射流出口", 850, 205, 21, Green, "middle"));
        g.Add(Text("出口静压 ≈ 大气压", 850, 485, 22, Green, "middle"));
        g.Add(ArticleLine(615, 175, 615, 565, Amber, 2, "comparison-boundary"));
        g.Add(Text("不能跨不同流线或跨风机直接比较", 600, 610, 21, Amber, "middle"));
        g.Add(Text("A、C、D 的空间邻近不等于属于同一流线", 600, 700, 20, Ink, "middle"));
    }
}
