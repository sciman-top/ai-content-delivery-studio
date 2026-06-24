namespace ContentDeliveryStudio.App.ViewModels;

internal enum MainWindowOperationLane
{
    ProjectRefresh,
    PlanLoad,
    DocumentSourceState,
    ProviderCenterRefresh,
    ProviderHealthCheck,
    GalleryWarmup,
}

internal readonly record struct MainWindowExclusiveOperationResult<T>(bool Executed, T? Value)
{
    public static MainWindowExclusiveOperationResult<T> Skipped()
    {
        return new MainWindowExclusiveOperationResult<T>(false, default);
    }

    public static MainWindowExclusiveOperationResult<T> Started(T value)
    {
        return new MainWindowExclusiveOperationResult<T>(true, value);
    }
}

internal sealed class MainWindowOperationGate
{
    private static readonly MainWindowOperationLane[] ExclusiveConflictLanes =
    [
        MainWindowOperationLane.ProjectRefresh,
        MainWindowOperationLane.PlanLoad,
        MainWindowOperationLane.DocumentSourceState,
        MainWindowOperationLane.GalleryWarmup,
    ];

    private readonly object _sync = new();
    private readonly Action<bool> _exclusiveStateChanged;
    private readonly Dictionary<MainWindowOperationLane, LatestWinsRegistration> _latestWins = [];
    private long _nextOperationId;
    private bool _exclusiveBusy;
    private Task _backgroundTask = Task.CompletedTask;

    public MainWindowOperationGate(Action<bool> exclusiveStateChanged)
    {
        _exclusiveStateChanged = exclusiveStateChanged;
    }

    public bool IsExclusiveBusy
    {
        get
        {
            lock (_sync)
            {
                return _exclusiveBusy;
            }
        }
    }

    public Task BackgroundTask
    {
        get
        {
            lock (_sync)
            {
                return _backgroundTask;
            }
        }
    }

    public void RunBackgroundLatestWins(MainWindowOperationLane lane, Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var task = RunLatestWinsCoreAsync(lane, operation, suppressFailures: true);
        var observedTask = ObserveBackgroundTaskAsync(task);

        lock (_sync)
        {
            _backgroundTask = observedTask;
        }
    }

    public Task RunLatestWinsAsync(MainWindowOperationLane lane, Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return RunLatestWinsCoreAsync(lane, operation, suppressFailures: false);
    }

    public Task<T?> RunLatestWinsAsync<T>(MainWindowOperationLane lane, Func<CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return RunLatestWinsCoreAsync<T>(lane, operation);
    }

    public async Task<bool> RunExclusiveAsync(Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!TryEnterExclusive())
        {
            return false;
        }

        CancelLatestWins(ExclusiveConflictLanes);

        using var cancellation = new CancellationTokenSource();
        try
        {
            await operation(cancellation.Token);
            return true;
        }
        finally
        {
            ExitExclusive();
        }
    }

    public async Task<MainWindowExclusiveOperationResult<T>> RunExclusiveAsync<T>(Func<CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!TryEnterExclusive())
        {
            return MainWindowExclusiveOperationResult<T>.Skipped();
        }

        CancelLatestWins(ExclusiveConflictLanes);

        using var cancellation = new CancellationTokenSource();
        try
        {
            var value = await operation(cancellation.Token);
            return MainWindowExclusiveOperationResult<T>.Started(value);
        }
        finally
        {
            ExitExclusive();
        }
    }

    private async Task RunLatestWinsCoreAsync(
        MainWindowOperationLane lane,
        Func<CancellationToken, Task> operation,
        bool suppressFailures)
    {
        LatestWinsRegistration? previous = null;
        LatestWinsRegistration current;

        lock (_sync)
        {
            if (_latestWins.TryGetValue(lane, out var active))
            {
                previous = active;
            }

            current = new LatestWinsRegistration(++_nextOperationId, new CancellationTokenSource());
            _latestWins[lane] = current;
        }

        previous?.Cancel();

        try
        {
            await operation(current.Cancellation.Token);
        }
        catch (OperationCanceledException) when (current.Cancellation.IsCancellationRequested)
        {
            // latest-wins cancellation is expected
        }
        catch when (suppressFailures)
        {
            // background latest-wins work is best-effort only
        }
        finally
        {
            lock (_sync)
            {
                if (_latestWins.TryGetValue(lane, out var active)
                    && active.OperationId == current.OperationId)
                {
                    _latestWins.Remove(lane);
                }
            }

            current.Dispose();
        }
    }

    private async Task<T?> RunLatestWinsCoreAsync<T>(
        MainWindowOperationLane lane,
        Func<CancellationToken, Task<T>> operation)
    {
        LatestWinsRegistration? previous = null;
        LatestWinsRegistration current;

        lock (_sync)
        {
            if (_latestWins.TryGetValue(lane, out var active))
            {
                previous = active;
            }

            current = new LatestWinsRegistration(++_nextOperationId, new CancellationTokenSource());
            _latestWins[lane] = current;
        }

        previous?.Cancel();

        try
        {
            return await operation(current.Cancellation.Token);
        }
        catch (OperationCanceledException) when (current.Cancellation.IsCancellationRequested)
        {
            return default;
        }
        finally
        {
            lock (_sync)
            {
                if (_latestWins.TryGetValue(lane, out var active)
                    && active.OperationId == current.OperationId)
                {
                    _latestWins.Remove(lane);
                }
            }

            current.Dispose();
        }
    }

    private bool TryEnterExclusive()
    {
        lock (_sync)
        {
            if (_exclusiveBusy)
            {
                return false;
            }

            _exclusiveBusy = true;
        }

        _exclusiveStateChanged(true);
        return true;
    }

    private void CancelLatestWins(IEnumerable<MainWindowOperationLane> lanes)
    {
        ArgumentNullException.ThrowIfNull(lanes);

        LatestWinsRegistration[] registrations;
        lock (_sync)
        {
            registrations = lanes
                .Distinct()
                .Where(lane => _latestWins.TryGetValue(lane, out _))
                .Select(lane => _latestWins[lane])
                .ToArray();
        }

        foreach (var registration in registrations)
        {
            registration.Cancel();
        }
    }

    private void ExitExclusive()
    {
        lock (_sync)
        {
            _exclusiveBusy = false;
        }

        _exclusiveStateChanged(false);
    }

    private static async Task ObserveBackgroundTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // background latest-wins work is best-effort only
        }
    }

    private sealed class LatestWinsRegistration(long operationId, CancellationTokenSource cancellation)
    {
        public long OperationId { get; } = operationId;

        public CancellationTokenSource Cancellation { get; } = cancellation;

        public void Cancel()
        {
            Cancellation.Cancel();
        }

        public void Dispose()
        {
            Cancellation.Dispose();
        }
    }
}
