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
}
