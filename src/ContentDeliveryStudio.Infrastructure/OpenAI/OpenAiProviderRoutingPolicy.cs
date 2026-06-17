namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public enum OpenAiEndpointFamily
{
    Responses = 0,
    Images = 1,
}

public sealed record OpenAiRoutingDecision(
    OpenAiEndpointFamily EndpointFamily,
    string RelativePath,
    bool UseStructuredOutputs,
    bool Store);

public static class OpenAiProviderRoutingPolicy
{
    public static OpenAiRoutingDecision ForTextPlanning()
    {
        return new OpenAiRoutingDecision(
            OpenAiEndpointFamily.Responses,
            "responses",
            UseStructuredOutputs: true,
            Store: false);
    }

    public static OpenAiRoutingDecision ForVisionReview()
    {
        return new OpenAiRoutingDecision(
            OpenAiEndpointFamily.Responses,
            "responses",
            UseStructuredOutputs: true,
            Store: false);
    }

    public static OpenAiRoutingDecision ForImageGeneration()
    {
        return new OpenAiRoutingDecision(
            OpenAiEndpointFamily.Images,
            "images/generations",
            UseStructuredOutputs: false,
            Store: false);
    }

    public static OpenAiRoutingDecision ForStatefulImageGeneration()
    {
        return new OpenAiRoutingDecision(
            OpenAiEndpointFamily.Responses,
            "responses",
            UseStructuredOutputs: false,
            Store: true);
    }
}
