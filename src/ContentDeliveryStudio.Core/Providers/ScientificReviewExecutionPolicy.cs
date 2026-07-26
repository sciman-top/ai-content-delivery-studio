namespace ContentDeliveryStudio.Core.Providers;

public static class ScientificReviewExecutionPolicy
{
    public const int MaximumFullResolutionBytes = 25 * 1024 * 1024;
    public const int MaximumCropBytes = 5 * 1024 * 1024;
    public const int MaximumRegionCrops = 128;
    public const long MaximumFullResolutionPixels = 100_000_000;

    public static void ValidateFullResolutionArtifact(
        int width,
        int height,
        int byteCount)
    {
        if (width <= 0
            || height <= 0
            || (long)width * height > MaximumFullResolutionPixels)
        {
            throw new InvalidOperationException(
                "Scientific review full-resolution dimensions exceed the dispatch budget.");
        }

        if (byteCount <= 0 || byteCount > MaximumFullResolutionBytes)
        {
            throw new InvalidOperationException(
                "Scientific review full-resolution bytes exceed the dispatch budget.");
        }
    }

    public static void ValidateCropPlan(int cropCount)
    {
        if (cropCount <= 0 || cropCount > MaximumRegionCrops)
        {
            throw new InvalidOperationException(
                "Scientific review crop count is missing or exceeds the dispatch budget.");
        }
    }

    public static void ValidateCropBytes(string cropId, int byteCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cropId);
        if (byteCount <= 0 || byteCount > MaximumCropBytes)
        {
            throw new InvalidOperationException(
                $"Scientific review crop bytes are missing or oversized: {cropId}.");
        }
    }
}
