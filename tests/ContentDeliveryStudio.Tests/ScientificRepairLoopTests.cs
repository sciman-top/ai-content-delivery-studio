using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificRepairLoopTests
{
    [Fact]
    public void RecordAutomaticAttempt_AllowsThreeRoundsAndRejectsFourth()
    {
        var service = new ScientificRepairApplicationService();
        var action = service.RouteFinding(
            "layout-overlap",
            "element-force",
            "Labels overlap.",
            ScientificRepairLayer.LayoutStyle);
        var plan = ScientificRepairPlan.Create([action]);
        var state = ScientificRepairLoopState.Start();

        state = service.RecordAutomaticAttempt(plan, state);
        state = service.RecordAutomaticAttempt(plan, state);
        state = service.RecordAutomaticAttempt(plan, state);

        Assert.Equal(3, state.CompletedAutomaticAttempts);
        var error = Assert.Throws<InvalidOperationException>(() =>
            service.RecordAutomaticAttempt(plan, state));
        Assert.Contains("fourth", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("human", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordAutomaticAttempt_RejectsScientificActionEvenIfModeIsForged()
    {
        var forged = new ScientificRepairAction(
            "formula-drift",
            "element-formula",
            "Formula changed.",
            ScientificRepairLayer.FigureSpecification,
            ScientificRepairExecutionMode.Automatic);
        var plan = ScientificRepairPlan.Create([forged]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            ScientificRepairLoopState.Start().RecordAutomaticAttempt(plan));

        Assert.Contains("Only layout/style", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordAutomaticAttempt_RequiresAnAutomaticAction()
    {
        var plan = ScientificRepairPlan.Create(
            [new ScientificRepairAction(
                "provider-failure",
                "Semantic",
                "Provider unavailable.",
                ScientificRepairLayer.ScientificUnderstanding,
                ScientificRepairExecutionMode.HumanRequired)]);

        Assert.Throws<InvalidOperationException>(() =>
            ScientificRepairLoopState.Start().RecordAutomaticAttempt(plan));
    }
}
