namespace ImageSeriesStudio.Core.Providers;

public static class ProviderCapabilityValidator
{
    public static IReadOnlyList<string> ValidateTextPlanningProvider(ITextPlanningProvider provider)
    {
        return Validate(provider.Capabilities, requiredCapabilityName: "text planning", capabilities => capabilities.SupportsTextPlanning);
    }

    public static IReadOnlyList<string> ValidateImageGenerationProvider(IImageGenerationProvider provider)
    {
        var errors = Validate(
            provider.Capabilities,
            requiredCapabilityName: "image generation",
            capabilities => capabilities.SupportsImageGeneration).ToList();

        ValidateImageOutputSettings(provider.Capabilities, errors);
        return errors;
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

    private static void ValidateImageOutputSettings(IProviderCapabilities capabilities, List<string> errors)
    {
        if (capabilities.SupportedSizes.Count == 0)
        {
            errors.Add("Image generation provider must declare at least one supported output size.");
        }

        if (capabilities.SupportedSizes.Any(size => size.Width <= 0 || size.Height <= 0))
        {
            errors.Add("Image generation provider output sizes must be positive.");
        }

        ValidateStringSet(
            capabilities.SupportedQualities,
            "Image generation provider must declare at least one supported quality.",
            "Image generation provider supported qualities cannot be blank.",
            "Image generation provider supported qualities must be unique.",
            errors);
        ValidateStringSet(
            capabilities.SupportedOutputFormats,
            "Image generation provider must declare at least one supported output format.",
            "Image generation provider supported output formats cannot be blank.",
            "Image generation provider supported output formats must be unique.",
            errors);
        ValidateStringSet(
            capabilities.SupportedBackgroundModes,
            "Image generation provider must declare at least one supported background mode.",
            "Image generation provider supported background modes cannot be blank.",
            "Image generation provider supported background modes must be unique.",
            errors);

        if (capabilities.CostHints.Count == 0)
        {
            errors.Add("Image generation provider must declare at least one cost hint.");
        }
    }

    private static void ValidateStringSet(
        IReadOnlyList<string> values,
        string missingMessage,
        string blankMessage,
        string duplicateMessage,
        List<string> errors)
    {
        if (values.Count == 0)
        {
            errors.Add(missingMessage);
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(blankMessage);
        }

        if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Count)
        {
            errors.Add(duplicateMessage);
        }
    }
}
