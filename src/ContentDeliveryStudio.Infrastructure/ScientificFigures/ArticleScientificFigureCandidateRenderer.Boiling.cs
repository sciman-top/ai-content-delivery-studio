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
    private static void RenderBoilingPreBubbleCollapse(XElement g)
    {
        g.Add(ArticleRect(210, 160, 560, 500, "#EFF6FF", Blue, 5, "vessel"));
        g.Add(ArticleRect(215, 165, 550, 225, "#DBEAFE", Blue, 0, "cool-water"));
        g.Add(ArticleRect(215, 390, 550, 265, "#FEE2E2", Magenta, 0, "hot-water"));
        DrawBoilingApparatusDetails(g, 210, 160, 560, 500, 165, 665);
        g.Add(Text("上层较冷：局部蒸气压较低", 490, 205, 20, Blue, "middle"));
        g.Add(Text("下层较热：可形成富含水蒸气的气泡", 490, 625, 19, Magenta, "middle"));
        var bubbles = new[] { (490d,550d,58d), (490d,455d,45d), (490d,365d,32d), (490d,285d,18d) };
        foreach (var (x,y,r) in bubbles) DrawArticleCircle(g, x, y, r, Green, 4, "shrinking-bubble");
        for (var y = 505d; y >= 315d; y -= 65d)
            g.Add(ArticleLine(570, y, 570, y - 45, Green, 4, "rise-path", true));
        g.Add(ArticleLine(475, 350, 405, 325, Blue, 4, "condensation-flux", true));
        g.Add(ArticleLine(505, 350, 575, 325, Blue, 4, "condensation-flux", true));
        g.Add(Text("凝结通量离开泡内", 650, 330, 19, Blue));
        g.Add(ArticleRect(820, 210, 300, 300, "#FFF7ED", Amber, 3, "origin-caveat"));
        g.Add(Text("先辨别气泡来源", 970, 255, 22, Amber, "middle"));
        g.Add(Text("水蒸气泡：遇冷可明显收缩", 970, 325, 18, Ink, "middle"));
        g.Add(Text("溶解气体析出：机制不同", 970, 380, 18, Ink, "middle"));
        g.Add(Text("不能把所有沸腾前小泡", 970, 438, 17, Magenta, "middle"));
        g.Add(Text("都自动等同为水蒸气泡", 970, 470, 17, Magenta, "middle"));
        g.Add(Text("净凝结 > 净汽化 → 泡内蒸气质量减少 → 半径缩小", 600, 755, 22, Ink, "middle"));
    }
    private static void RenderBoilingBubbleGrowth(XElement g)
    {
        g.Add(ArticleRect(150, 160, 620, 500, "#EFF6FF", Blue, 5, "vessel"));
        DrawBoilingApparatusDetails(g, 150, 160, 620, 500, 210, 665);
        // Keep the state label away from the largest bubble so it remains legible
        // while the bubble-size progression is visible without explanatory prose.
        g.Add(Text("接近均匀沸腾温度的水", 645, 205, 21, Blue, "middle"));
        var bubbles = new[] { (460d,555d,25d), (460d,460d,38d), (460d,350d,54d), (460d,235d,68d) };
        foreach (var (x,y,r) in bubbles) DrawArticleCircle(g, x, y, r, Green, 4, "growing-bubble");
        for (var y = 520d; y >= 330d; y -= 75d)
            g.Add(ArticleLine(550, y, 550, y - 52, Green, 4, "rise-path", true));
        foreach (var (x1,y1,x2,y2) in new[] { (350d,350d,405d,350d), (570d,350d,515d,350d), (460d,270d,460d,310d), (460d,430d,460d,395d) })
            g.Add(ArticleLine(x1,y1,x2,y2,Magenta,4,"vaporization-flux",true));
        g.Add(Text("液体在气泡界面净汽化，向泡内补充水蒸气", 460, 620, 19, Magenta, "middle"));
        g.Add(ArticleRect(820, 205, 300, 330, "#ECFDF5", Green, 3, "pressure-balance"));
        g.Add(Text("气泡能存在的条件", 970, 252, 23, Green, "middle"));
        g.Add(Text("泡内蒸气压", 970, 315, 20, Ink, "middle"));
        g.Add(Text("≈ 外界压强 + 表面张力项", 970, 360, 18, Ink, "middle"));
        g.Add(Text("上升时外压略降", 970, 430, 18, Blue, "middle"));
        g.Add(Text("但浅水中量级通常很小", 970, 475, 18, Amber, "middle"));
        g.Add(Text("沸腾判据：液体平衡蒸气压达到周围压强", 600, 755, 22, Ink, "middle"));
    }
    private static void RenderBoilingPressureScale(XElement g)
    {
        g.Add(ArticleRect(85, 180, 490, 390, "#EFF6FF", Blue, 3, "pressure-column"));
        g.Add(Text("10 cm 浅水示例", 330, 225, 24, Blue, "middle"));
        g.Add(ArticleRect(155, 280, 95, 235, "#DBEAFE", Blue, 3, "water-depth"));
        DrawBoilingApparatusDetails(g, 155, 280, 95, 235, 280, 535);
        // The pressure scale is an explanatory comparison, but it must still show
        // the physical object whose behaviour is being discussed rather than
        // collapsing into a label-only list of quantities.
        DrawArticleCircle(g, 202, 455, 17, Green, 3, "reference-bubble");
        DrawArticleCircle(g, 202, 350, 22, Green, 3, "reference-bubble");
        g.Add(ArticleLine(202, 492, 202, 385, Green, 3, "bubble-rise", true));
        g.Add(Text("气泡上升", 240, 435, 17, Green));
        g.Add(ArticleLine(270, 280, 270, 515, Amber, 3, "depth-bracket"));
        g.Add(Text("h = 0.10 m", 285, 405, 20, Amber));
        g.Add(Text("ρgh ≈ 0.98 kPa", 420, 330, 21, Ink, "middle"));
        g.Add(Text("大气压约 101 kPa", 420, 385, 21, Ink, "middle"));
        g.Add(Text("底/面绝对压强比约 1.01", 420, 450, 20, Green, "middle"));
        g.Add(ArticleLine(155, 280, 250, 280, Amber, 3, "pressure-column", true));
        g.Add(ArticleLine(155, 515, 250, 515, Amber, 3, "pressure-column", true));
        g.Add(ArticleRect(625, 180, 490, 390, "#FFF7ED", Amber, 3, "causal-balance"));
        g.Add(Text("决定气泡大小的量", 870, 225, 24, Amber, "middle"));
        var labels = new[] { "泡内蒸气质量", "局部温度与蒸气压", "静水压与大气压", "表面张力（小泡更重要）" };
        for (var i = 0; i < labels.Length; i++)
        {
            g.Add(ArticleRect(675, 270 + i * 65, 390, 45, "#FFFFFF", i == 0 ? Magenta : Blue, 2, "causal-factor"));
            g.Add(Text(labels[i], 870, 300 + i * 65, 18, Ink, "middle"));
        }
        g.Add(ArticleLine(575, 375, 625, 375, Green, 5, "scale-link", true));
        g.Add(Text("浅水压强差可定量估算，但不能单独解释全部可见增长", 600, 635, 22, Ink, "middle"));
        g.Add(Text("温度变化与相变质量交换必须同时进入因果图", 600, 690, 20, Magenta, "middle"));
    }
    private static void DrawBoilingApparatusDetails(
        XElement group,
        double left,
        double top,
        double width,
        double height,
        double waterSurfaceY,
        double heaterY)
    {
        group.Add(ArticleRect(left, top, width, height, "none", Blue, 4, "beaker-glass"));
        group.Add(ArticleLine(left + 5, waterSurfaceY, left + width - 5, waterSurfaceY, Blue, 3, "water-surface"));
        var thermometerX = left + width - Math.Min(38, width * 0.28);
        var thermometer = ArticleLine(thermometerX, top + 58, thermometerX, top + height - 72, Magenta, 5, "thermometer");
        thermometer.SetAttributeValue("data-article-connection", "thermometer-in-water");
        group.Add(thermometer);
        group.Add(DrawArticleCircleElement(thermometerX, top + height - 72, 9, Magenta, 3, "thermometer"));
        group.Add(ArticleRect(left + (width * 0.31), heaterY, width * 0.38, 34, "#FDE68A", Amber, 3, "heater"));
        group.Add(ArticleLine(left + (width * 0.38), heaterY + 17, left + (width * 0.62), heaterY + 17, Magenta, 3, "heater"));
        var heatToWater = ArticleLine(left + (width * 0.5), heaterY, left + (width * 0.5), top + height - 12, Amber, 4, "heat-flux", true);
        heatToWater.SetAttributeValue("data-article-connection", "heat-to-water");
        group.Add(heatToWater);
    }
}
