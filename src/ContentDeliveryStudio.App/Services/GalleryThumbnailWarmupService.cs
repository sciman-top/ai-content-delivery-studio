namespace ContentDeliveryStudio.App.Services;

public class GalleryThumbnailWarmupService
{
    private const int DefaultWarmupCount = 24;

    public virtual Task WarmupAsync(IEnumerable<string> assetPaths, CancellationToken cancellationToken)
    {
        var warmupPaths = assetPaths.Take(DefaultWarmupCount).ToArray();
        if (warmupPaths.Length == 0)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            foreach (var assetPath in warmupPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    _ = GalleryThumbnailCache.GetOrCreate(assetPath);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Best-effort warmup only.
                }
            }
        }, cancellationToken);
    }
}
