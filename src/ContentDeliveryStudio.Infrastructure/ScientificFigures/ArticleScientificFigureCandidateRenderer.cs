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
            case ArticleScientificFigureCandidateKind.BernoulliFanEnergy: RenderBernoulliFanEnergy(group); break;
            case ArticleScientificFigureCandidateKind.BernoulliFanZones: RenderBernoulliFanZones(group); break;
            case ArticleScientificFigureCandidateKind.BernoulliStreamlineBoundary: RenderBernoulliStreamlineBoundary(group); break;
            case ArticleScientificFigureCandidateKind.PinholeGeometry: RenderPinholeGeometry(group); break;
            case ArticleScientificFigureCandidateKind.PinholeFocusPlane: RenderPinholeFocusPlane(group); break;
            case ArticleScientificFigureCandidateKind.PinholeObservation: RenderPinholeObservation(group); break;
            case ArticleScientificFigureCandidateKind.SuperconductingEnergy: RenderSuperconductingEnergy(group); break;
            case ArticleScientificFigureCandidateKind.SuperconductingPersistentCurrent: RenderSuperconductingPersistentCurrent(group); break;
            case ArticleScientificFigureCandidateKind.SuperconductingExcitation: RenderSuperconductingExcitation(group); break;
            case ArticleScientificFigureCandidateKind.MeterTransientResponse: RenderMeterTransientResponse(group); break;
            case ArticleScientificFigureCandidateKind.MeterTrialDecision: RenderMeterTrialDecision(group); break;
            case ArticleScientificFigureCandidateKind.MeterProtectionLayers: RenderMeterProtectionLayers(group); break;
            case ArticleScientificFigureCandidateKind.BoilingPreBubbleCollapse: RenderBoilingPreBubbleCollapse(group); break;
            case ArticleScientificFigureCandidateKind.BoilingBubbleGrowth: RenderBoilingBubbleGrowth(group); break;
            case ArticleScientificFigureCandidateKind.BoilingPressureScale: RenderBoilingPressureScale(group); break;
            case ArticleScientificFigureCandidateKind.GalileanAfocalPath: RenderGalileanAfocalPath(group); break;
            case ArticleScientificFigureCandidateKind.GalileanVirtualObjectRegimes: RenderGalileanVirtualObjectRegimes(group); break;
            case ArticleScientificFigureCandidateKind.GalileanAngularMagnification: RenderGalileanAngularMagnification(group); break;
            case ArticleScientificFigureCandidateKind.DryIceWaterMechanism: RenderDryIceWaterMechanism(group); break;
            case ArticleScientificFigureCandidateKind.DryIceHeatTransferComparison: RenderDryIceHeatTransferComparison(group); break;
            case ArticleScientificFigureCandidateKind.DryIceIsolationVerification: RenderDryIceIsolationVerification(group); break;
            case ArticleScientificFigureCandidateKind.LeverRockContact: RenderLeverRockContact(group); break;
            case ArticleScientificFigureCandidateKind.LeverSeesawFriction: RenderLeverSeesawFriction(group); break;
            case ArticleScientificFigureCandidateKind.LeverTwoForceMember: RenderLeverTwoForceMember(group); break;
            case ArticleScientificFigureCandidateKind.RestIntervalDefinition: RenderRestIntervalDefinition(group); break;
            case ArticleScientificFigureCandidateKind.RestZeroVelocityTurningPoint: RenderRestZeroVelocityTurningPoint(group); break;
            case ArticleScientificFigureCandidateKind.RestStateComparison: RenderRestStateComparison(group); break;
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

    private static void RenderPinholeGeometry(XElement g)
    {
        g.Add(ArticleLine(95, 610, 95, 260, Ink, 5, "object"));
        g.Add(ArticleLine(95, 260, 75, 300, Ink, 5, "object"));
        g.Add(ArticleLine(95, 260, 115, 300, Ink, 5, "object"));
        g.Add(Text("发光物体", 95, 645, 20, Ink, "middle"));
        g.Add(ArticleLine(520, 170, 520, 355, Ink, 9, "barrier"));
        g.Add(ArticleLine(520, 365, 520, 650, Ink, 9, "barrier"));
        g.Add(ArticleLine(510, 355, 530, 355, Ink, 4, "barrier"));
        g.Add(ArticleLine(510, 365, 530, 365, Ink, 4, "barrier"));
        g.Add(Text("小孔", 520, 335, 21, Blue, "middle"));
        g.Add(ArticleLine(95, 260, 520, 360, Blue, 3, "principal-ray", true));
        g.Add(ArticleLine(520, 360, 1030, 610, Magenta, 3, "principal-ray", true));
        g.Add(ArticleLine(95, 610, 520, 360, Blue, 3, "principal-ray", true));
        g.Add(ArticleLine(520, 360, 1030, 260, Magenta, 3, "principal-ray", true));
        g.Add(ArticleLine(1050, 180, 1050, 650, Green, 6, "image-plane"));
        g.Add(ArticleLine(1010, 260, 1010, 610, Magenta, 5, "inverted-image"));
        g.Add(ArticleLine(1010, 610, 990, 570, Magenta, 5, "inverted-image"));
        g.Add(ArticleLine(1010, 610, 1030, 570, Magenta, 5, "inverted-image"));
        g.Add(Text("倒立实像所在平面", 1040, 695, 20, Magenta, "end"));
        g.Add(Text("可视范围", 790, 215, 18, Amber, "middle"));
        g.Add(Text("孔径限制通光量；观察者能截取的像域还受瞳孔/镜头口径限制", 600, 740, 18, Amber, "middle"));
    }

    private static void RenderPinholeFocusPlane(XElement g)
    {
        var planes = new[] { (180d, "光源处/光源平面", Blue), (490d, "小孔处/小孔平面", Amber), (765d, "倒立像位置（无屏）", Magenta) };
        foreach (var (x, label, color) in planes) { g.Add(ArticleLine(x, 190, x, 560, color, 4, "focus-plane")); g.Add(Text(label, x, 165, 20, color, "middle")); }
        g.Add(ArticleLine(180, 260, 490, 365, Blue, 3, "ray", true));
        g.Add(ArticleLine(180, 490, 490, 365, Blue, 3, "ray", true));
        g.Add(ArticleLine(490, 365, 765, 490, Magenta, 3, "ray", true));
        g.Add(ArticleLine(490, 365, 765, 260, Magenta, 3, "ray", true));
        g.Add(ArticleRect(835, 220, 275, 340, "#EFF6FF", Blue, 3, "camera-body"));
        DrawLens(g, 900, 270, 510, "相机镜头", Blue);
        g.Add(ArticleLine(765, 260, 900, 300, Magenta, 3, "camera-input-ray", true));
        g.Add(ArticleLine(765, 260, 900, 430, Magenta, 3, "camera-input-ray", true));
        g.Add(ArticleLine(900, 300, 1060, 365, Green, 3, "camera-focused-ray", true));
        g.Add(ArticleLine(900, 430, 1060, 365, Green, 3, "camera-focused-ray", true));
        g.Add(ArticleLine(1060, 270, 1060, 510, Green, 5, "sensor"));
        g.Add(ArticleLine(1045, 270, 1075, 270, Green, 3, "sensor"));
        g.Add(Text("传感器", 1080, 535, 17, Green, "end"));
        g.Add(Text("手动对焦：镜头把所选物距对应的平面清晰成像到传感器", 600, 625, 21, Ink, "middle"));
        g.Add(Text("对焦小孔→孔清晰；对焦光源→正立物体清晰；对焦像位置→倒立像清晰（无需放屏）", 600, 685, 18, Amber, "middle"));
    }

    private static void RenderPinholeObservation(XElement g)
    {
        g.Add(Rect(55, 155, 530, 500, "#EFF6FF", Blue, 2));
        g.Add(Rect(615, 155, 530, 500, "#ECFDF5", Green, 2));
        g.Add(Text("光源近：视场只覆盖局部", 320, 195, 21, Blue, "middle"));
        g.Add(Text("光源远：视场可覆盖全物体", 880, 195, 21, Green, "middle"));
        g.Add(Text("近距", 90, 225, 17, Blue));
        g.Add(Text("远距", 650, 225, 17, Green));
        foreach (var left in new[] { 105d, 665d })
        {
            var side = left < 500 ? "near" : "far";
            g.Add(ArticleLine(left + 175, 250, left + 175, 535, Ink, 6, "barrier"));
            g.Add(ArticleLine(left + 172, 390, left + 178, 390, "#FFFFFF", 10, $"{side}-aperture"));
            g.Add(ArticleRect(left + 360, 305, 100, 170, "#FFFFFF", Muted, 3, $"{side}-camera"));
            DrawLens(g, left + 385, 330, 450, "相机", Muted);
        }
        g.Add(ArticleLine(105, 275, 280, 390, Blue, 3, "near-field", true));
        g.Add(ArticleLine(105, 505, 280, 390, Blue, 3, "near-field", true));
        g.Add(ArticleLine(280, 390, 465, 350, Blue, 3, "near-field", true));
        g.Add(ArticleLine(420, 350, 445, 345, Blue, 2, "near-field"));
        g.Add(ArticleRect(90, 250, 55, 280, "#DBEAFE", Blue, 3, "near-object"));
        g.Add(Text("大物体", 118, 560, 17, Blue, "middle"));
        g.Add(ArticleLine(645, 320, 840, 390, Green, 3, "far-field", true));
        g.Add(ArticleLine(645, 460, 840, 390, Green, 3, "far-field", true));
        g.Add(ArticleLine(840, 390, 1025, 340, Green, 3, "far-field", true));
        g.Add(ArticleLine(980, 350, 1005, 343, Green, 2, "far-field"));
        g.Add(ArticleRect(635, 305, 35, 170, "#D1FAE5", Green, 3, "far-object"));
        g.Add(Text("全景", 652, 505, 17, Green, "middle"));
        g.Add(Text("实验条件：相机靠近小孔，同时让光源远离小孔", 600, 710, 21, Ink, "middle"));
    }

    private static void RenderSuperconductingEnergy(XElement g)
    {
        g.Add(Rect(65, 170, 1070, 470, "#F8FAFC", "#CBD5E1", 2));
        g.Add(ArticleRect(130, 320, 170, 160, "#EFF6FF", Blue, 3, "power-source"));
        g.Add(Text("直流电源", 215, 405, 22, Blue, "middle"));

        // Draw a real excitation loop: source -> closed switch -> coil -> return.
        g.Add(ArticleLine(300, 350, 385, 350, Ink, 4, "circuit", true));
        g.Add(ArticleLine(435, 350, 460, 350, Ink, 4, "circuit", true));
        g.Add(ArticleLine(460, 350, 460, 400, Ink, 4, "circuit"));
        g.Add(ArticleLine(460, 400, 468, 400, Ink, 4, "circuit", true));
        for (var x = 500; x <= 800; x += 55) DrawArticleCircle(g, x, 400, 32, Magenta, 3, "coil");
        g.Add(ArticleLine(832, 400, 900, 400, Ink, 4, "circuit", true));
        g.Add(ArticleLine(900, 400, 900, 450, Ink, 4, "circuit"));
        g.Add(ArticleLine(900, 450, 300, 450, Ink, 4, "circuit", true));
        g.Add(ArticleLine(385, 350, 420, 330, Amber, 5, "switch"));
        g.Add(ArticleLine(420, 330, 435, 350, Amber, 5, "switch"));
        g.Add(Text("闭合开关", 410, 300, 18, Amber, "middle"));
        g.Add(Text("超导线圈 L", 650, 500, 22, Magenta, "middle"));
        g.Add(ArticleLine(315, 535, 470, 535, Amber, 4, "electrical-work", true));
        g.Add(Text("电功输入", 390, 565, 18, Amber, "middle"));
        for (var x = 500; x <= 800; x += 65) g.Add(ArticleLine(x, 270, x + 40, 270, Green, 3, "magnetic-field", true));
        g.Add(Text("建立磁场：电流变化，电源向磁场输入能量", 600, 230, 22, Green, "middle"));
        g.Add(Text("电能 → 磁能", 850, 330, 21, Amber, "middle"));
        g.Add(Text("储能 W = ½LI²", 850, 385, 24, Ink, "middle"));
        g.Add(Text("I 稳定后：磁场能保持不变；理想超导闭环无焦耳损耗", 600, 590, 21, Blue, "middle"));
        g.Add(Text("断流时磁场衰减，磁能可通过感应电动势回到电路", 600, 700, 19, Amber, "middle"));
    }

    private static void RenderSuperconductingPersistentCurrent(XElement g)
    {
        g.Add(Rect(70, 155, 500, 500, "#FFF7ED", Amber, 2));
        g.Add(Rect(630, 155, 500, 500, "#ECFDF5", Green, 2));
        g.Add(Text("励磁阶段", 320, 200, 23, Amber, "middle")); g.Add(Text("持久电流阶段", 880, 200, 23, Green, "middle"));
        g.Add(ArticleRect(105, 330, 120, 110, "#FFFFFF", Blue, 3, "power-source")); g.Add(Text("励磁电源", 165, 395, 18, Blue, "middle"));
        g.Add(ArticleLine(225, 350, 495, 350, Amber, 4, "charging-loop", true));
        g.Add(Text("励磁主回路接通", 360, 320, 17, Amber, "middle"));
        for (var x = 325; x <= 465; x += 47) DrawArticleCircle(g, x, 465, 28, Magenta, 3, "charging-coil");
        g.Add(ArticleLine(495, 350, 495, 465, Magenta, 4, "charging-loop"));
        g.Add(ArticleLine(325, 465, 245, 465, Magenta, 4, "charging-loop", true));
        g.Add(ArticleLine(245, 465, 297, 465, Magenta, 4, "charging-loop"));
        g.Add(ArticleLine(245, 465, 245, 420, Magenta, 4, "charging-loop"));
        g.Add(ArticleLine(245, 420, 225, 420, Magenta, 4, "charging-loop"));
        g.Add(Text("线圈电流逐渐增大", 390, 540, 17, Magenta, "middle"));
        DrawCircle(g, 880, 420, 165, Green, 5);
        for (var angle = 0; angle < 360; angle += 90) { var r = angle * Math.PI / 180; g.Add(ArticleLine(880 + 130 * Math.Cos(r), 420 + 130 * Math.Sin(r), 880 + 130 * Math.Cos(r + .45), 420 + 130 * Math.Sin(r + .45), Green, 5, "persistent-current", true)); }
        g.Add(Text("超导开关闭合", 880, 410, 20, Green, "middle")); g.Add(Text("恒定电流 I", 880, 450, 22, Magenta, "middle"));
        g.Add(Text("闭合通路", 880, 520, 19, Green, "middle"));
        g.Add(Text("撤去励磁电源 ≠ 断开线圈回路", 600, 710, 23, Ink, "middle"));
    }

    private static void RenderSuperconductingExcitation(XElement g)
    {
        g.Add(ArticleRect(90, 175, 1020, 470, "#EFF6FF", Blue, 3, "cryostat"));
        g.Add(Text("液氦槽 4.2 K", 600, 615, 21, Blue, "middle"));
        g.Add(Rect(135, 290, 150, 105, "#FFFFFF", Blue, 3)); g.Add(Text("励磁电源", 210, 352, 20, Blue, "middle"));
        g.Add(ArticleLine(285, 315, 380, 315, Blue, 4, "excitation-circuit", true));
        g.Add(ArticleLine(380, 315, 380, 445, Blue, 4, "excitation-circuit"));
        for (var x = 420; x <= 805; x += 55) DrawArticleCircle(g, x, 445, 34, Green, 4, "main-coil");
        g.Add(ArticleLine(380, 445, 386, 445, Blue, 4, "excitation-circuit"));
        g.Add(ArticleLine(840, 445, 895, 445, Blue, 4, "excitation-circuit"));
        g.Add(ArticleLine(895, 445, 895, 370, Blue, 4, "excitation-circuit"));
        g.Add(ArticleLine(895, 370, 285, 370, Blue, 4, "excitation-circuit", true));
        g.Add(ArticleLine(300, 370, 350, 370, Blue, 2, "excitation-circuit", true));
        g.Add(ArticleLine(835, 445, 875, 445, Blue, 2, "excitation-circuit", true));
        g.Add(Text("MRI 主磁体超导线圈", 610, 520, 22, Green, "middle"));
        g.Add(ArticleRect(470, 235, 230, 70, "#FDF4FF", Magenta, 3, "superconducting-switch")); g.Add(Text("超导开关（并联支路）", 585, 278, 18, Magenta, "middle"));
        g.Add(ArticleLine(400, 315, 470, 270, Magenta, 3, "persistent-switch-branch"));
        g.Add(ArticleLine(700, 270, 875, 370, Magenta, 3, "persistent-switch-branch"));
        g.Add(Rect(760, 195, 150, 65, "#FFF7ED", Amber, 3)); g.Add(Text("加热电源", 835, 235, 17, Amber, "middle"));
        g.Add(ArticleRect(585, 185, 100, 40, "#FFF7ED", Amber, 3, "heater-element")); g.Add(Text("heater", 635, 211, 16, Amber, "middle"));
        g.Add(ArticleLine(760, 225, 685, 205, Amber, 3, "heater-circuit", true));
        g.Add(ArticleLine(910, 225, 685, 220, Amber, 3, "heater-circuit"));
        g.Add(ArticleLine(635, 225, 635, 235, Amber, 3, "thermal-coupling", true));
        g.Add(Text("热耦合", 730, 205, 16, Amber, "middle"));
        g.Add(Text("heater 仅热耦合超导开关，不串联主励磁回路", 600, 575, 18, Amber, "middle"));
        g.Add(Text("① 加热开关使其有电阻 → ② 励磁升流 → ③ 冷却闭合超导回路 → ④ 撤去电源", 600, 700, 19, Ink, "middle"));
    }

    private static void RenderMeterTransientResponse(XElement g)
    {
        DrawMeterTrialApparatus(g, 820, 445, 0.72);
        g.Add(ArticleLine(120, 610, 1080, 610, Ink, 4, "time-axis", true));
        g.Add(ArticleLine(120, 610, 120, 155, Ink, 4, "reading-axis", true));
        g.Add(Text("时间", 1080, 645, 18, Ink, "end"));
        g.Add(Text("指针示值", 105, 145, 18, Ink));
        g.Add(ArticleLine(120, 245, 1080, 245, Magenta, 3, "range-limit"));
        g.Add(Text("量程上限／机械限位区", 1060, 225, 18, Magenta, "end"));
        var points = new[] { (120d,610d), (205d,190d), (300d,350d), (400d,270d), (520d,310d), (680d,292d), (850d,298d), (1030d,296d) };
        for (var i = 0; i < points.Length - 1; i++)
            g.Add(ArticleLine(points[i].Item1, points[i].Item2, points[i + 1].Item1, points[i + 1].Item2, Blue, 5, "damped-response", i == points.Length - 2));
        foreach (var (x, y) in points.Skip(1))
            DrawArticleCircle(g, x, y, 7, Blue, 3, "damped-response");
        g.Add(ArticleLine(120, 610, 260, 120, Amber, 5, "danger-response", true));
        g.Add(Text("正常阻尼：首峰可能越过刻度线，随后回摆到稳定示值", 610, 690, 21, Blue, "middle"));
        g.Add(Text("快速逼近限位、反向偏转或异常：立即断开", 650, 165, 20, Amber, "middle"));
        g.Add(ArticleRect(820, 330, 240, 105, "#ECFDF5", Green, 3, "steady-window"));
        g.Add(Text("稳定示值用于量程判断", 940, 377, 20, Green, "middle"));
        g.Add(Text("动态峰值 ≠ 稳态测量值", 940, 410, 17, Green, "middle"));
    }

    private static void RenderMeterTrialDecision(XElement g)
    {
        DrawMeterTrialApparatus(g, 720, 45, 0.75);
        var boxes = new[]
        {
            (70d, 180d, "① 先预估", "低压教学电路\n核对额定值"),
            (330d, 180d, "② 选量程与极性", "不确定时先较大量程\n核对正负接线"),
            (590d, 180d, "③ 手保持在开关", "闭合后持续观察\n看趋势和限位"),
            (850d, 180d, "④ 判断并行动", "稳定后选量程\n或断开排查"),
        };
        foreach (var (x, y, title, body) in boxes)
        {
            g.Add(ArticleRect(x, y, 220, 165, "#F8FAFC", Blue, 3, "decision-step"));
            g.Add(Text(title, x + 110, y + 48, 20, Blue, "middle"));
            var lines = body.Split('\n');
            g.Add(Text(lines[0], x + 110, y + 92, 17, Ink, "middle"));
            g.Add(Text(lines[1], x + 110, y + 125, 17, Ink, "middle"));
            DrawArticleCircle(g, x + 110, y + 145, 5, Green, 2, "decision-step");
        }
        for (var x = 290d; x <= 810d; x += 260d)
            g.Add(ArticleLine(x, 262, x + 40, 262, Green, 4, "workflow-link", true));
        g.Add(ArticleRect(110, 430, 445, 170, "#FEF2F2", Magenta, 3, "abort-branch"));
        g.Add(Text("立即断开", 332, 477, 24, Magenta, "middle"));
        g.Add(Text("反向偏转｜快速逼近限位｜异味、发热、异常", 332, 525, 18, Ink, "middle"));
        g.Add(Text("断电后再检查接线", 332, 565, 18, Magenta, "middle"));
        g.Add(ArticleRect(645, 430, 445, 170, "#ECFDF5", Green, 3, "stable-branch"));
        g.Add(Text("继续观察到基本稳定", 868, 477, 23, Green, "middle"));
        g.Add(Text("未超量程 → 记录趋势并选择合适量程", 868, 525, 18, Ink, "middle"));
        g.Add(Text("不是机械性“一触即断”", 868, 565, 18, Green, "middle"));
        g.Add(ArticleLine(960, 345, 960, 430, Green, 4, "stable-decision", true));
        g.Add(ArticleLine(700, 345, 500, 430, Magenta, 4, "abort-decision", true));
        g.Add(Text("本图只适用于已评估的低压课堂实验", 600, 700, 19, Amber, "middle"));
    }

    private static void RenderMeterProtectionLayers(XElement g)
    {
        DrawMeterTrialApparatus(g, 850, 45, 0.75);
        var layers = new[]
        {
            (120d, "预防层", "预估电压/电流\n核对元件额定值", Blue, "prevention-layer"),
            (455d, "测量层", "量程、极性、接法\n观察动态与稳态", Green, "measurement-layer"),
            (790d, "保护层", "限流、保险、断路保护\n异常立即断电", Magenta, "protection-layer"),
        };
        foreach (var (x, title, body, color, role) in layers)
        {
            g.Add(ArticleRect(x, 190, 290, 310, "#FFFFFF", color, 4, role));
            g.Add(ArticleRect(x + 35, 225, 220, 65, "#F8FAFC", color, 3, role));
            g.Add(Text(title, x + 145, 267, 25, color, "middle"));
            var lines = body.Split('\n');
            g.Add(Text(lines[0], x + 145, 355, 19, Ink, "middle"));
            g.Add(Text(lines[1], x + 145, 405, 19, Ink, "middle"));
            g.Add(ArticleLine(x + 75, 450, x + 215, 450, color, 5, role, true));
        }
        g.Add(ArticleLine(410, 345, 455, 345, Amber, 4, "layer-link", true));
        g.Add(ArticleLine(745, 345, 790, 345, Amber, 4, "layer-link", true));
        g.Add(Text("试触服务于量程与连接核对，不能替代安全保护", 600, 575, 24, Ink, "middle"));
        g.Add(Text("重新接线、换量程或排故前：先断电", 600, 645, 22, Magenta, "middle"));
        g.Add(Text("超出仪表和电路额定环境时，整套装置必须重新评估", 600, 700, 18, Amber, "middle"));
    }

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

    /// <summary>
    /// A readable low-voltage apparatus anchor prevents the meter figures from
    /// degrading into ungrounded process cards or a curve with no operation.
    /// The two named connections are part of the semantic contract, not a
    /// decorative circuit motif.
    /// </summary>
    private static void DrawMeterTrialApparatus(XElement group, double x, double y, double scale)
    {
        double X(double value) => x + (value * scale);
        double Y(double value) => y + (value * scale);
        double S(double value) => value * scale;
        var apparatus = new XElement(Svg + "g");
        apparatus.Add(ArticleRect(X(0), Y(40), S(100), S(72), "#FEF3C7", Amber, S(3), "low-voltage-board"));
        apparatus.Add(Text("低压实验板", X(50), Y(68), (int)Math.Round(S(16)), Ink, "middle"));
        apparatus.Add(DrawArticleCircleElement(X(18), Y(92), S(7), Magenta, S(3), "low-voltage-terminal"));
        apparatus.Add(DrawArticleCircleElement(X(82), Y(92), S(7), Blue, S(3), "low-voltage-terminal"));
        apparatus.Add(ArticleRect(X(155), Y(15), S(112), S(120), "#F8FAFC", Ink, S(4), "meter-body"));
        apparatus.Add(DrawArticleCircleElement(X(211), Y(62), S(36), Blue, S(3), "meter-dial"));
        apparatus.Add(ArticleLine(X(211), Y(62), X(232), Y(43), Magenta, S(3), "meter-pointer"));
        apparatus.Add(DrawArticleCircleElement(X(178), Y(120), S(6), Magenta, S(3), "meter-terminal"));
        apparatus.Add(DrawArticleCircleElement(X(244), Y(120), S(6), Blue, S(3), "meter-terminal"));
        apparatus.Add(Text("电表", X(211), Y(155), (int)Math.Round(S(16)), Ink, "middle"));
        apparatus.Add(ArticleLine(X(100), Y(74), X(125), Y(74), Ink, S(3), "meter-wire"));
        var boardToSwitch = ArticleLine(X(125), Y(74), X(147), Y(86), Ink, S(3), "meter-wire");
        boardToSwitch.SetAttributeValue("data-article-connection", "board-meter-loop");
        apparatus.Add(boardToSwitch);
        apparatus.Add(ArticleLine(X(147), Y(86), X(178), Y(120), Ink, S(3), "switch-control"));
        var switchToMeter = ArticleLine(X(82), Y(92), X(130), Y(120), Ink, S(3), "meter-wire");
        switchToMeter.SetAttributeValue("data-article-connection", "board-meter-loop");
        apparatus.Add(switchToMeter);
        var returnWire = ArticleLine(X(130), Y(120), X(244), Y(120), Ink, S(3), "meter-wire");
        returnWire.SetAttributeValue("data-article-connection", "board-meter-loop");
        apparatus.Add(returnWire);
        apparatus.Add(ArticleLine(X(126), Y(90), X(147), Y(86), Amber, S(5), "switch-control"));
        apparatus.Add(Text("手控开关", X(130), Y(50), (int)Math.Round(S(14)), Amber, "middle"));
        apparatus.Add(ArticleLine(X(117), Y(128), X(130), Y(100), "#C084FC", S(7), "operator-hand"));
        apparatus.Add(ArticleLine(X(106), Y(142), X(118), Y(124), "#C084FC", S(6), "operator-hand"));
        var handToSwitch = ArticleLine(X(130), Y(100), X(145), Y(88), "#C084FC", S(5), "operator-hand");
        handToSwitch.SetAttributeValue("data-article-connection", "hand-switch-control");
        apparatus.Add(handToSwitch);
        group.Add(apparatus);
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

    private static void RenderLeverRockContact(XElement group)
    {
        group.Add(Text("翻转石块时，阻力是接触力的合力", 600, 132, 22, Ink, "middle"));
        group.Add(ArticleRect(90, 515, 980, 55, "#CBD5E1", "#64748B", 2, "ground"));
        group.Add(ArticleLine(220, 480, 760, 335, Amber, 14, "lever", false));
        group.Add(DrawArticleCircleElement(390, 435, 22, Ink, 4, "pivot"));
        group.Add(ArticleRect(590, 275, 245, 165, "#A8A29E", Ink, 4, "rock"));
        group.Add(Text("石块", 712, 365, 24, Ink, "middle"));
        group.Add(Text("支点 O", 390, 475, 19, Ink, "middle"));
        group.Add(Text("地面摩擦使石块翻转", 735, 470, 18, Muted, "middle"));
        var normal = ArticleLine(650, 430, 650, 510, Blue, 4, "normal-force", true);
        normal.SetAttributeValue("data-article-connection", "lever-rock-contact");
        group.Add(normal);
        group.Add(Text("压力 F压", 675, 500, 19, Blue));
        var friction = ArticleLine(650, 430, 555, 480, Magenta, 4, "friction-force", true);
        friction.SetAttributeValue("data-article-connection", "ground-rock-contact");
        group.Add(friction);
        group.Add(Text("f", 545, 485, 19, Magenta));
        var resultant = ArticleLine(650, 430, 530, 535, Green, 6, "resultant-force", true);
        resultant.SetAttributeValue("data-article-connection", "lever-rock-contact");
        group.Add(resultant);
        group.Add(Text("阻力合力 F阻：向左下", 470, 565, 20, Green));
        group.Add(DashedArticleLine(835, 350, 900, 285, Amber, 3, "rock-slide"));
        group.Add(DashedArticleLine(600, 285, 540, 235, Amber, 3, "rock-slide"));
        group.Add(Text("相对滑动 → 合力方向不固定为竖直向下", 600, 680, 19, Amber, "middle"));
    }

    private static void RenderLeverSeesawFriction(XElement group)
    {
        group.Add(Text("人体与杠杆是否滑动，会改变接触合力", 600, 132, 22, Ink, "middle"));
        group.Add(ArticleRect(60, 175, 520, 430, "#EFF6FF", "#93C5FD", 2, "sliding-state"));
        group.Add(ArticleRect(620, 175, 520, 430, "#FFF7ED", "#FDBA74", 2, "sliding-state"));
        group.Add(Text("不滑动：静摩擦", 320, 220, 24, Blue, "middle"));
        group.Add(Text("滑动：动摩擦", 880, 220, 24, Amber, "middle"));
        for (var left = 125d; left <= 685d; left += 560d)
        {
            group.Add(ArticleLine(left, 470, left + 380, 385, Ink, 13, "seesaw"));
            group.Add(DrawArticleCircleElement(left + 190, 428, 20, Ink, 4, "pivot"));
            group.Add(ArticleRect(left + 245, 300, 70, 90, "#FDE68A", Ink, 3, "person"));
            group.Add(Text("人体", left + 280, 340, 18, Ink, "middle"));
        }
        var gravity1 = ArticleLine(405, 300, 405, 410, Blue, 4, "gravity-component", true);
        gravity1.SetAttributeValue("data-article-connection", "friction-balances-component");
        group.Add(gravity1);
        group.Add(Text("重力分量 G2", 485, 345, 18, Blue, "middle"));
        var staticFriction = ArticleLine(405, 390, 405, 300, Green, 5, "static-friction", true);
        staticFriction.SetAttributeValue("data-article-connection", "friction-balances-component");
        group.Add(staticFriction);
        group.Add(Text("静摩擦 f1 = G2", 350, 270, 19, Green));
        group.Add(ArticleLine(945, 300, 945, 410, Blue, 4, "gravity-component", true));
        group.Add(Text("重力分量 G2", 1035, 430, 18, Blue, "middle"));
        group.Add(ArticleLine(945, 390, 905, 340, Amber, 4, "kinetic-friction", true));
        group.Add(Text("滑动摩擦 f2 < G2", 805, 270, 19, Amber, "middle"));
        group.Add(ArticleLine(905, 340, 1010, 280, Green, 5, "force-resultant", true));
        group.Add(ArticleLine(405, 390, 405, 285, Green, 5, "force-resultant", true));
        group.Add(Text("合力方向与大小随摩擦改变", 600, 675, 20, Magenta, "middle"));
        var contact = ArticleLine(390, 390, 420, 390, Ink, 4, "contact-line");
        contact.SetAttributeValue("data-article-connection", "person-seesaw-contact");
        group.Add(contact);
    }

    private static void RenderLeverTwoForceMember(XElement group)
    {
        group.Add(Text("杆的形状与约束决定动力方向", 600, 132, 22, Ink, "middle"));
        group.Add(ArticleRect(65, 175, 520, 430, "#EFF6FF", "#93C5FD", 2, "straight-member"));
        group.Add(ArticleRect(615, 175, 520, 430, "#FFF7ED", "#FDBA74", 2, "bent-member"));
        group.Add(Text("直杆 AC：二力平衡", 325, 220, 24, Blue, "middle"));
        group.Add(Text("弯曲撑杆：多约束合力", 875, 220, 24, Amber, "middle"));
        group.Add(ArticleLine(170, 490, 470, 300, Ink, 15, "straight-member"));
        group.Add(ArticleRect(135, 465, 50, 50, "#CBD5E1", Ink, 2, "support"));
        group.Add(ArticleRect(450, 275, 50, 50, "#CBD5E1", Ink, 2, "support"));
        group.Add(DrawArticleCircleElement(170, 490, 12, Ink, 3, "support"));
        group.Add(DrawArticleCircleElement(470, 300, 12, Ink, 3, "support"));
        var collinear = ArticleLine(125, 520, 510, 270, Green, 4, "two-force-line", true);
        collinear.SetAttributeValue("data-article-connection", "two-force-collinear");
        group.Add(collinear);
        group.Add(Text("FA、FC 共线等大反向", 325, 555, 18, Green, "middle"));
        group.Add(Text("沿杆方向", 325, 250, 18, Green, "middle"));
        var forceA = ArticleLine(170, 490, 120, 525, Magenta, 4, "force-along-member", true);
        forceA.SetAttributeValue("data-article-connection", "two-force-collinear");
        group.Add(forceA);
        var forceC = ArticleLine(470, 300, 520, 265, Magenta, 4, "force-along-member", true);
        forceC.SetAttributeValue("data-article-connection", "two-force-collinear");
        group.Add(forceC);
        group.Add(ArticleLine(710, 500, 820, 420, Ink, 15, "bent-member"));
        group.Add(ArticleLine(820, 420, 980, 470, Ink, 15, "bent-member"));
        group.Add(ArticleRect(680, 475, 50, 50, "#CBD5E1", Ink, 2, "support"));
        group.Add(ArticleRect(955, 445, 50, 50, "#CBD5E1", Ink, 2, "crane-arm"));
        group.Add(DrawArticleCircleElement(820, 420, 12, Ink, 3, "support"));
        var bentForce = ArticleLine(820, 420, 925, 350, Amber, 5, "constraint-force", true);
        bentForce.SetAttributeValue("data-article-connection", "bent-member-to-arm");
        group.Add(bentForce);
        group.Add(ArticleLine(820, 420, 760, 350, Amber, 5, "constraint-force", true));
        group.Add(Text("约束力合力", 1000, 365, 18, Amber, "middle"));
        group.Add(Text("不能仅凭杆身方向猜合力", 875, 560, 19, Amber, "middle"));
        group.Add(Text("二力杆结论不能直接推广到弯杆或多点约束", 600, 675, 19, Ink, "middle"));
    }

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

    private static XElement DrawArticleCircleElement(double centerX, double centerY, double radius, string color, double width, string role)
    {
        var circle = new XElement(Svg + "g");
        const int segments = 24;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            circle.Add(ArticleLine(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width,
                role));
        }
        return circle;
    }

    private static XElement DrawObserverEye(double centerX, double centerY, string role)
    {
        var eye = new XElement(Svg + "g");
        var outline = new[]
        {
            (centerX - 24, centerY), (centerX - 12, centerY - 11),
            (centerX, centerY - 15), (centerX + 12, centerY - 11),
            (centerX + 24, centerY), (centerX + 12, centerY + 11),
            (centerX, centerY + 15), (centerX - 12, centerY + 11),
            (centerX - 24, centerY),
        };
        for (var index = 0; index < outline.Length - 1; index++)
        {
            eye.Add(ArticleLine(
                outline[index].Item1,
                outline[index].Item2,
                outline[index + 1].Item1,
                outline[index + 1].Item2,
                Green,
                3,
                role));
        }
        eye.Add(DrawArticleCircleElement(centerX, centerY, 5, Ink, 2, role));
        return eye;
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

    private static void DrawArticleCircle(
        XElement group,
        double centerX,
        double centerY,
        double radius,
        string color,
        double width,
        string role)
    {
        const int segments = 48;
        for (var index = 0; index < segments; index++)
        {
            var start = 2 * Math.PI * index / segments;
            var end = 2 * Math.PI * (index + 1) / segments;
            group.Add(ArticleLine(
                centerX + radius * Math.Cos(start),
                centerY + radius * Math.Sin(start),
                centerX + radius * Math.Cos(end),
                centerY + radius * Math.Sin(end),
                color,
                width,
                role));
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

    private static XElement ArticleRect(
        double x,
        double y,
        double width,
        double height,
        string fill,
        string stroke,
        double strokeWidth,
        string role)
    {
        var rect = Rect(x, y, width, height, fill, stroke, strokeWidth);
        rect.SetAttributeValue("data-article-role", role);
        rect.SetAttributeValue("data-element-graphic", "true");
        return rect;
    }

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

    private static XElement ArticleLine(
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
        line.SetAttributeValue("data-article-role", role);
        return line;
    }

    private static XElement DashedArticleLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string color,
        double width,
        string role)
    {
        var line = ArticleLine(x1, y1, x2, y2, color, width, role);
        line.SetAttributeValue("stroke-dasharray", "7 6");
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
