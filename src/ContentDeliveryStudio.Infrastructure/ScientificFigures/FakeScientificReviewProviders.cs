using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class FakeScientificSemanticReviewProvider
    : IScientificSemanticReviewProvider
{
    private readonly ScientificProviderReviewResult _result;
    private readonly Exception? _failure;

    public FakeScientificSemanticReviewProvider(
        ScientificProviderReviewResult? result = null,
        Exception? failure = null)
    {
        _result = result ?? FakeScientificReviewResults.Pass("fake-semantic-review");
        _failure = failure;
    }

    public int InvocationCount { get; private set; }

    public ScientificSemanticReviewRequest? LastRequest { get; private set; }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificSemanticReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        InvocationCount++;
        LastRequest = request;
        return _failure is null
            ? Task.FromResult(_result)
            : Task.FromException<ScientificProviderReviewResult>(_failure);
    }
}

public sealed class FakeScientificVisualReviewProvider
    : IScientificVisualReviewProvider
{
    private readonly ScientificProviderReviewResult _result;
    private readonly Exception? _failure;

    public FakeScientificVisualReviewProvider(
        ScientificProviderReviewResult? result = null,
        Exception? failure = null)
    {
        _result = result ?? FakeScientificReviewResults.Pass("fake-visual-review");
        _failure = failure;
    }

    public int InvocationCount { get; private set; }

    public ScientificVisualReviewRequest? LastRequest { get; private set; }

    public Task<ScientificProviderReviewResult> ReviewAsync(
        ScientificVisualReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        InvocationCount++;
        LastRequest = request;
        return _failure is null
            ? Task.FromResult(_result)
            : Task.FromException<ScientificProviderReviewResult>(_failure);
    }
}

public static class FakeScientificReviewResults
{
    public static ScientificProviderReviewResult Pass(string traceId)
    {
        return new ScientificProviderReviewResult(
            ScientificReviewVerdict.Pass,
            [],
            traceId,
            ScientificProviderReviewOrigin.FakeProvider);
    }

    public static ScientificProviderReviewResult WithVerdict(
        ScientificReviewVerdict verdict,
        string traceId,
        IReadOnlyList<ScientificProviderFinding>? findings = null)
    {
        return new ScientificProviderReviewResult(
            verdict,
            findings ?? [],
            traceId,
            ScientificProviderReviewOrigin.FakeProvider);
    }
}
