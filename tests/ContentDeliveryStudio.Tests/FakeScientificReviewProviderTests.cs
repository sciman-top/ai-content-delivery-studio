using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class FakeScientificReviewProviderTests
{
    [Fact]
    public async Task ReviewAsync_PassesOnlyWhenBothIndependentProvidersPass()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var semantic = new FakeScientificSemanticReviewProvider();
        var visual = new FakeScientificVisualReviewProvider();

        var decision = await ExecuteAsync(fixture, semantic, visual);

        Assert.True(decision.CanProceedToGate2);
        Assert.Empty(decision.Blockers);
        Assert.Same(fixture.SemanticRequest, semantic.LastRequest);
        Assert.Same(fixture.VisualRequest, visual.LastRequest);
        Assert.Equal(1, semantic.InvocationCount);
        Assert.Equal(1, visual.InvocationCount);
    }

    [Theory]
    [InlineData(ScientificReviewVerdict.Uncertain, "provider-uncertain")]
    [InlineData(ScientificReviewVerdict.Fail, "provider-failed-review")]
    public async Task ReviewAsync_BlocksNonPassVerdicts(
        ScientificReviewVerdict verdict,
        string expectedCode)
    {
        var fixture = ScientificReviewTestFixture.Create();
        var semantic = new FakeScientificSemanticReviewProvider(
            FakeScientificReviewResults.WithVerdict(verdict, "semantic-trace"));

        var decision = await ExecuteAsync(
            fixture,
            semantic,
            new FakeScientificVisualReviewProvider());

        AssertBlocked(decision, ScientificReviewLayer.Semantic, expectedCode);
    }

    [Fact]
    public async Task ReviewAsync_BlocksMissingElementFindingEvenWithPassVerdict()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var finding = new ScientificProviderFinding(
            "missing-critical-element",
            ScientificProviderFindingKind.MissingElement,
            "element-force",
            "The required element is absent from the full-resolution output.");
        var visual = new FakeScientificVisualReviewProvider(
            FakeScientificReviewResults.WithVerdict(
                ScientificReviewVerdict.Pass,
                "visual-trace",
                [finding]));

        var decision = await ExecuteAsync(
            fixture,
            new FakeScientificSemanticReviewProvider(),
            visual);

        AssertBlocked(decision, ScientificReviewLayer.Visual, finding.Code);
    }

    [Fact]
    public async Task ReviewAsync_BlocksInvalidProviderOutput()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var invalid = new ScientificProviderReviewResult(
            ScientificReviewVerdict.Pass,
            [],
            ProviderTraceId: string.Empty);

        var decision = await ExecuteAsync(
            fixture,
            new FakeScientificSemanticReviewProvider(invalid),
            new FakeScientificVisualReviewProvider());

        AssertBlocked(
            decision,
            ScientificReviewLayer.Semantic,
            "invalid-provider-output");
    }

    [Fact]
    public async Task ReviewAsync_BlocksProviderFailureWithoutHidingOtherLayer()
    {
        var fixture = ScientificReviewTestFixture.Create();
        var semantic = new FakeScientificSemanticReviewProvider(
            failure: new InvalidOperationException("simulated provider outage"));

        var decision = await ExecuteAsync(
            fixture,
            semantic,
            new FakeScientificVisualReviewProvider());

        AssertBlocked(decision, ScientificReviewLayer.Semantic, "provider-failure");
        Assert.Equal(1, semantic.InvocationCount);
    }

    private static Task<ScientificMachineReviewDecision> ExecuteAsync(
        ScientificReviewTestFixture fixture,
        IScientificSemanticReviewProvider semantic,
        IScientificVisualReviewProvider visual)
    {
        return new ScientificReviewExecutionService(semantic, visual).ReviewAsync(
            fixture.SemanticRequest,
            fixture.VisualRequest,
            CancellationToken.None);
    }

    private static void AssertBlocked(
        ScientificMachineReviewDecision decision,
        ScientificReviewLayer layer,
        string code)
    {
        Assert.False(decision.CanProceedToGate2);
        Assert.Contains(
            decision.Blockers,
            blocker => blocker.Layer == layer && blocker.Code == code);
    }
}
