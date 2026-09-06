namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public enum OpenAiExecutionQualityTier
{
    Deep = 0,
    Balanced = 1,
    Fast = 2,
}

public sealed record OpenAiExecutionSlotSchedulerOptions(
    int DeepConcurrency = 1,
    int BalancedConcurrency = 2,
    int FastConcurrency = 2,
    int MaxQueuedRequestsPerTier = 8)
{
    public int GetConcurrency(OpenAiExecutionQualityTier qualityTier) => qualityTier switch
    {
        OpenAiExecutionQualityTier.Deep => DeepConcurrency,
        OpenAiExecutionQualityTier.Balanced => BalancedConcurrency,
        OpenAiExecutionQualityTier.Fast => FastConcurrency,
        _ => throw new ArgumentOutOfRangeException(nameof(qualityTier), qualityTier, "Unknown quality tier."),
    };

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (DeepConcurrency <= 0)
        {
            errors.Add("Deep concurrency must be positive.");
        }

        if (BalancedConcurrency <= 0)
        {
            errors.Add("Balanced concurrency must be positive.");
        }

        if (FastConcurrency <= 0)
        {
            errors.Add("Fast concurrency must be positive.");
        }

        if (MaxQueuedRequestsPerTier < 0)
        {
            errors.Add("Max queued requests per tier cannot be negative.");
        }

        return errors;
    }
}

public interface IOpenAiExecutionSlotScheduler
{
    Task<TResult> ExecuteAsync<TResult>(
        OpenAiExecutionQualityTier qualityTier,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken);
}

public sealed class OpenAiExecutionQueueFullException : InvalidOperationException
{
    public OpenAiExecutionQueueFullException(OpenAiExecutionQualityTier qualityTier)
        : base($"The OpenAI {qualityTier.ToString().ToLowerInvariant()} execution queue is full.")
    {
        QualityTier = qualityTier;
    }

    public OpenAiExecutionQualityTier QualityTier { get; }
}

/// <summary>
/// Reserves one shared model-execution slot for the complete request, including
/// a bounded model-health probe and any same-quality failover attempt.
/// </summary>
public sealed class OpenAiExecutionSlotScheduler : IOpenAiExecutionSlotScheduler
{
    public static OpenAiExecutionSlotSchedulerOptions DefaultOptions { get; } = new();

    private readonly OpenAiExecutionSlotSchedulerOptions _options;
    private readonly SlotBucket _deep;
    private readonly SlotBucket _balanced;
    private readonly SlotBucket _fast;

    public OpenAiExecutionSlotScheduler(OpenAiExecutionSlotSchedulerOptions? options = null)
    {
        _options = options ?? DefaultOptions;
        var validationErrors = _options.Validate();
        if (validationErrors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", validationErrors), nameof(options));
        }

        _deep = new SlotBucket(_options.DeepConcurrency, _options.MaxQueuedRequestsPerTier);
        _balanced = new SlotBucket(_options.BalancedConcurrency, _options.MaxQueuedRequestsPerTier);
        _fast = new SlotBucket(_options.FastConcurrency, _options.MaxQueuedRequestsPerTier);
    }

    public int TotalConcurrency =>
        _options.DeepConcurrency + _options.BalancedConcurrency + _options.FastConcurrency;

    public Task<TResult> ExecuteAsync<TResult>(
        OpenAiExecutionQualityTier qualityTier,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteCoreAsync(GetBucket(qualityTier), qualityTier, operation, cancellationToken);
    }

    private static async Task<TResult> ExecuteCoreAsync<TResult>(
        SlotBucket bucket,
        OpenAiExecutionQualityTier qualityTier,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var lease = await bucket.AcquireAsync(qualityTier, cancellationToken);
        return await operation();
    }

    private SlotBucket GetBucket(OpenAiExecutionQualityTier qualityTier) => qualityTier switch
    {
        OpenAiExecutionQualityTier.Deep => _deep,
        OpenAiExecutionQualityTier.Balanced => _balanced,
        OpenAiExecutionQualityTier.Fast => _fast,
        _ => throw new ArgumentOutOfRangeException(nameof(qualityTier), qualityTier, "Unknown quality tier."),
    };

