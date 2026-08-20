using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class ArticleScientificFigureCandidateRenderer
    : IArticleScientificFigureCandidateRenderer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private const string Ink = "#172033";
    private const string Muted = "#64748B";
    private const string Blue = "#2563EB";
    private const string Magenta = "#C026D3";
    private const string Green = "#087E8B";
    private const string Amber = "#B45309";
    private const string Panel = "#F8FAFC";

    public ScientificSvgArtifact Render(
        ArticleScientificFigureCandidate candidate,
        int presentationAttempt)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (presentationAttempt is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(presentationAttempt));
        }

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            throw new InvalidOperationException(
                "Source evidence boards must retain source pixels and cannot use the vector candidate renderer.");
        }

        var specificationId = StableGuid(candidate.CandidateId);
        var planId = $"article-candidate:{candidate.CandidateId}:presentation-{presentationAttempt}";
        var root = CreateRoot(candidate, planId, specificationId);
        var layer = new XElement(
            Svg + "g",
            new XAttribute("id", "layer-scientific-content"),
            new XAttribute("data-layer-id", "scientific-content"),
            new XAttribute("data-scientific", "true"));
        var group = new XElement(
            Svg + "g",
            new XAttribute("id", $"render-{candidate.CandidateId}"),
            new XAttribute("data-spec-id", candidate.CandidateId),
            new XAttribute("data-element-kind", FigureElementKind.Entity),
            new XAttribute("data-layer-id", "scientific-content"),
            new XAttribute("data-critical", "true"),
            new XAttribute("data-authoritative", "false"),
            new XAttribute("data-provenance-kind", ScientificProvenanceKind.ClaimEvidence));
        group.Add(Rect(24, 24, 1152, 752, "#FFFFFF", "#CBD5E1", 2));
        group.Add(Text(candidate.Title, 64, 74, 30, Ink));

        switch (candidate.Kind)
        {
            case ArticleScientificFigureCandidateKind.Mechanism:
                RenderSecondaryImaging(group);
                break;
            case ArticleScientificFigureCandidateKind.LensEquationGraph:
                RenderLensEquationGraph(group);
                break;
            case ArticleScientificFigureCandidateKind.ExperimentalComparison:
                RenderScreenRetinaComparison(group);
                break;
            case ArticleScientificFigureCandidateKind.Comparison:
                RenderObservationPositionComparison(group);
                break;
            case ArticleScientificFigureCandidateKind.CorrectiveLensControl:
                RenderCorrectiveLensControl(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalFrontMechanism:
                RenderThermalFront(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalBasinException:
                RenderThermalBasin(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalConductivityComparison:
                RenderThermalConductivity(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalTransferModes:
                RenderThermalTransferModes(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalHumidityClothing:
                RenderThermalHumidityClothing(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermalDryWetHeat:
                RenderThermalDryWetHeat(group);
                break;
            case ArticleScientificFigureCandidateKind.GravityTerminology:
                RenderGravityTerminology(group);
                break;
            case ArticleScientificFigureCandidateKind.GravityOrbitFreeFall:
                RenderGravityOrbitFreeFall(group);
                break;
            case ArticleScientificFigureCandidateKind.GravityElevatorFreeFall:
                RenderGravityElevatorFreeFall(group);
                break;
            case ArticleScientificFigureCandidateKind.GravitySurfaceRotation:
                RenderGravitySurfaceRotation(group);
                break;
            case ArticleScientificFigureCandidateKind.GravityCaseComparison:
                RenderGravityCaseComparison(group);
                break;
            case ArticleScientificFigureCandidateKind.GravityReferenceFrames:
                RenderGravityReferenceFrames(group);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(candidate),
                    candidate.Kind,
                    "Unsupported article candidate kind.");
        }

        layer.Add(group);
        root.Add(layer);
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var svg = document.ToString(SaveOptions.DisableFormatting);
        var sha256 = Hash(Encoding.UTF8.GetBytes(svg));
        return new ScientificSvgArtifact(planId, specificationId, 1, svg, sha256);
    }

    private static XElement CreateRoot(
        ArticleScientificFigureCandidate candidate,
        string planId,
        Guid specificationId) =>
        new(
            Svg + "svg",
            new XAttribute("xmlns", Svg.NamespaceName),
            new XAttribute("width", 1200),
            new XAttribute("height", 800),
            new XAttribute("viewBox", "0 0 1200 800"),
            new XAttribute("role", "img"),
            new XAttribute("aria-labelledby", "svg-title svg-description"),
            new XAttribute("data-plan-id", planId),
            new XAttribute("data-specification-id", specificationId.ToString("D")),
            new XAttribute("data-specification-version", 1),
            new XElement(Svg + "title", new XAttribute("id", "svg-title"), candidate.Title),
            new XElement(Svg + "desc", new XAttribute("id", "svg-description"), candidate.CentralMessage),
            new XElement(Svg + "metadata", $"candidate={candidate.CandidateId};gate1=pending;format=svg"),
            new XElement(
                Svg + "defs",
                new XElement(
                    Svg + "marker",
                    new XAttribute("id", "arrowhead"),
                    new XAttribute("markerWidth", 10),
                    new XAttribute("markerHeight", 7),
                    new XAttribute("refX", 9),
                    new XAttribute("refY", 3.5),
                    new XAttribute("orient", "auto-start-reverse"),
                    new XAttribute("markerUnits", "strokeWidth"),
                    new XElement(
                        Svg + "path",
                        new XAttribute("d", "M 0 0 L 10 3.5 L 0 7 z"),
                        new XAttribute("fill", Ink)))));

    private static void RenderSecondaryImaging(XElement group)
    {
        group.Add(Text("第一次成像", 350, 160, 18, Blue, "middle"));
        group.Add(Text("第二次成像", 865, 160, 18, Magenta, "middle"));
        group.Add(Line(72, 410, 1128, 410, Muted, 1));
        DrawLens(group, 330, 270, 550, "主凸透镜 L1", Blue);
        DrawLens(group, 850, 270, 550, "眼睛晶状体 L2", Magenta);
        group.Add(Line(1100, 285, 1100, 535, Green, 5));
        group.Add(Text("固定接收面/视网膜方向", 1118, 574, 15, Green, "end"));
        DrawArrow(group, 135, 410, 135, 255, Ink, 4);
        group.Add(Text("物体 O", 135, 235, 17, Ink, "middle"));
        DrawArrow(group, 625, 410, 625, 505, Blue, 4);
        group.Add(Text("中间像 S", 625, 536, 18, Blue, "middle"));
        group.Add(Line(135, 255, 330, 300, Blue, 3, arrow: true));
        group.Add(Line(330, 300, 625, 505, Blue, 3, arrow: true));
        group.Add(Line(135, 255, 330, 410, Blue, 3, arrow: true));
        group.Add(Line(330, 410, 625, 505, Blue, 3, arrow: true));
        group.Add(Line(625, 505, 850, 330, Magenta, 3, arrow: true));
        group.Add(Line(850, 330, 1100, 390, Magenta, 3, arrow: true));
        group.Add(Line(625, 505, 850, 410, Magenta, 3, arrow: true));
        group.Add(Line(850, 410, 1100, 390, Magenta, 3, arrow: true));
        group.Add(Rect(506, 570, 238, 70, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("S 是 L2 的物体", 625, 604, 18, Blue, "middle"));
    }

    private static void RenderLensEquationGraph(XElement group)
    {
        group.Add(Rect(68, 142, 1064, 520, Panel, "#CBD5E1", 1));
        group.Add(Text("归一化约定：x = u/f，y = v/f；u、v 均表示相应距离的正值", 90, 178, 18, Ink));
        const double left = 130;
        const double bottom = 620;
        const double plotSize = 390;
        const double maximum = 3.5;
        var scale = plotSize / maximum;
        group.Add(Line(left, bottom, left + plotSize, bottom, Ink, 2, arrow: true));
        group.Add(Line(left, bottom, left, bottom - plotSize, Ink, 2, arrow: true));
        group.Add(Text("x = u/f", left + plotSize + 44, bottom + 6, 16, Ink));
        group.Add(Text("y = v/f", left - 6, bottom - plotSize - 18, 16, Ink, "middle"));
        group.Add(Line(left, bottom, left + plotSize, bottom - plotSize, Muted, 1));
        group.Add(Text("y = x", left + plotSize - 12, bottom - plotSize + 26, 15, Muted, "end"));
        group.Add(Line(left + scale, bottom, left + scale, bottom - plotSize, Amber, 1));
        group.Add(Line(left, bottom - scale, left + plotSize, bottom - scale, Green, 1));
        group.Add(Text("x = 1", left + scale, bottom + 24, 14, Amber, "middle"));
        group.Add(Text("y = 1", left - 14, bottom - scale + 5, 14, Green, "end"));

        PlotCurve(group, x => x / (x + 1), 0.04, maximum, 52, left, bottom, scale, Blue);
        PlotCurve(group, x => x / (1 - x), 0.04, 0.77, 42, left, bottom, scale, Magenta);

        group.Add(Rect(600, 224, 472, 324, "#FFFFFF", "#CBD5E1", 1));
        group.Add(Text("虚物到实像（u 为虚物距大小）", 628, 264, 18, Blue));
        group.Add(Text("-1/u + 1/v = 1/f", 628, 298, 18, Ink));
        group.Add(Text("y = x / (x + 1)，x > 0", 628, 330, 19, Blue));
        group.Add(Text("0 < y < 1，且 y < x", 628, 360, 16, Muted));
        group.Add(Text("实物到虚像（0 < u < f，v 为像距大小）", 628, 408, 18, Magenta));
        group.Add(Text("1/u - 1/v = 1/f", 628, 442, 18, Ink));
        group.Add(Text("y = x / (1 - x)，0 < x < 1", 628, 474, 19, Magenta));
        group.Add(Text("两支互为反函数，关于 y = x 对称", 628, 520, 17, Amber));
    }

    private static void RenderScreenRetinaComparison(XElement group)
    {
        group.Add(Rect(60, 150, 520, 500, Panel, "#CBD5E1", 1));
        group.Add(Rect(620, 150, 520, 500, Panel, "#CBD5E1", 1));
        group.Add(Text("A. 光屏接收中间实像", 86, 190, 20, Blue));
        group.Add(Text("B. 眼睛/相机模型继续接收光束", 646, 190, 20, Magenta));
        group.Add(Line(92, 410, 548, 410, Muted, 1));
        DrawArrow(group, 120, 410, 120, 292, Ink, 4);
        DrawLens(group, 280, 270, 535, "L1", Blue);
        group.Add(Line(120, 292, 280, 330, Blue, 3, arrow: true));
        group.Add(Line(280, 330, 486, 490, Blue, 3, arrow: true));
        group.Add(Line(120, 292, 280, 410, Blue, 3, arrow: true));
        group.Add(Line(280, 410, 486, 490, Blue, 3, arrow: true));
        group.Add(Line(486, 262, 486, 535, Green, 5));
        group.Add(Text("光屏位于像面", 486, 566, 17, Green, "middle"));

        group.Add(Line(652, 410, 1108, 410, Muted, 1));
        group.Add(Rect(676, 300, 8, 220, Blue, Blue, 1));
        group.Add(Text("S 平面", 680, 548, 16, Blue, "middle"));
        group.Add(Line(680, 330, 860, 350, Magenta, 3, arrow: true));
        group.Add(Line(680, 490, 860, 410, Magenta, 3, arrow: true));
        DrawLens(group, 860, 270, 535, "L2", Magenta);
        group.Add(Line(860, 350, 1065, 398, Magenta, 3, arrow: true));
        group.Add(Line(860, 410, 1065, 398, Magenta, 3, arrow: true));
        group.Add(Line(1065, 275, 1065, 525, Green, 5));
        group.Add(Text("固定接收面", 1065, 558, 17, Green, "middle"));
    }

    private static void RenderObservationPositionComparison(XElement group)
    {
        var cards = new[]
        {
            (Left: 58d, Title: "A. L2 位于 S 右侧", Accent: Blue, LensOffset: 258d,
                State: "到达 L2 前：光束已越过 S 并发散"),
            (Left: 414d, Title: "B. L2 与 S 平面重合", Accent: Green, LensOffset: 166d,
                State: "边界状态：会聚点落在 L2 平面"),
            (Left: 770d, Title: "C. L2 位于 S 左侧", Accent: Magenta, LensOffset: 92d,
                State: "到达 L2 前：光束仍在会聚"),
        };
        foreach (var card in cards)
        {
            group.Add(Rect(card.Left, 160, 326, 470, Panel, "#CBD5E1", 1));
            group.Add(Text(card.Title, card.Left + 22, 200, 18, card.Accent));
            group.Add(Line(card.Left + 30, 410, card.Left + 296, 410, Muted, 1));
            var startX = card.Left + 34;
            var imageX = card.Left + 166;
            var lensX = card.Left + card.LensOffset;
            group.Add(Rect(imageX - 3, 286, 6, 248, Blue, Blue, 1));
            if (lensX != imageX)
            {
                group.Add(Text("S 平面", imageX, 560, 16, Blue, "middle"));
            }

            DrawIncidentRay(group, startX, 326, imageX, 410, lensX, card.Accent);
            DrawIncidentRay(group, startX, 494, imageX, 410, lensX, card.Accent);
            DrawLens(
                group,
                lensX,
                300,
                520,
                lensX == imageX ? "S/L2 共面" : "L2",
                card.Accent);
            group.Add(Text(card.State, card.Left + 163, 598, 14, Muted, "middle"));
        }

    }

    private static void RenderCorrectiveLensControl(XElement group)
    {
        group.Add(Rect(62, 158, 1076, 220, Panel, "#CBD5E1", 1));
        group.Add(Rect(62, 412, 1076, 220, Panel, "#CBD5E1", 1));
        group.Add(Text("对照：无附加透镜", 88, 196, 20, Blue));
        group.Add(Text("干预：加入附加透镜", 88, 450, 20, Magenta));
        DrawLens(group, 650, 205, 340, "眼睛/相机镜头", Blue);
        group.Add(Line(1080, 205, 1080, 342, Green, 5));
        group.Add(Text("固定传感器面", 1096, 360, 15, Green, "end"));
        group.Add(Line(126, 240, 650, 258, Blue, 3, arrow: true));
        group.Add(Line(126, 318, 650, 288, Blue, 3, arrow: true));
        group.Add(Line(650, 258, 990, 276, Blue, 3, arrow: true));
        group.Add(Line(650, 288, 990, 276, Blue, 3, arrow: true));
        group.Add(Text("焦点 A（示意）", 990, 310, 14, Blue, "middle"));

        DrawConcaveLens(group, 370, 460, 592, "附加凹透镜（近视镜片）", Magenta);
        DrawLens(group, 650, 460, 592, "眼睛/相机镜头", Blue);
        group.Add(Line(1080, 460, 1080, 595, Green, 5));
        group.Add(Text("同一固定传感器面", 1096, 614, 15, Green, "end"));
        group.Add(Line(126, 494, 370, 512, Magenta, 3, arrow: true));
        group.Add(Line(126, 572, 370, 554, Magenta, 3, arrow: true));
        group.Add(Line(370, 512, 650, 492, Magenta, 3, arrow: true));
        group.Add(Line(370, 554, 650, 574, Magenta, 3, arrow: true));
        group.Add(Line(650, 492, 1050, 533, Blue, 3, arrow: true));
        group.Add(Line(650, 574, 1050, 533, Blue, 3, arrow: true));
        group.Add(Text("焦点 B（示意）", 1038, 568, 14, Blue, "end"));
        group.Add(Text(
            "凹透镜使到达 L2 的光束更发散；最终像面位置与清晰度仍取决于完整系统参数",
            600,
            672,
            17,
            Amber,
            "middle"));
    }

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

    private static void RenderGravityTerminology(XElement group)
    {
        var cards = new[]
        {
            (70d, "地球引力 Fg", "真实相互作用力", "Fg = GMm/r²", Blue),
            (390d, "有效重力 m·g_eff", "指定参考系中的合成量", "地球固连系含自转离心项", Green),
            (710d, "支持力/拉力 N 或 T", "秤或测力计的直接读数来源", "自由落体时可约为 0", Magenta),
        };
        foreach (var card in cards)
        {
            group.Add(Rect(card.Item1, 175, 285, 360, Panel, "#CBD5E1", 1));
            group.Add(Text(card.Item2, card.Item1 + 142, 230, 21, card.Item5, "middle"));
            group.Add(Text(card.Item3, card.Item1 + 142, 300, 16, Ink, "middle"));
            group.Add(Text(card.Item4, card.Item1 + 142, 365, 16, Muted, "middle"));
        }

        group.Add(Rect(70, 575, 960, 105, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("NIST/ISO 的 weight 不是默认等同于秤读数", 550, 618, 19, Amber, "middle"));
        group.Add(Text("先声明参考系与术语约定，再写公式和结论", 550, 655, 18, Ink, "middle"));
    }

    private static void RenderGravityOrbitFreeFall(XElement group)
    {
        DrawCircle(group, 350, 430, 190, Blue, 3);
        group.Add(Text("地球", 350, 440, 28, Blue, "middle"));
        group.Add(Rect(830, 360, 170, 110, Panel, "#64748B", 2));
        group.Add(Text("空间站", 915, 425, 22, Ink, "middle"));
        group.Add(GravityLine(830, 415, 540, 425, Blue, 5, "orbit-gravity", arrow: true));
        group.Add(Text("Fg 与 a 均指向地心", 690, 390, 18, Blue, "middle"));
        group.Add(GravityLine(1035, 465, 1035, 285, Magenta, 4, "orbit-velocity", arrow: true));
        group.Add(Text("切向速度 v", 1020, 270, 17, Magenta, "end"));
        group.Add(Rect(665, 555, 430, 115, "#ECFDF5", "#6EE7B7", 1));
        group.Add(Text("g(r) = GM/r² ≠ 0", 880, 598, 21, Green, "middle"));
        group.Add(Text("共同自由落体：秤读数 N ≈ 0", 880, 638, 19, Ink, "middle"));
    }

    private static void RenderGravityElevatorFreeFall(XElement group)
    {
        group.Add(Rect(300, 155, 600, 485, Panel, "#64748B", 3));
        group.Add(Line(330, 560, 870, 560, Muted, 4));
        DrawCircle(group, 600, 380, 55, Ink, 3);
        group.Add(Text("物体 m", 600, 388, 18, Ink, "middle"));
        group.Add(GravityLine(600, 445, 600, 545, Blue, 5, "elevator-gravity", arrow: true));
        group.Add(Text("Fg = mg", 625, 505, 18, Blue));
        group.Add(GravityLine(780, 220, 780, 390, Magenta, 5, "elevator-acceleration", arrow: true));
        group.Add(Text("电梯与物体 a ≈ g", 800, 305, 18, Magenta));
        group.Add(Text("支持力 N ≈ 0", 600, 610, 20, Green, "middle"));
        group.Add(Text("地球引力与自由落体加速度并未消失", 600, 705, 19, Amber, "middle"));
    }

    private static void RenderGravitySurfaceRotation(XElement group)
    {
        const double cx = 430;
        const double cy = 430;
        DrawCircle(group, cx, cy, 225, Blue, 3);
        group.Add(Line(cx, 155, cx, 705, Muted, 2));
        group.Add(Text("自转轴", cx - 12, 180, 16, Muted, "end"));
        DrawCircle(group, 590, 270, 10, Magenta, 4);
        group.Add(Text("物体", 615, 265, 18, Ink));
        group.Add(GravityLine(590, 280, 470, 400, Blue, 5, "surface-gravity", arrow: true));
        group.Add(Text("引力场 g_grav", 565, 355, 17, Blue));
        group.Add(GravityLine(600, 270, 760, 270, Magenta, 4, "surface-centrifugal", arrow: true));
        group.Add(Text("离心项 ω²r⊥", 780, 275, 17, Magenta));
        group.Add(GravityLine(590, 282, 495, 388, Green, 6, "surface-effective-gravity", arrow: true));
        group.Add(Text("有效重力 g_eff", 680, 420, 18, Green));
        group.Add(GravityLine(585, 258, 680, 152, Amber, 4, "surface-support", arrow: true));
        group.Add(Text("地面合支持力 R", 700, 145, 17, Amber));
        group.Add(Rect(750, 500, 370, 150, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("g_eff = g_grav + a_cf", 935, 550, 20, Amber, "middle"));
        group.Add(Text("向心加速度是运动学结果", 935, 592, 17, Ink, "middle"));
        group.Add(Text("不是额外的相互作用力", 935, 625, 17, Ink, "middle"));
    }

    private static void RenderGravityCaseComparison(XElement group)
    {
        var columns = new[]
        {
            (70d, "绕地轨道", "Fg ≠ 0", "a_orbit ≠ 0", "N ≈ 0", Blue),
            (390d, "自由落体电梯", "Fg ≠ 0", "a ≈ g ≠ 0", "N ≈ 0", Magenta),
            (710d, "地表相对静止", "Fg ≠ 0", "g_eff ≠ 0", "N ≈ m·g_eff", Green),
        };
        foreach (var column in columns)
        {
            group.Add(Rect(column.Item1, 170, 285, 430, Panel, "#CBD5E1", 1));
            group.Add(Text(column.Item2, column.Item1 + 142, 225, 21, column.Item6, "middle"));
            group.Add(Text("地球引力", column.Item1 + 32, 320, 16, Muted));
            group.Add(Text(column.Item3, column.Item1 + 245, 320, 19, Ink, "end"));
            group.Add(Text("物体加速度", column.Item1 + 32, 405, 16, Muted));
            group.Add(Text(column.Item4, column.Item1 + 245, 405, 19, Ink, "end"));
            group.Add(Text("秤读数/支持力", column.Item1 + 142, 480, 16, Muted, "middle"));
            group.Add(Text(column.Item5, column.Item1 + 142, 525, 19, column.Item6, "middle"));
        }
        group.Add(Text("失重判据：支持力/秤读数接近零，而不是地球引力消失", 550, 685, 19, Amber, "middle"));
    }

    private static void RenderGravityReferenceFrames(XElement group)
    {
        group.Add(Rect(70, 165, 460, 450, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(610, 165, 460, 450, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("惯性系", 300, 220, 24, Blue, "middle"));
        group.Add(Text("只画真实相互作用力", 300, 280, 19, Ink, "middle"));
        group.Add(Text("ΣF_real = m·a", 300, 350, 22, Blue, "middle"));
        group.Add(Text("向心力 = 合力的径向角色", 300, 430, 17, Muted, "middle"));
        group.Add(Text("不要再额外添加一支“向心力”箭头", 300, 505, 16, Amber, "middle"));

        group.Add(Text("随动/旋转非惯性系", 840, 220, 24, Magenta, "middle"));
        group.Add(Text("声明参考系后加入惯性力", 840, 280, 19, Ink, "middle"));
        group.Add(Text("ΣF_real + F_inertial = m·a_rel", 840, 350, 21, Magenta, "middle"));
        group.Add(Text("自由落体随动系可见近似平衡", 840, 430, 17, Muted, "middle"));
        group.Add(Text("不得与惯性系方程混用", 840, 505, 17, Amber, "middle"));
        group.Add(Text("先选参考系 → 列真实力 → 必要时加惯性力 → 再解释秤读数", 570, 690, 18, Green, "middle"));
    }

    private static void DrawLens(
        XElement group,
        double x,
        double top,
        double bottom,
        string label,
        string color)
    {
        group.Add(Line(x, top, x, bottom, color, 5));
        group.Add(Line(x, top, x - 12, top + 18, color, 3));
        group.Add(Line(x, top, x + 12, top + 18, color, 3));
        group.Add(Line(x, bottom, x - 12, bottom - 18, color, 3));
        group.Add(Line(x, bottom, x + 12, bottom - 18, color, 3));
        group.Add(Text(label, x, bottom + 30, 16, color, "middle"));
    }

    private static void DrawConcaveLens(
        XElement group,
        double x,
        double top,
        double bottom,
        string label,
        string color)
    {
        group.Add(Line(x, top, x, bottom, color, 5));
        group.Add(Line(x, top + 18, x - 12, top, color, 3));
        group.Add(Line(x, top + 18, x + 12, top, color, 3));
        group.Add(Line(x, bottom - 18, x - 12, bottom, color, 3));
        group.Add(Line(x, bottom - 18, x + 12, bottom, color, 3));
        group.Add(Text(label, x, bottom + 30, 15, color, "middle"));
    }

    private static void DrawIncidentRay(
        XElement group,
        double startX,
        double startY,
        double imageX,
        double imageY,
        double lensX,
        string color)
    {
        if (lensX <= imageX)
        {
            var fraction = (lensX - startX) / (imageX - startX);
            var lensY = startY + ((imageY - startY) * fraction);
            group.Add(Line(startX, startY, lensX, lensY, color, 3, arrow: true));
            return;
        }

        var slope = (imageY - startY) / (imageX - startX);
        var extendedY = imageY + (slope * (lensX - imageX));
        group.Add(Line(startX, startY, imageX, imageY, color, 3, arrow: true));
        group.Add(Line(imageX, imageY, lensX, extendedY, color, 3, arrow: true));
    }

    private static void PlotCurve(
        XElement group,
        Func<double, double> function,
        double start,
        double end,
        int steps,
        double left,
        double bottom,
        double scale,
        string color)
    {
        var previousX = start;
        var previousY = function(start);
        for (var step = 1; step <= steps; step++)
        {
            var x = start + ((end - start) * step / steps);
            var y = function(x);
            group.Add(Line(
                left + (previousX * scale),
                bottom - (previousY * scale),
                left + (x * scale),
                bottom - (y * scale),
                color,
                3));
            previousX = x;
            previousY = y;
        }
    }

    private static void DrawArrow(
        XElement group,
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width) =>
        group.Add(Line(x1, y1, x2, y2, color, width, arrow: true));

    private static void DrawCircle(
        XElement group,
        double centerX,
        double centerY,
        double radius,
        string color,
        double width)
    {
        const int segments = 48;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            group.Add(Line(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width));
        }
    }

    private static XElement Rect(
        double x,
        double y,
        double width,
        double height,
        string fill,
        string stroke,
        double strokeWidth) =>
        new(
            Svg + "rect",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("width", Number(width)),
            new XAttribute("height", Number(height)),
            new XAttribute("rx", 4),
            new XAttribute("fill", fill),
            new XAttribute("stroke", stroke),
            new XAttribute("stroke-width", Number(strokeWidth)));

    private static XElement Line(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        bool arrow = false) =>
        new(
            Svg + "path",
            new XAttribute("d", $"M {Number(x1)} {Number(y1)} L {Number(x2)} {Number(y2)}"),
            new XAttribute("fill", "none"),
            new XAttribute("stroke", color),
            new XAttribute("stroke-width", Number(width)),
            new XAttribute("data-element-graphic", "true"),
            arrow ? new XAttribute("marker-end", "url(#arrowhead)") : null);

    private static XElement ThermalLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role,
        bool arrow = false)
    {
        var line = Line(x1, y1, x2, y2, color, width, arrow);
        line.SetAttributeValue("data-thermal-role", role);
        return line;
    }

    private static XElement GravityLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role,
        bool arrow = false)
    {
        var line = Line(x1, y1, x2, y2, color, width, arrow);
        line.SetAttributeValue("data-gravity-role", role);
        return line;
    }

    private static XElement Text(
        string value,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor = "start") =>
        new(
            Svg + "text",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("text-anchor", anchor),
            new XAttribute("font-family", "Microsoft YaHei"),
            new XAttribute("font-size", fontSize),
            new XAttribute("fill", color),
            new XAttribute("data-content-kind", FigureElementKind.Entity),
            value);

    private static Guid StableGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static string Number(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
