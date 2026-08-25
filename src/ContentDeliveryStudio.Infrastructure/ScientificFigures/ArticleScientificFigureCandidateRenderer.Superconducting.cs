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
}