    private sealed class SlotBucket
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxQueuedRequests;
        private int _queuedRequests;

        public SlotBucket(int concurrency, int maxQueuedRequests)
        {
            _semaphore = new SemaphoreSlim(concurrency, concurrency);
            _maxQueuedRequests = maxQueuedRequests;
        }

        public async ValueTask<IAsyncDisposable> AcquireAsync(
            OpenAiExecutionQualityTier qualityTier,
            CancellationToken cancellationToken)
        {
            if (_semaphore.Wait(0))
            {
                return new Lease(_semaphore);
            }

            if (Interlocked.Increment(ref _queuedRequests) > _maxQueuedRequests)
            {
                Interlocked.Decrement(ref _queuedRequests);
                throw new OpenAiExecutionQueueFullException(qualityTier);
            }

            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                return new Lease(_semaphore);
            }
            finally
            {
                Interlocked.Decrement(ref _queuedRequests);
            }
        }

        private sealed class Lease(SemaphoreSlim semaphore) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                semaphore.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}

/// <summary>
/// Keeps exactly one active model preset set per gateway and credential scope.
/// The active value is one of the registered family-only preset sets.
/// individual slots may select different tiers only inside that one set.
/// </summary>
public interface IOpenAiActivePresetSetState
{
    string? GetActivePresetSet(OpenAiProviderOptions options);

    void MarkActivePresetSet(OpenAiProviderOptions options, string presetSet);

    OpenAiActivePresetSetSnapshot GetSnapshot(OpenAiProviderOptions options);

    bool TrySwitchActivePresetSet(
        OpenAiProviderOptions options,
        long expectedVersion,
        string presetSet);
}

public sealed record OpenAiActivePresetSetSnapshot(string? PresetSet, long Version);

public sealed class OpenAiActivePresetSetState : IOpenAiActivePresetSetState
{
    private sealed record Entry(string PresetSet, long Version);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _activePresetSets = new(StringComparer.Ordinal);

    public string? GetActivePresetSet(OpenAiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return GetSnapshot(options).PresetSet;
    }

    public void MarkActivePresetSet(OpenAiProviderOptions options, string presetSet)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (TextProviderModelPresetSets.Names.Contains(presetSet, StringComparer.Ordinal))
        {
            var key = CreateKey(options);
            _activePresetSets.AddOrUpdate(
                key,
                _ => new Entry(presetSet, Version: 1),
                (_, current) => new Entry(presetSet, checked(current.Version + 1)));
        }
    }

    public OpenAiActivePresetSetSnapshot GetSnapshot(OpenAiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _activePresetSets.TryGetValue(CreateKey(options), out var entry)
            ? new OpenAiActivePresetSetSnapshot(entry.PresetSet, entry.Version)
            : new OpenAiActivePresetSetSnapshot(null, Version: 0);
    }

    public bool TrySwitchActivePresetSet(
        OpenAiProviderOptions options,
        long expectedVersion,
        string presetSet)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!TextProviderModelPresetSets.Names.Contains(presetSet, StringComparer.Ordinal)
            || expectedVersion < 0)
        {
            return false;
        }

        var key = CreateKey(options);
        while (true)
        {
            if (!_activePresetSets.TryGetValue(key, out var current))
            {
                return expectedVersion == 0
                    && _activePresetSets.TryAdd(key, new Entry(presetSet, Version: 1));
            }

            if (current.Version != expectedVersion)
            {
                return false;
            }

            var next = new Entry(presetSet, checked(current.Version + 1));
            if (_activePresetSets.TryUpdate(key, next, current))
            {
                return true;
            }
        }
    }

    private static string CreateKey(OpenAiProviderOptions options) =>
        $"{options.BaseUri.AbsoluteUri}|{options.ApiKeySecretName}";
}
