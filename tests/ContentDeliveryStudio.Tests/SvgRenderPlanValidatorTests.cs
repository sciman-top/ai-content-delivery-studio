using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class SvgRenderPlanValidatorTests
{
    [Fact]
    public void Validate_RejectsConnectionWithMissingEndpoint()
    {
        var plan = Plan(
            elements: [Element("render-element-a", "element-a")],
            connections:
            [
                new SvgRenderConnection(
                    "render-relation-a-b",
                    "relation-a-b",
                    "render-element-a",
                    "render-element-missing",
                    FigureRelationKind.Causes,
                    FigureRelationDirection.Directed,
                    "causes",
                    "single directed arrow",
                    IsCritical: true,
                    ScientificProvenanceKind.ClaimEvidence),
            ]);

        var result = SvgRenderPlanValidator.Validate(plan);

        Assert.Contains(
            result.Errors,
            error => error.Code == "missing-connection-endpoint");
    }

    [Fact]
    public void Validate_RejectsUnknownApprovedSpecificationItem()
    {
        var plan = Plan(
            elements: [Element("render-element-a", "element-unapproved")],
            connections: []);

        var result = SvgRenderPlanValidator.Validate(
            plan,
            approvedElementIds: ["element-approved"],
            approvedRelationIds: []);

        Assert.Contains(
            result.Errors,
            error => error.Code == "unapproved-specification-item");
    }

    [Fact]
    public void Validate_RequiresExactFormulaContent()
    {
        var formula = new SvgRenderElement(
            "render-formula",
            "element-formula",
            FigureElementKind.Formula,
            ScientificMeaning: "Newton's second law.",
            ExactContent: null,
            RenderStrategy: "deterministic-formula",
            LayerId: "scientific-content",
            IsCritical: true,
            ScientificProvenanceKind.ScientificConvention);

        var result = SvgRenderPlanValidator.Validate(Plan([formula], []));

        Assert.Contains(
            result.Errors,
            error => error.Code == "missing-exact-content");
    }

    private static SvgRenderElement Element(string id, string sourceId)
    {
        return new SvgRenderElement(
            id,
            sourceId,
            FigureElementKind.Entity,
            "Scientific entity.",
            "Entity",
            "deterministic-node",
            "scientific-content",
            IsCritical: true,
            ScientificProvenanceKind.ClaimEvidence);
    }

    private static SvgRenderPlan Plan(
        IReadOnlyList<SvgRenderElement> elements,
        IReadOnlyList<SvgRenderConnection> connections)
    {
        return SvgRenderPlan.Create(
            "render-plan:test:v1",
            Guid.NewGuid(),
            specificationVersion: 1,
            new SvgCanvas(1200, 800, "0 0 1200 800"),
            [new SvgRenderLayer("scientific-content", 0, IsScientific: true)],
            elements,
            connections,
            new SvgAccessibilityMetadata("Test figure", "Test description"),
            new SvgExportSettings("svg", IncludeMetadata: true));
    }
}
