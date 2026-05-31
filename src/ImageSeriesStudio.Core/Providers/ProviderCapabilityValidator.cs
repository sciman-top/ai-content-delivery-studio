namespace ImageSeriesStudio.Core.Providers;

public static class ProviderCapabilityValidator
{
    public static IReadOnlyList<string> ValidateTextPlanningProvider(ITextPlanningProvider provider)
    {
        return Validate(provider.Capabilities, requiredCapabilityName: "text planning", capabilities => capabilities.SupportsTextPlanning);
    }

    public static IReadOnlyList<string> ValidateImageGenerationProvider(IImageGenerationProvider provider)
    {
        return Validate(provider.Capabilities, requiredCapabilityName: "image generation", capabilities => capabilities.SupportsImageGeneration);
    }

    public static IReadOnlyList<string> ValidateVisionReviewProvider(IVisionReviewProvider provider)
    {
        return Validate(provider.Capabilities, requiredCapabilityName: "vision review", capabilities => capabilities.SupportsVisionReview);
    }

    private static IReadOnlyList<string> Validate(
        IProviderCapabilities capabilities,
        string requiredCapabilityName,
        Func<IProviderCapabilities, bool> supportsRequiredCapability)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(capabilities.ProviderId))
        {
            errors.Add("Provider id is required.");
        }

        if (string.IsNullOrWhiteSpace(capabilities.DisplayName))
        {
            errors.Add("Provider display name is required.");
        }

        if (!supportsRequiredCapability(capabilities))
        {
            errors.Add($"Provider must support {requiredCapabilityName}.");
        }

        if (capabilities.ModelIds.Count == 0)
        {
            errors.Add("Provider must declare at least one model id.");
        }

        if (capabilities.ModelIds.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Provider model ids cannot be blank.");
        }

        if (capabilities.ModelIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != capabilities.ModelIds.Count)
        {
            errors.Add("Provider model ids must be unique.");
        }

        return errors;
    }
}
