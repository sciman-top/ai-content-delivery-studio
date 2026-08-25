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
}
