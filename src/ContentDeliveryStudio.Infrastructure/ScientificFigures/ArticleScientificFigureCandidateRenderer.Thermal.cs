using System.Globalization;
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
    private static void RenderThermalFront(XElement group)
    {
        group.Add(Text("寒冷空气", 170, 190, 20, Blue));
        group.Add(Text("暖湿空气", 830, 190, 20, Magenta));
        group.Add(Text("凝华成雪", 610, 230, 18, Ink, "middle"));
        group.Add(Text("高空放热", 610, 270, 16, Amber, "middle"));
        group.Add(Text("地面", 600, 650, 18, Green, "middle"));
        group.Add(Line(90, 590, 1110, 590, Green, 4));
        group.Add(Line(150, 260, 360, 360, Blue, 4, arrow: true));
        group.Add(Line(360, 360, 560, 280, Blue, 4, arrow: true));
        group.Add(Line(1060, 300, 850, 330, Magenta, 4, arrow: true));
        group.Add(Line(850, 330, 650, 275, Magenta, 4, arrow: true));
        group.Add(Line(430, 330, 650, 275, Ink, 3, arrow: true));
        group.Add(Line(650, 275, 760, 330, Ink, 3, arrow: true));
        group.Add(Rect(540, 300, 140, 90, "#E0F2FE", "#93C5FD", 1));
        group.Add(Text("锋面抬升区", 610, 350, 17, Ink, "middle"));
        group.Add(Text("放热发生在高空，不等于地面升温", 600, 730, 17, Amber, "middle"));
    }
    private static void RenderThermalBasin(XElement group)
    {
        group.Add(Text("南下寒冷空气", 115, 190, 20, Blue));
        group.Add(Text("高山", 455, 395, 20, Ink, "middle"));
        group.Add(Text("盆地", 760, 555, 20, Green, "middle"));
        group.Add(Text("雪", 770, 330, 20, Magenta, "middle"));
        group.Add(Text("地面仍较暖", 760, 625, 18, Amber, "middle"));
        group.Add(Line(90, 610, 1110, 610, Green, 3));
        group.Add(ThermalLine(150, 260, 390, 340, Blue, 4, "basin-cold-air-aloft", arrow: true));
        group.Add(ThermalLine(390, 340, 525, 300, Blue, 4, "basin-cold-air-aloft", arrow: true));
        group.Add(ThermalLine(525, 300, 650, 350, Blue, 4, "basin-cold-air-aloft", arrow: true));
        group.Add(ThermalLine(650, 350, 790, 405, Blue, 4, "basin-cold-air-aloft", arrow: true));
        group.Add(Line(300, 600, 430, 380, Ink, 4));
        group.Add(Line(430, 380, 500, 300, Ink, 4));
        group.Add(Line(500, 300, 570, 390, Ink, 4));
        group.Add(Line(570, 390, 650, 600, Ink, 4));
        group.Add(Line(650, 600, 830, 600, Ink, 4));
        group.Add(Line(830, 600, 990, 430, Ink, 4));
        group.Add(Text("冷空气越过高山后仍在高空，未快速下沉", 610, 700, 17, Amber, "middle"));
    }
    private static void RenderThermalConductivity(XElement group)
    {
        group.Add(Text("导热系数 λ", 88, 158, 22, Ink));
        group.Add(Text("W/(m·K)", 88, 190, 18, Muted));
        var rows = new[]
        {
            ("空气", 0.02, Blue),
            ("水蒸气", 0.02, Magenta),
            ("棉毛", 0.05, Green),
            ("水", 0.6, Amber),
        };
        var y = 270d;
        foreach (var row in rows)
        {
            group.Add(Text(row.Item1, 110, y + 12, 19, Ink));
            group.Add(Rect(260, y - 12, 720 * row.Item2 / 0.6, 28, row.Item3, row.Item3, 1));
            group.Add(Text(row.Item2.ToString("0.##", CultureInfo.InvariantCulture), 1010, y + 12, 18, Ink));
            y += 82;
        }
        group.Add(Rect(105, 610, 990, 86, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("水蒸气≈空气；液态水导热更强；棉毛受潮后保温性下降", 600, 660, 18, Blue, "middle"));
    }
    private static void RenderThermalTransferModes(XElement group)
    {
        var cards = new[]
        {
            (70d, "热传导", "冬季：人体→衣物", string.Empty, Blue),
            (345d, "热对流", "冬季：人体→空气", string.Empty, Magenta),
            (620d, "热辐射", "人体红外散热", "占比随环境变化", Green),
            (895d, "相变潜热", "夏季：汗液蒸发", string.Empty, Amber),
        };
        foreach (var card in cards)
        {
            group.Add(Rect(card.Item1, 220, 235, 300, "#F8FAFC", "#CBD5E1", 1));
            group.Add(Text(card.Item2, card.Item1 + 118, 275, 20, card.Item5, "middle"));
            group.Add(Text(card.Item3, card.Item1 + 118, 345, 17, Ink, "middle"));
            group.Add(Line(card.Item1 + 55, 420, card.Item1 + 180, 420, card.Item5, 5, arrow: true));
            if (!string.IsNullOrEmpty(card.Item4))
            {
                group.Add(Text(card.Item4, card.Item1 + 118, 475, 15, Muted, "middle"));
            }
        }
        group.Add(Text("冬季", 260, 625, 19, Blue, "middle"));
        group.Add(Text("夏季", 940, 625, 19, Amber, "middle"));
    }
    private static void RenderThermalHumidityClothing(XElement group)
    {
        var nodes = new[]
        {
            ("相对湿度较高", 120d, Blue),
            ("衣物潮湿", 370d, Magenta),
            ("导热系数增大", 650d, Amber),
            ("热量快速散去", 920d, Green),
        };
        foreach (var node in nodes)
        {
            group.Add(Rect(node.Item2, 330, 190, 100, "#F8FAFC", "#CBD5E1", 1));
            group.Add(Text(node.Item1, node.Item2 + 95, 390, 18, node.Item3, "middle"));
        }
        for (var i = 0; i < nodes.Length - 1; i++)
        {
            group.Add(ThermalLine(
                nodes[i].Item2 + 190,
                380,
                nodes[i + 1].Item2,
                380,
                Ink,
                3,
                "humidity-causal-link",
                arrow: true));
        }
        group.Add(Text("高相对湿度使衣物保温性下降，人体热量散失加快", 600, 610, 18, Amber, "middle"));
    }
    private static void RenderThermalDryWetHeat(XElement group)
    {
        group.Add(Rect(90, 180, 480, 420, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(630, 180, 480, 420, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("干热", 330, 235, 24, Blue, "middle"));
        group.Add(Text("湿热", 870, 235, 24, Amber, "middle"));
        group.Add(Text("汗液蒸发", 330, 350, 22, Ink, "middle"));
        group.Add(Text("汗液蒸发", 870, 350, 22, Ink, "middle"));
        group.Add(ThermalLine(180, 430, 480, 430, Blue, 5, "dry-evaporation-rate", arrow: true));
        group.Add(ThermalLine(820, 430, 920, 430, Amber, 5, "humid-evaporation-rate", arrow: true));
        group.Add(Text("蒸发快，较舒适", 330, 500, 18, Blue, "middle"));
        group.Add(Text("蒸发受阻，闷热", 870, 500, 18, Amber, "middle"));
        group.Add(Text("相对湿度改变汗液蒸发速率", 600, 690, 18, Ink, "middle"));
    }
}
