using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using SkiaSharp;
using FormulaPiece = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.FormulaPiece;
using MathRun = ContentDeliveryStudio.Infrastructure.ScientificFigures.ScientificMathLayout.MathRun;

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
    private static readonly ScientificMathLayout MathLayout = new();

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
            case ArticleScientificFigureCandidateKind.ThermistorCircuitDivider:
                RenderThermistorCircuitDivider(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermistorCurvature:
                RenderThermistorCurvature(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermistorError:
                RenderThermistorError(group);
                break;
            case ArticleScientificFigureCandidateKind.ThermistorSpecialValues:
                RenderThermistorSpecialValues(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesDefinition:
                RenderArchimedesDefinition(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesWaterModel:
                RenderArchimedesWaterModel(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesBottomContact:
                RenderArchimedesBottomContact(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesDepthDependence:
                RenderArchimedesDepthDependence(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesTopContact:
                RenderArchimedesTopContact(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesPier:
                RenderArchimedesPier(group);
                break;
            case ArticleScientificFigureCandidateKind.ArchimedesPressureCaveat:
                RenderArchimedesPressureCaveat(group);
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
        group.Add(Text("归一化约定：", 90, 178, 18, Ink));
        group.Add(FractionFormula(
            @"x=\frac{u}{f},\ y=\frac{v}{f}",
            220, 173, 18, Ink, "start",
            FormulaPiece.Plain("x = "), FormulaPiece.Fraction("u", "f"), FormulaPiece.Plain("，y = "), FormulaPiece.Fraction("v", "f")));
        group.Add(Text("；u、v 均表示相应距离的正值", 420, 178, 18, Ink));
        const double left = 130;
        const double bottom = 620;
        const double plotSize = 390;
        const double maximum = 3.5;
        var scale = plotSize / maximum;
        group.Add(Line(left, bottom, left + plotSize, bottom, Ink, 2, arrow: true));
        group.Add(Line(left, bottom, left, bottom - plotSize, Ink, 2, arrow: true));
        group.Add(FractionFormula(@"x=\frac{u}{f}", left + plotSize + 44, bottom + 2, 16, Ink, "start",
            FormulaPiece.Plain("x = "), FormulaPiece.Fraction("u", "f")));
        group.Add(FractionFormula(@"y=\frac{v}{f}", left - 6, bottom - plotSize - 18, 16, Ink, "middle",
            FormulaPiece.Plain("y = "), FormulaPiece.Fraction("v", "f")));
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
        group.Add(FractionFormula(@"-\frac{1}{u}+\frac{1}{v}=\frac{1}{f}", 628, 295, 18, Ink, "start",
            FormulaPiece.Plain("−"), FormulaPiece.Fraction("1", "u"), FormulaPiece.Plain(" + "),
            FormulaPiece.Fraction("1", "v"), FormulaPiece.Plain(" = "), FormulaPiece.Fraction("1", "f")));
        group.Add(FractionFormula(@"y=\frac{x}{x+1},\ x>0", 628, 328, 19, Blue, "start",
            FormulaPiece.Plain("y = "), FormulaPiece.Fraction("x", "x + 1"), FormulaPiece.Plain("，x > 0")));
        group.Add(Text("0 < y < 1，且 y < x", 628, 360, 16, Muted));
        group.Add(Text("实物到虚像（0 < u < f，v 为像距大小）", 628, 408, 18, Magenta));
        group.Add(FractionFormula(@"\frac{1}{u}-\frac{1}{v}=\frac{1}{f}", 628, 439, 18, Ink, "start",
            FormulaPiece.Fraction("1", "u"), FormulaPiece.Plain(" − "), FormulaPiece.Fraction("1", "v"),
            FormulaPiece.Plain(" = "), FormulaPiece.Fraction("1", "f")));
        group.Add(FractionFormula(@"y=\frac{x}{1-x},\ 0<x<1", 628, 472, 19, Magenta, "start",
            FormulaPiece.Plain("y = "), FormulaPiece.Fraction("x", "1 − x"), FormulaPiece.Plain("，0 < x < 1")));
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
            (70d, "地球引力", "真实相互作用力", "", Blue),
            (390d, "有效重力", "指定参考系中的合成量", "地球固连系含自转离心项", Green),
            (710d, "支持力/拉力", "秤或测力计的直接读数来源", "自由落体时可约为 0", Magenta),
        };
        foreach (var card in cards)
        {
            group.Add(Rect(card.Item1, 175, 285, 360, Panel, "#CBD5E1", 1));
            group.Add(Text(card.Item2, card.Item1 + 142, 220, 21, card.Item5, "middle"));
            group.Add(Text(card.Item3, card.Item1 + 142, 315, 16, Ink, "middle"));
            if (!string.IsNullOrWhiteSpace(card.Item4))
            {
                group.Add(Text(card.Item4, card.Item1 + 142, 390, 16, Muted, "middle"));
            }
        }

        group.Add(MathText(@"\mathbf{F}_g", 212, 260, 22, Blue, "middle", MathRun.Vector("F"), MathRun.Subscript("g")));
        group.Add(MathText(@"F_g=", 152, 385, 18, Muted, "start", MathRun.ItalicRun("F"), MathRun.Subscript("g"), MathRun.Normal(" = ")));
        group.Add(FractionFormula(@"\frac{GMm}{r^2}", 220, 380, 18, Muted, "start", FormulaPiece.Fraction("GMm", "r²")));
        group.Add(MathText(@"\mathbf{g}_{\mathrm{eff}}", 532, 260, 22, Green, "middle", MathRun.Vector("g"), MathRun.Subscript("eff")));
        group.Add(MathText(@"\mathbf{N}\;\text{或}\;\mathbf{T}", 852, 260, 22, Magenta, "middle", MathRun.Vector("N"), MathRun.Normal(" 或 "), MathRun.Vector("T")));
    }

    private static void RenderGravityOrbitFreeFall(XElement group)
    {
        DrawCircle(group, 350, 430, 190, Blue, 3);
        group.Add(Text("地球", 350, 440, 28, Blue, "middle"));
        group.Add(Rect(830, 360, 170, 110, Panel, "#64748B", 2));
        group.Add(Text("空间站", 915, 425, 22, Ink, "middle"));
        group.Add(GravityLine(830, 415, 540, 425, Blue, 5, "orbit-gravity", arrow: true));
        group.Add(MathText(@"\mathbf{F}_g", 610, 390, 18, Blue, "start", MathRun.Vector("F"), MathRun.Subscript("g")));
        group.Add(Text("与 a 均指向地心", 647, 390, 18, Blue));
        group.Add(GravityLine(1035, 465, 1035, 285, Magenta, 4, "orbit-velocity", arrow: true));
        group.Add(Text("切向速度 v", 1020, 270, 17, Magenta, "end"));
        var callout = new XElement(
            Svg + "g",
            new XAttribute("data-layout-panel-id", "orbit-callout"));
        callout.Add(Rect(665, 555, 430, 115, "#ECFDF5", "#6EE7B7", 1));
        callout.Add(FractionFormula(@"g(r)=\frac{GM}{r^2}\ne0", 880, 592, 21, Green, "middle",
            FormulaPiece.Plain("g(r) = "), FormulaPiece.Fraction("GM", "r²"), FormulaPiece.Plain(" ≠ 0")));
        // Keep the explanatory label and formula inside the green callout. The
        // previous right-aligned label started at x≈660, outside the panel's
        // x=665 boundary at the generated 1200px canvas width.
        callout.Add(Text("共同自由落体：秤读数", 700, 642, 19, Ink, "start"));
        callout.Add(MathText(@"\mathbf{N}\approx0", 900, 642, 19, Ink, "start", MathRun.Vector("N"), MathRun.Normal(" ≈ 0")));
        group.Add(callout);
    }

    private static void RenderGravityElevatorFreeFall(XElement group)
    {
        group.Add(Rect(300, 155, 600, 485, Panel, "#64748B", 3));
        group.Add(Line(330, 560, 870, 560, Muted, 4));
        DrawCircle(group, 600, 380, 55, Ink, 3);
        group.Add(Text("物体 m", 600, 388, 18, Ink, "middle"));
        group.Add(GravityLine(600, 445, 600, 545, Blue, 5, "elevator-gravity", arrow: true));
        group.Add(MathText(@"\mathbf{F}_g=m\mathbf{g}", 625, 505, 18, Blue, "start",
            MathRun.Vector("F"), MathRun.Subscript("g"), MathRun.Normal(" = m"), MathRun.Vector("g")));
        group.Add(GravityLine(780, 220, 780, 390, Magenta, 5, "elevator-acceleration", arrow: true));
        group.Add(Text("电梯与物体", 800, 290, 18, Magenta));
        group.Add(MathText(@"\mathbf{a}\approx\mathbf{g}", 800, 320, 18, Magenta, "start",
            MathRun.Vector("a"), MathRun.Normal(" ≈ "), MathRun.Vector("g")));
        group.Add(Text("支持力", 575, 610, 20, Green, "end"));
        group.Add(MathText(@"\mathbf{N}\approx0", 590, 610, 20, Green, "start", MathRun.Vector("N"), MathRun.Normal(" ≈ 0")));
        group.Add(Text("地球引力与自由落体加速度并未消失", 600, 705, 19, Amber, "middle"));
    }

    private static void RenderGravitySurfaceRotation(XElement group)
    {
        const double cx = 340;
        const double cy = 430;
        DrawCircle(group, cx, cy, 210, Blue, 3);
        group.Add(Line(cx, 155, cx, 705, Muted, 2));
        group.Add(Text("自转轴", cx - 12, 180, 16, Muted, "end"));
        const double px = 485;
        const double py = 278;
        const double gravityX = 370;
        const double gravityY = 420;
        const double centrifugalX = 535;
        const double centrifugalY = 278;
        const double effectiveX = 420;
        const double effectiveY = 420;
        DrawCircle(group, px, py, 10, Magenta, 4);
        group.Add(Text("物体", px + 20, py - 14, 17, Ink));
        group.Add(GravityLine(px, py, gravityX, gravityY, Blue, 5, "surface-gravity", arrow: true));
        group.Add(Text("引力场", 255, 365, 17, Blue));
        group.Add(MathText(@"\mathbf{g}_{\mathrm{grav}}", 325, 365, 17, Blue, "start",
            MathRun.Vector("g"), MathRun.Subscript("grav")));
        group.Add(GravityLine(px, py, centrifugalX, centrifugalY, Magenta, 4, "surface-centrifugal", arrow: true));
        group.Add(Text("离心项", 550, 258, 17, Magenta));
        group.Add(MathText(@"\mathbf{a}_{\mathrm{cf}}=\omega^2r_\perp", 550, 288, 17, Magenta, "start",
            MathRun.Vector("a"), MathRun.Subscript("cf"), MathRun.Normal(" = ω"), MathRun.Superscript("2"),
            MathRun.Normal("r"), MathRun.Subscript("⊥")));
        group.Add(DashedGravityLine(gravityX, gravityY, effectiveX, effectiveY, Magenta, 2, "surface-centrifugal-translation"));
        group.Add(DashedGravityLine(centrifugalX, centrifugalY, effectiveX, effectiveY, Blue, 2, "surface-gravity-translation"));
        group.Add(GravityLine(px, py, effectiveX, effectiveY, Green, 6, "surface-effective-gravity", arrow: true));
        group.Add(Text("有效重力", 470, 445, 18, Green));
        group.Add(MathText(@"\mathbf{g}_{\mathrm{eff}}", 565, 445, 18, Green, "start",
            MathRun.Vector("g"), MathRun.Subscript("eff")));
        group.Add(Text("矢量平行四边形（地球固连系）", 350, 690, 17, Muted, "middle"));
        group.Add(Text("向心加速度是运动学结果，不是额外的相互作用力", 350, 720, 16, Ink, "middle"));

        group.Add(Rect(680, 145, 440, 495, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("地表静止物体的受力平衡", 900, 195, 21, Amber, "middle"));
        DrawCircle(group, 900, 350, 18, Ink, 3);
        group.Add(GravityLine(900, 350, 970, 240, Amber, 5, "surface-support", arrow: true));
        group.Add(Text("地面合支持力 R", 985, 218, 17, Amber));
        group.Add(GravityLine(900, 350, 830, 460, Green, 5, "surface-effective-force", arrow: true));
        group.Add(MathText(@"m\mathbf{g}_{\mathrm{eff}}", 810, 510, 17, Green, "middle",
            MathRun.Normal("m"), MathRun.Vector("g"), MathRun.Subscript("eff")));
        group.Add(MathText(@"\mathbf{R}+m\mathbf{g}_{\mathrm{eff}}=0", 900, 560, 21, Amber, "middle",
            MathRun.Vector("R"), MathRun.Normal(" + m"), MathRun.Vector("g"), MathRun.Subscript("eff"), MathRun.Normal(" = 0")));
        group.Add(Text("支持力不参与有效重力的定义", 900, 605, 16, Ink, "middle"));
    }

    private static void RenderGravityCaseComparison(XElement group)
    {
        var columns = new[]
        {
            (70d, "绕地轨道", Blue),
            (390d, "自由落体电梯", Magenta),
            (710d, "地表相对静止", Green),
        };
        foreach (var column in columns)
        {
            group.Add(Rect(column.Item1, 170, 285, 430, Panel, "#CBD5E1", 1));
            group.Add(Text(column.Item2, column.Item1 + 142, 225, 21, column.Item3, "middle"));
            group.Add(Text("地球引力", column.Item1 + 32, 320, 16, Muted));
            group.Add(Text("物体加速度", column.Item1 + 32, 405, 16, Muted));
            group.Add(Text("秤读数/支持力", column.Item1 + 142, 480, 16, Muted, "middle"));
        }

        foreach (var x in new[] { 315d, 635d, 955d })
        {
            group.Add(MathText(@"\mathbf{F}_g\ne0", x, 320, 19, Ink, "end",
                MathRun.Vector("F"), MathRun.Subscript("g"), MathRun.Normal(" ≠ 0")));
        }
        group.Add(MathText(@"\mathbf{a}_{\mathrm{orbit}}\ne0", 315, 405, 19, Ink, "end",
            MathRun.Vector("a"), MathRun.Subscript("orbit"), MathRun.Normal(" ≠ 0")));
        group.Add(MathText(@"\mathbf{a}\approx\mathbf{g}\ne0", 635, 405, 19, Ink, "end",
            MathRun.Vector("a"), MathRun.Normal(" ≈ "), MathRun.Vector("g"), MathRun.Normal(" ≠ 0")));
        group.Add(MathText(@"\mathbf{g}_{\mathrm{eff}}\ne0", 955, 405, 19, Ink, "end",
            MathRun.Vector("g"), MathRun.Subscript("eff"), MathRun.Normal(" ≠ 0")));
        group.Add(MathText(@"\mathbf{N}\approx0", 212, 525, 19, Blue, "middle", MathRun.Vector("N"), MathRun.Normal(" ≈ 0")));
        group.Add(MathText(@"\mathbf{N}\approx0", 532, 525, 19, Magenta, "middle", MathRun.Vector("N"), MathRun.Normal(" ≈ 0")));
        group.Add(MathText(@"\mathbf{N}\approx m\mathbf{g}_{\mathrm{eff}}", 852, 525, 19, Green, "middle",
            MathRun.Vector("N"), MathRun.Normal(" ≈ m"), MathRun.Vector("g"), MathRun.Subscript("eff")));
        group.Add(Text("失重判据：支持力/秤读数接近零，而不是地球引力消失", 550, 685, 19, Amber, "middle"));
    }

    private static void RenderGravityReferenceFrames(XElement group)
    {
        group.Add(Rect(70, 165, 460, 450, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(610, 165, 460, 450, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("惯性系", 300, 220, 24, Blue, "middle"));
        group.Add(Text("只画真实相互作用力", 300, 280, 19, Ink, "middle"));
        group.Add(MathText(@"\sum\mathbf{F}_{\mathrm{real}}=m\mathbf{a}", 300, 350, 22, Blue, "middle",
            MathRun.Normal("Σ"), MathRun.Vector("F"), MathRun.Subscript("real"), MathRun.Normal(" = m"), MathRun.Vector("a")));
        group.Add(Text("向心力 = 合力的径向角色", 300, 430, 17, Muted, "middle"));
        group.Add(Text("不要再额外添加一支“向心力”箭头", 300, 505, 16, Amber, "middle"));

        group.Add(Text("随动/旋转非惯性系", 840, 220, 24, Magenta, "middle"));
        group.Add(Text("声明参考系后加入惯性力", 840, 280, 19, Ink, "middle"));
        group.Add(MathText(@"\sum\mathbf{F}_{\mathrm{real}}+\mathbf{F}_{\mathrm{inertial}}=m\mathbf{a}_{\mathrm{rel}}", 840, 350, 21, Magenta, "middle",
            MathRun.Normal("Σ"), MathRun.Vector("F"), MathRun.Subscript("real"), MathRun.Normal(" + "),
            MathRun.Vector("F"), MathRun.Subscript("inertial"), MathRun.Normal(" = m"), MathRun.Vector("a"), MathRun.Subscript("rel")));
        group.Add(Text("自由落体随动系可见近似平衡", 840, 430, 17, Muted, "middle"));
        group.Add(Text("不得与惯性系方程混用", 840, 505, 17, Amber, "middle"));
        group.Add(Text("先选参考系 → 列真实力 → 必要时加惯性力 → 再解释秤读数", 570, 690, 18, Green, "middle"));
    }

    private static void RenderThermistorCircuitDivider(XElement group)
    {
        group.Add(Rect(70, 155, 500, 430, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(630, 155, 500, 430, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Text("题图电路：串联分压", 320, 205, 23, Blue, "middle"));
        group.Add(Line(150, 300, 250, 300, Ink, 3));
        group.Add(Rect(250, 280, 110, 40, "#FFFFFF", Ink, 2));
        group.Add(Text("定值电阻 R0", 305, 305, 16, Ink, "middle"));
        group.Add(Line(360, 300, 445, 300, Ink, 3));
        group.Add(Rect(445, 280, 105, 40, "#FFFFFF", Blue, 2));
        group.Add(Text("热敏电阻 R1", 497, 305, 16, Blue, "middle"));
        group.Add(Line(550, 300, 550, 430, Ink, 3));
        group.Add(Line(550, 430, 150, 430, Ink, 3));
        group.Add(Line(150, 430, 150, 300, Ink, 3));
        group.Add(Text("电源 U总", 150, 470, 17, Ink, "middle"));
        group.Add(Rect(330, 360, 130, 52, "#FFFFFF", Green, 2));
        group.Add(Text("电压表测 R1", 395, 392, 16, Green, "middle"));
        var voltmeterRightLead = Line(460, 360, 550, 320, Green, 2);
        voltmeterRightLead.SetAttributeValue("data-thermistor-role", "voltmeter-right-lead");
        group.Add(voltmeterRightLead);
        var voltmeterLeftLead = Line(330, 360, 360, 320, Green, 2);
        voltmeterLeftLead.SetAttributeValue("data-thermistor-role", "voltmeter-left-lead");
        group.Add(voltmeterLeftLead);
        group.Add(Text("I=U总/(R0+R1)", 320, 520, 20, Ink, "middle"));
        group.Add(Text("电流随 R1 变化", 320, 555, 18, Amber, "middle"));
        group.Add(Text("测量与公式边界", 880, 205, 23, Ink, "middle"));
        group.Add(MathText(@"U_{R1}=U_{总}\frac{R_1}{R_0+R_1}", 880, 300, 24, Blue, "middle",
            MathRun.Normal("U"), MathRun.Subscript("R1"), MathRun.Normal(" = U总 R1/(R0+R1)")));
        group.Add(Text("电压表读数随 R1 增大而增大", 880, 370, 18, Green, "middle"));
        group.Add(Text("串联电流不是常量", 880, 430, 20, Magenta, "middle"));
        group.Add(Text("ΔR=ΔU/I 不能直接跨区间使用", 880, 510, 18, Amber, "middle"));
    }

    private static void RenderThermistorCurvature(XElement group)
    {
        group.Add(Rect(70, 145, 700, 510, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Rect(810, 145, 320, 510, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("U-R1", 115, 190, 22, Ink));
        group.Add(Line(150, 570, 700, 570, Ink, 3, arrow: true));
        group.Add(Line(150, 570, 150, 220, Ink, 3, arrow: true));
        group.Add(Text("R1", 700, 600, 18, Ink, "end"));
        group.Add(Text("U", 125, 220, 18, Ink, "end"));
        var points = new[] { (150d, 570d), (240d, 500d), (340d, 430d), (450d, 370d), (570d, 325d), (680d, 295d) };
        for (var i = 1; i < points.Length; i++)
        {
            group.Add(Line(points[i - 1].Item1, points[i - 1].Item2, points[i].Item1, points[i].Item2, Blue, 4));
        }
        group.Add(Text("凹函数：斜率递减", 400, 265, 19, Blue, "middle"));
        group.Add(Line(280, 500, 280, 430, Amber, 2, arrow: true));
        group.Add(Line(280, 430, 410, 430, Amber, 2, arrow: true));
        group.Add(Text("相同 ΔU", 345, 415, 17, Amber, "middle"));
        group.Add(Line(410, 430, 410, 365, Magenta, 2, arrow: true));
        group.Add(Line(410, 365, 590, 365, Magenta, 2, arrow: true));
        group.Add(Text("ΔR后段更大", 500, 350, 17, Magenta, "middle"));
        group.Add(Text("U=U总R1/(R0+R1)", 970, 235, 20, Blue, "middle"));
        group.Add(Text("凹函数", 970, 300, 24, Green, "middle"));
        group.Add(Text("斜率递减", 970, 360, 20, Ink, "middle"));
        group.Add(Text("相同 ΔU", 900, 445, 18, Amber, "middle"));
        group.Add(Text("ΔR后段更大", 1040, 445, 18, Magenta, "middle"));
        group.Add(Text("线性关系不成立", 970, 535, 20, Amber, "middle"));
    }

    private static void RenderThermistorError(XElement group)
    {
        group.Add(Rect(70, 155, 510, 470, "#FFF7ED", "#FDBA74", 1));
        group.Add(Rect(620, 155, 510, 470, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("错误近似", 325, 215, 25, Amber, "middle"));
        group.Add(Text("ΔR=ΔU/I", 325, 305, 28, Ink, "middle"));
        group.Add(Text("把 I 当作同一常量", 325, 370, 20, Amber, "middle"));
        group.Add(Text("跨温区电流实际改变", 325, 440, 19, Ink, "middle"));
        group.Add(Text("不能用变化前后某一个 I", 325, 510, 18, Magenta, "middle"));
        group.Add(Text("正确边界", 875, 215, 25, Blue, "middle"));
        group.Add(Text("ΔR=ΔU/I 仅在 I 恒定时成立", 875, 305, 21, Blue, "middle"));
        group.Add(Text("本题 I 会变化", 875, 385, 22, Green, "middle"));
        group.Add(Text("先写 I=U总/(R0+R1)", 875, 455, 20, Ink, "middle"));
        group.Add(Text("再比较函数曲率和区间", 875, 525, 19, Green, "middle"));
    }

    private static void RenderThermistorSpecialValues(XElement group)
    {
        group.Add(Rect(70, 155, 500, 470, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(630, 155, 500, 470, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Text("极限方向", 320, 215, 25, Blue, "middle"));
        group.Add(Text("R1→0", 200, 320, 28, Ink, "middle"));
        group.Add(Text("U→0", 440, 320, 28, Green, "middle"));
        group.Add(Text("R1→∞", 200, 430, 28, Ink, "middle"));
        group.Add(Text("U→U总", 440, 430, 28, Green, "middle"));
        group.Add(Text("方向验证，不给出题设参数", 320, 540, 18, Amber, "middle"));
        group.Add(Text("特殊值示例", 880, 215, 25, Ink, "middle"));
        group.Add(Text("示例参数，仅作方向验证", 880, 315, 20, Blue, "middle"));
        group.Add(Text("R0=20Ω，U总=6V（示例）", 880, 385, 18, Ink, "middle"));
        group.Add(Text("不等同题设实测", 880, 460, 22, Magenta, "middle"));
        group.Add(Text("结论：后段 ΔR 小于前段", 880, 545, 20, Green, "middle"));
    }

    private static void RenderArchimedesDefinition(XElement group)
    {
        group.Add(Rect(70, 150, 500, 470, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(630, 150, 500, 470, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("体积定义", 320, 205, 25, Blue, "middle"));
        group.Add(Line(120, 340, 520, 340, Blue, 3));
        group.Add(Rect(245, 255, 160, 210, "#CBD5E1", Ink, 2));
        group.Add(Text("物体", 325, 360, 20, Ink, "middle"));
        group.Add(Text("V浸 = V排", 320, 510, 24, Blue, "middle"));
        group.Add(Text("浸入液面以下所占空间", 320, 555, 18, Ink, "middle"));
        group.Add(Text("公式条件", 880, 205, 25, Amber, "middle"));
        group.Add(Text("ρ液V排g 不是脱离边界的标签", 880, 300, 19, Amber, "middle"));
        group.Add(Text("上、下表面与流体接触", 880, 385, 22, Green, "middle"));
        group.Add(Text("模型边界", 880, 450, 24, Magenta, "middle"));
        group.Add(Text("底部贴合需单独核验压力", 880, 530, 19, Ink, "middle"));
    }

    private static void RenderArchimedesWaterModel(XElement group)
    {
        group.Add(Rect(80, 150, 1040, 490, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("理想水体替换模型", 600, 205, 26, Blue, "middle"));
        group.Add(Line(140, 315, 1060, 315, Blue, 3));
        group.Add(Rect(430, 315, 340, 220, "#BAE6FD", Blue, 2));
        group.Add(Text("同体积理想水体", 600, 420, 24, Blue, "middle"));
        group.Add(Line(600, 315, 600, 245, Green, 4, arrow: true));
        group.Add(Line(600, 535, 600, 600, Amber, 4, arrow: true));
        group.Add(Text("压力合力 F液合 ↑", 360, 280, 19, Green, "middle"));
        group.Add(Text("重力 G水体 ↓", 840, 610, 19, Amber, "middle"));
        group.Add(Text("F液合 = G水体 = ρ液V排g", 600, 690, 22, Ink, "middle"));
        group.Add(Text("所有表面与流体接触；静止流体模型", 600, 735, 18, Magenta, "middle"));
    }

    private static void RenderArchimedesBottomContact(XElement group)
    {
        group.Add(Rect(70, 150, 500, 470, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Rect(630, 150, 500, 470, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("底面完全贴合", 320, 205, 25, Amber, "middle"));
        group.Add(Rect(190, 280, 260, 170, "#BAE6FD", Blue, 2));
        group.Add(Line(170, 470, 470, 470, Ink, 5));
        group.Add(Text("缺失底面压力", 320, 520, 22, Magenta, "middle"));
        group.Add(Text("边界改变", 320, 565, 18, Ink, "middle"));
        group.Add(Text("条件化修正", 880, 205, 25, Blue, "middle"));
        group.Add(Text("F液合=ρ液V排g-F底", 880, 300, 24, Blue, "middle"));
        group.Add(Text("支持力 + 液压合力 + 重力 = 0", 880, 385, 19, Ink, "middle"));
        group.Add(Text("需按边界条件修正", 880, 465, 23, Green, "middle"));
        group.Add(Text("不能无条件套用阿基米德公式", 880, 545, 18, Amber, "middle"));
    }

    private static void RenderArchimedesDepthDependence(XElement group)
    {
        group.Add(Rect(70, 150, 650, 500, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(780, 150, 350, 500, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Text("底面贴合：水深改变压力项", 395, 205, 23, Blue, "middle"));
        group.Add(Line(120, 300, 650, 300, Blue, 3));
        group.Add(Rect(270, 300, 220, 180, "#BAE6FD", Blue, 2));
        group.Add(Line(255, 480, 505, 480, Ink, 5));
        group.Add(Line(540, 480, 540, 270, Amber, 3, arrow: true));
        group.Add(Text("水深 h", 575, 280, 18, Amber));
        group.Add(Text("底面压力项", 380, 545, 20, Magenta, "middle"));
        group.Add(Text("浮力与深度无关需条件", 955, 250, 22, Blue, "middle"));
        group.Add(Text("水深 h ↑", 955, 335, 22, Amber, "middle"));
        group.Add(Text("底面压力项改变", 955, 405, 20, Magenta, "middle"));
        group.Add(Text("合力可能减小并改变方向", 955, 480, 18, Ink, "middle"));
        group.Add(Text("需要条件", 955, 555, 24, Green, "middle"));
    }

    private static void RenderArchimedesTopContact(XElement group)
    {
        group.Add(Rect(70, 150, 500, 470, "#EFF6FF", "#93C5FD", 1));
        group.Add(Rect(630, 150, 500, 470, "#FFF7ED", "#FDBA74", 1));
        group.Add(Text("底部贴合", 320, 205, 25, Blue, "middle"));
        group.Add(Line(170, 275, 470, 275, Ink, 5));
        group.Add(Rect(230, 275, 180, 170, "#BAE6FD", Blue, 2));
        group.Add(Text("缺少底面压力", 320, 520, 20, Blue, "middle"));
        group.Add(Text("顶部贴合", 880, 205, 25, Amber, "middle"));
        group.Add(Line(730, 450, 1030, 450, Ink, 5));
        group.Add(Rect(790, 280, 180, 170, "#BAE6FD", Blue, 2));
        group.Add(Text("缺少顶部压力", 880, 520, 20, Amber, "middle"));
        group.Add(Text("压力方向依接触面", 600, 690, 22, Magenta, "middle"));
    }

    private static void RenderArchimedesPier(XElement group)
    {
        group.Add(Rect(70, 150, 650, 500, "#F8FAFC", "#CBD5E1", 1));
        group.Add(Rect(780, 150, 350, 500, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("倾斜桥墩", 395, 205, 25, Blue, "middle"));
        group.Add(Line(130, 540, 650, 540, Blue, 3));
        group.Add(Line(270, 470, 470, 260, Ink, 12));
        group.Add(Line(300, 435, 250, 390, Amber, 4, arrow: true));
        group.Add(Line(440, 300, 510, 350, Magenta, 4, arrow: true));
        group.Add(Text("侧压力分量", 395, 585, 20, Amber, "middle"));
        group.Add(Text("相同截面高度", 955, 250, 22, Blue, "middle"));
        group.Add(Text("左右压力分量反向", 955, 335, 20, Ink, "middle"));
        group.Add(Text("积分核验", 955, 425, 25, Green, "middle"));
        group.Add(Text("蓝色体积不能直接证明合力", 955, 520, 18, Amber, "middle"));
    }

    private static void RenderArchimedesPressureCaveat(XElement group)
    {
        group.Add(Rect(70, 150, 500, 470, "#FFF7ED", "#FDBA74", 1));
        group.Add(Rect(630, 150, 500, 470, "#EFF6FF", "#93C5FD", 1));
        group.Add(Text("压力模型", 320, 205, 25, Amber, "middle"));
        group.Add(Text("表压/绝对压强", 320, 310, 24, Blue, "middle"));
        group.Add(Text("接触界面", 320, 390, 24, Magenta, "middle"));
        group.Add(Text("系统边界先于数值代入", 320, 505, 20, Ink, "middle"));
        group.Add(Text("审查边界", 880, 205, 25, Green, "middle"));
        group.Add(Text("p0S 不是普遍修正", 880, 320, 25, Green, "middle"));
        group.Add(Text("需说明哪些表面接触流体", 880, 405, 20, Ink, "middle"));
        group.Add(Text("不能由单一示例断言所有实验都等于同一支持力", 880, 520, 18, Amber, "middle"));
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

    private static XElement DashedGravityLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role)
    {
        var line = GravityLine(x1, y1, x2, y2, color, width, role);
        line.SetAttributeValue("stroke-dasharray", "7 6");
        return line;
    }

    private static XElement MathText(
        string tex,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor,
        params MathRun[] runs)
    {
        return MathLayout.Render(
            new ScientificMathLayout.FormulaSpec(
                tex,
                new ScientificMathLayout.MathExpression(
                    [new ScientificMathLayout.MathTextNode(runs)])),
            x,
            y,
            fontSize,
            color,
            anchor);
    }

    private static XElement FractionFormula(
        string tex,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor,
        params FormulaPiece[] pieces)
    {
        var nodes = pieces.Select(piece => piece.Denominator is null
            ? (ScientificMathLayout.MathNode)new ScientificMathLayout.MathTextNode(
                [ScientificMathLayout.MathRun.Normal(piece.Text!)])
            : new ScientificMathLayout.MathFractionNode(piece.Text!, piece.Denominator))
            .ToArray();
        return MathLayout.Render(
            new ScientificMathLayout.FormulaSpec(
                tex,
                new ScientificMathLayout.MathExpression(nodes)),
            x,
            y,
            fontSize,
            color,
            anchor);
    }

    private static XElement Text(
        string value,
        double x,
        double y,
        int fontSize,
        string color,
        string anchor = "start")
    {
        if (ScientificMathLayout.LooksLikeUnparsedFormula(value))
        {
            throw new InvalidOperationException(
                $"Visible ordinary text contains an unparsed formula fragment: '{value}'.");
        }

        var bounds = MeasureTextBounds(value, x, y, fontSize, anchor);

        return new(
            Svg + "text",
            new XAttribute("x", Number(x)),
            new XAttribute("y", Number(y)),
            new XAttribute("text-anchor", anchor),
            new XAttribute("font-family", "Microsoft YaHei"),
            new XAttribute("font-size", fontSize),
            new XAttribute("fill", color),
            new XAttribute("data-text-bounds", FormatBounds(bounds)),
            new XAttribute("data-content-kind", FigureElementKind.Entity),
            value);
    }

    private static SKRect MeasureTextBounds(
        string value,
        double x,
        double baseline,
        int fontSize,
        string anchor)
    {
        using var paint = new SKPaint { IsAntialias = true };
        using var typeface = ResolveTextTypeface(value);
        using var font = new SKFont(typeface, fontSize);
        var advance = font.MeasureText(value, out var glyphBounds, paint);
        var origin = anchor switch
        {
            "middle" => x - advance / 2,
            "end" => x - advance,
            "start" => x,
            _ => throw new InvalidOperationException($"Unsupported SVG text anchor: '{anchor}'."),
        };
        return new SKRect(
            (float)(origin + glyphBounds.Left),
            (float)(baseline + glyphBounds.Top),
            (float)(origin + glyphBounds.Right),
            (float)(baseline + glyphBounds.Bottom));
    }

    private static SKTypeface ResolveTextTypeface(string value)
    {
        const string family = "Microsoft YaHei";
        var primary = SKTypeface.FromFamilyName(family);
        if (primary.ContainsGlyphs(value))
        {
            return primary;
        }

        primary.Dispose();
        var manager = SKFontManager.Default;
        foreach (var character in value.Where(character => !char.IsWhiteSpace(character)))
        {
            var fallback = manager.MatchCharacter(family, character)
                ?? manager.MatchCharacter(character);
            if (fallback is not null && fallback.ContainsGlyphs(value))
            {
                return fallback;
            }

            fallback?.Dispose();
        }

        throw new InvalidOperationException(
            $"No installed typeface can measure the approved SVG text: '{value}'.");
    }

    private static string FormatBounds(SKRect bounds) =>
        string.Join(",", Number(bounds.Left), Number(bounds.Top), Number(bounds.Width), Number(bounds.Height));

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
