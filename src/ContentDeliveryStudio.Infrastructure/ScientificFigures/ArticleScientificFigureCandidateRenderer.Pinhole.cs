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
}
