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
}
