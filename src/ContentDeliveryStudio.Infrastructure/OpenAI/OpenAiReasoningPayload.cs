namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal static class OpenAiReasoningPayload
{
    public static Dictionary<string, object?> Create(string effort)
    {
        if (effort is not ("none" or "low" or "medium" or "high" or "xhigh" or "max"))
        {
            throw new ArgumentOutOfRangeException(nameof(effort), effort, "Unsupported reasoning effort.");
        }

        return new Dictionary<string, object?> { ["effort"] = effort };
    }
}
