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
}
