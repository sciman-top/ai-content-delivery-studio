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
}
