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
}
