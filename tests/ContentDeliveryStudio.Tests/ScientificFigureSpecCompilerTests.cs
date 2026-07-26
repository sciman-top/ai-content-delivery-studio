using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureSpecCompilerTests
{
    [Fact]
    public void Compile_RequiresCurrentGateOneApproval()
    {
        var workflow = Workflow(approved: false);

        Assert.Throws<InvalidOperationException>(() =>
            new ScientificFigureSpecCompiler().Compile(workflow));
    }

    [Fact]
    public void Compile_MapsEveryCriticalItemToStableSpecificationIdentifier()
    {
        var workflow = Workflow(approved: true);

        var first = new ScientificFigureSpecCompiler().Compile(workflow);
        var second = new ScientificFigureSpecCompiler().Compile(workflow);

        Assert.Equal(first.PlanId, second.PlanId);
        Assert.Equal(
            workflow.Specification.SpecificationId,
            first.SpecificationId);
        Assert.NotEmpty(first.LayoutConstraints);
        Assert.NotEmpty(first.StyleTokens);
        Assert.Equal("svg", first.Export.Format);
        Assert.All(
            first.Elements.Where(element => element.IsCritical),
            element => Assert.Contains(
                workflow.Specification.Elements,
                item => item.ElementId == element.SourceSpecificationItemId));
        Assert.All(
            first.Connections.Where(connection => connection.IsCritical),
            connection => Assert.Contains(
                workflow.Specification.Relations,
                item => item.RelationId == connection.SourceSpecificationItemId));
    }

    [Fact]
    public void Compile_RejectsUnsupportedRenderStrategyBeforeRendering()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var unsupported = FigureElementSpec.Create(
            "element-unsupported",
            claim.NormalizedStatement,
            FigureElementKind.Entity,
            "Force",
            "provider-generated-layout",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
        var workflow = ScientificFigureWorkflow
            .Create(ScientificFigureTestFixture.CreateSpec(
                understanding,
                [unsupported],
                [],
                []))
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow);

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ScientificFigureSpecCompiler().Compile(workflow));

        Assert.Contains("unsupported-render-strategy", error.Message);
    }

    private static ScientificFigureWorkflow Workflow(bool approved)
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var force = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var acceleration = FigureElementSpec.Create(
            "element-acceleration",
            "Acceleration of the object.",
            FigureElementKind.Entity,
            "Acceleration",
            "deterministic-node",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
        var relation = FigureRelationSpec.Create(
            "relation-force-acceleration",
            force.ElementId,
            acceleration.ElementId,
            FigureRelationKind.Causes,
            FigureRelationDirection.Directed,
            "causes",
            "Net force causes acceleration.",
            "single directed arrow",
            FigureContentRequirement.Required,
            isCritical: true,
            ScientificFigureProvenance.FromEvidence(claim, evidence));
        var workflow = ScientificFigureWorkflow.Create(
            ScientificFigureTestFixture.CreateSpec(
                understanding,
                [force, acceleration],
                [relation],
                []));
        return approved
            ? workflow.ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow)
            : workflow;
    }
}
