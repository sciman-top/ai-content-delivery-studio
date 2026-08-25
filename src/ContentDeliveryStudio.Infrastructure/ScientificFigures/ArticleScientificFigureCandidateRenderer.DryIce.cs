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
    private static void RenderDryIceWaterMechanism(XElement group)
    {
        group.Add(Text("水中相变与气体逸出", 600, 132, 22, Green, "middle"));
        group.Add(ArticleRect(150, 190, 390, 430, "none", Blue, 4, "beaker"));
        group.Add(ArticleRect(158, 350, 374, 262, "#BAE6FD", Blue, 1, "water"));
        group.Add(Text("水", 195, 390, 21, Blue));
        group.Add(ArticleRect(270, 468, 150, 108, "#E0F2FE", "#0EA5E9", 4, "ice-shell"));
        group.Add(ArticleRect(292, 490, 106, 72, "#CBD5E1", Ink, 3, "dry-ice"));
        group.Add(Text("干冰", 345, 533, 20, Ink, "middle"));
        var contact = ArticleLine(345, 468, 345, 440, Amber, 4, "dry-ice-water-contact", true);
        contact.SetAttributeValue("data-article-connection", "dry-ice-water-contact");
        group.Add(contact);
        group.Add(Text("接触处先凝固成薄冰", 345, 425, 16, Amber, "middle"));

        foreach (var (x, y, r) in new[] { (235d, 470d, 16d), (465d, 480d, 14d), (250d, 405d, 12d), (435d, 385d, 18d) })
            group.Add(DrawArticleCircleElement(x, y, r, Green, 3, "co2-bubble"));
        foreach (var (x, y) in new[] { (330d, 430d), (370d, 410d), (305d, 385d), (395d, 360d), (350d, 335d) })
            group.Add(ArticleRect(x, y, 14, 14, "#FFFFFF", Blue, 2, "ice-crystal"));
        foreach (var (x1, y1, x2, y2) in new[] { (345d, 450d, 330d, 330d), (360d, 450d, 370d, 300d), (380d, 450d, 410d, 345d) })
        {
            var path = ArticleLine(x1, y1, x2, y2, Green, 3, "crystal-egress", true);
            path.SetAttributeValue("data-article-connection", "gas-carries-crystals");
            group.Add(path);
        }
        group.Add(Text("二氧化碳气泡（CO2）把冰晶带出水面", 355, 270, 20, Green, "middle"));
        group.Add(Text("白烟≠液态水雾", 760, 270, 28, Magenta, "middle"));
        group.Add(Text("可见颗粒主要是小冰晶；气泡本身是二氧化碳", 760, 330, 18, Ink, "middle"));
        group.Add(ArticleRect(600, 390, 330, 150, "#FDF2F8", "#F0ABFC", 2, "phase-change-summary"));
        group.Add(Text("水 → 冰晶 / 冰壳", 765, 440, 24, Blue, "middle"));
        group.Add(Text("干冰 → CO₂(g)", 765, 490, 23, Green, "middle"));
        group.Add(Text("两条物质路径不可混为一谈", 765, 585, 18, Amber, "middle"));
    }
    private static void RenderDryIceHeatTransferComparison(XElement group)
    {
        group.Add(Text("同一干冰，不同介质的供热与现象", 600, 132, 22, Ink, "middle"));
        var panels = new[] { (Left: 55d, Title: "空气", Fill: "#EFF6FF", Role: "air-vessel"), (Left: 415d, Title: "水", Fill: "#E0F2FE", Role: "water-vessel"), (Left: 775d, Title: "油/酒精", Fill: "#FEF3C7", Role: "oil-vessel") };
        foreach (var panel in panels)
        {
            group.Add(ArticleRect(panel.Left, 175, 320, 420, "#FFFFFF", "#CBD5E1", 2, "comparison-panel"));
            group.Add(Text(panel.Title, panel.Left + 160, 215, 24, Ink, "middle"));
            group.Add(ArticleRect(panel.Left + 50, 280, 220, 220, panel.Fill, Blue, 3, panel.Role));
            group.Add(ArticleRect(panel.Left + 125, 395, 70, 60, "#CBD5E1", Ink, 3, "dry-ice"));
        }
        foreach (var (x1, x2, role, connection) in new[] { (100d, 175d, "heat-arrow", "air-heat"), (460d, 590d, "heat-arrow", "water-heat"), (820d, 900d, "heat-arrow", "oil-heat") })
        {
            for (var offset = 0; offset < 2; offset++)
            {
                var arrow = ArticleLine(x1 + offset * 35, 330 + offset * 70, x2 + offset * 35, 360 + offset * 70, Amber, 4, role, true);
                arrow.SetAttributeValue("data-article-connection", connection);
                group.Add(arrow);
            }
        }
        foreach (var (x, y) in new[] { (560d, 320d), (610d, 350d), (600d, 390d), (650d, 365d) })
            group.Add(ArticleRect(x, y, 12, 12, "#FFFFFF", Blue, 2, "smoke-crystal"));
        foreach (var (x, y) in new[] { (875d, 330d), (930d, 390d), (1000d, 350d), (955d, 430d) })
            group.Add(DrawArticleCircleElement(x, y, 10, Green, 3, "clear-bubble"));
        group.Add(Text("白烟较少", 215, 545, 18, Blue, "middle"));
        group.Add(Text("白烟明显：水供热强且可凝固放热", 575, 545, 17, Green, "middle"));
        group.Add(Text("多为清澈气泡；介质条件不同", 935, 545, 17, Amber, "middle"));
        group.Add(Text("传热速率取决于 h、A、ΔT 与介质热容，不是单一颜色标签", 600, 670, 19, Ink, "middle"));
    }
    private static void RenderDryIceIsolationVerification(XElement group)
    {
        group.Add(Text("隔离接触，定位白烟来源", 600, 132, 22, Green, "middle"));
        group.Add(ArticleRect(90, 205, 650, 400, "#E0F2FE", Blue, 3, "water-bath"));
        group.Add(Text("水浴", 125, 245, 21, Blue));
        var barrier = ArticleRect(270, 275, 260, 250, "#FFFFFF", Magenta, 4, "plastic-bag");
        barrier.SetAttributeValue("data-article-connection", "bag-isolates-contact");
        group.Add(barrier);
        group.Add(ArticleLine(270, 275, 270, 525, Magenta, 3, "isolation-barrier"));
        group.Add(ArticleRect(335, 355, 130, 100, "#CBD5E1", Ink, 3, "dry-ice"));
        group.Add(Text("干冰", 400, 415, 22, Ink, "middle"));
        group.Add(ArticleRect(392, 266, 16, 16, "#FFFFFF", Magenta, 2, "vent-hole"));
        group.Add(Text("小孔", 430, 270, 17, Magenta));
        var exit = ArticleLine(400, 275, 400, 210, Green, 4, "clear-bubble", true);
        exit.SetAttributeValue("data-article-connection", "gas-escapes-hole");
        group.Add(exit);
        foreach (var (x, y) in new[] { (400d, 190d), (420d, 155d), (380d, 135d), (440d, 105d) })
            group.Add(DrawArticleCircleElement(x, y, 10, Green, 3, "clear-bubble"));
        foreach (var x in new[] { 285d, 330d, 475d })
            group.Add(ArticleRect(x, 515, 36, 12, "#DBEAFE", Blue, 2, "ice-on-bag"));
        group.Add(Text("袋外结冰", 400, 555, 20, Blue, "middle"));
        group.Add(Text("薄塑料袋隔离接触", 400, 585, 18, Magenta, "middle"));
        group.Add(ArticleRect(800, 230, 300, 280, "#F8FAFC", "#CBD5E1", 2, "isolation-summary"));
        group.Add(Text("观察结果", 950, 275, 24, Ink, "middle"));
        group.Add(Text("袋外：结冰", 950, 340, 20, Blue, "middle"));
        group.Add(Text("清澈二氧化碳气泡", 950, 395, 20, Green, "middle"));
        group.Add(Text("没有白烟", 950, 450, 22, Magenta, "middle"));
        group.Add(Text("结论：冰晶需要干冰与水直接接触才能大量形成", 600, 670, 19, Amber, "middle"));
    }
}
