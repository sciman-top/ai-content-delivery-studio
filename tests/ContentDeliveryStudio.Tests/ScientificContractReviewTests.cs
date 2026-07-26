using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificContractReviewTests
{
    [Fact]
    public void Review_PassesWhenSpecificationPlanSvgAndExportsAreEquivalent()
    {
        var fixture = ScientificContractReviewFixture.Create(advisoryScore: 0.25);

        var report = new ScientificContractReviewer().Review(fixture.Request);

        Assert.True(report.Passed);
        Assert.Empty(report.HardFailures);
        Assert.Equal(0.25, report.AdvisoryScore);
    }

    [Fact]
    public void Review_FindingsAlwaysIdentifyInvariantItemEvidenceAndRepairLayer()
    {
        var fixture = ScientificContractReviewFixture.Create();
        var mutatedPlan = fixture.CopyPlan(
            elements: fixture.Plan.Elements
                .Where(item => item.SourceSpecificationItemId != "element-formula")
                .ToArray());

        var report = new ScientificContractReviewer().Review(
            fixture.Request with { RenderPlan = mutatedPlan });

        Assert.False(report.Passed);
        Assert.NotEmpty(report.HardFailures);
        Assert.All(
            report.HardFailures,
            finding =>
            {
                Assert.False(string.IsNullOrWhiteSpace(finding.Code));
                Assert.True(Enum.IsDefined(finding.Invariant));
                Assert.False(string.IsNullOrWhiteSpace(finding.ResponsibleItemId));
                Assert.False(string.IsNullOrWhiteSpace(finding.Evidence));
                Assert.True(Enum.IsDefined(finding.RepairLayer));
            });
    }
}

internal sealed record ScientificContractReviewFixture(
    ScientificFigureSpec Specification,
    SvgRenderPlan Plan,
    ScientificSvgArtifact Svg,
    ScientificFigureExportBundle Exports,
    ScientificContractReviewRequest Request)
{
    public static ScientificContractReviewFixture Create(double advisoryScore = 1)
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var provenance = ScientificFigureProvenance.FromEvidence(claim, evidence);
        var force = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var acceleration = FigureElementSpec.Create(
            "element-acceleration",
            "Acceleration of the object.",
            FigureElementKind.Entity,
            "Acceleration",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var formula = FigureElementSpec.Create(
            "element-formula",
            "Exact formula relating force, mass, and acceleration.",
            FigureElementKind.Formula,
            "F = m * a",
            "deterministic-formula",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var value = FigureElementSpec.Create(
            "element-value",
            "Approved acceleration magnitude.",
            FigureElementKind.Value,
            "9.81",
            "deterministic-value",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var unit = FigureElementSpec.Create(
            "element-unit",
            "Approved acceleration unit.",
            FigureElementKind.Unit,
            "m/s^2",
            "deterministic-unit",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var relation = FigureRelationSpec.Create(
            "relation-force-acceleration",
            force.ElementId,
            acceleration.ElementId,
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "Net force causes acceleration for constant mass.",
            "single directed arrow",
            FigureContentRequirement.Required,
            isCritical: true,
            provenance);
        var specification = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [force, acceleration, formula, value, unit],
            [relation],
            []);
        var workflow = ScientificFigureWorkflow.Create(specification)
            .ApproveGate1("reviewer", "Approved scientific contract.", DateTimeOffset.UtcNow);
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var svg = new DeterministicSvgRenderer().Render(plan);
        var exports = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, Width: 1200, Height: 800));
        var request = new ScientificContractReviewRequest(
            specification,
            plan,
            svg,
            exports,
            advisoryScore);
        return new ScientificContractReviewFixture(specification, plan, svg, exports, request);
    }

    public SvgRenderPlan CopyPlan(
        IReadOnlyList<SvgRenderElement>? elements = null,
        IReadOnlyList<SvgRenderConnection>? connections = null)
    {
        return SvgRenderPlan.Create(
            Plan.PlanId,
            Plan.SpecificationId,
            Plan.SpecificationVersion,
            Plan.Canvas,
            Plan.Layers,
            elements ?? Plan.Elements,
            connections ?? Plan.Connections,
            Plan.Accessibility,
            Plan.Export,
            Plan.LayoutConstraints,
            Plan.StyleTokens);
    }
}
