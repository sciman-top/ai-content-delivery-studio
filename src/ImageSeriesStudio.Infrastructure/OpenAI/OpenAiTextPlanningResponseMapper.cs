using System.Text.Json;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

internal static class OpenAiTextPlanningResponseMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SeriesPlanResult ParseSeriesPlan(JsonElement root)
    {
        var providerTraceId = ExtractTraceId(root);
        var outputText = ExtractOutputText(root);
        var plan = JsonSerializer.Deserialize<OpenAiPlanResponse>(outputText, JsonOptions)
            ?? throw new InvalidOperationException("OpenAI text planning response was empty.");

        if (plan.Items.Count == 0)
        {
            throw new InvalidOperationException("OpenAI text planning response did not include any items.");
        }

        return new SeriesPlanResult(
            plan.Summary,
            plan.Items
                .Select(item => new SeriesPlanItem(item.Title, item.Brief, item.PromptDraft))
                .ToArray(),
            providerTraceId);
    }

    public static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextElement)
            && outputTextElement.ValueKind is JsonValueKind.String)
        {
            return outputTextElement.GetString()!;
        }

        if (root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind is JsonValueKind.Array)
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement)
                    || contentElement.ValueKind is not JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement)
                        && textElement.ValueKind is JsonValueKind.String)
                    {
                        return textElement.GetString()!;
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI text planning response did not include output text.");
    }

    public static string ExtractTraceId(JsonElement root)
    {
        return root.TryGetProperty("id", out var idElement) && idElement.ValueKind is JsonValueKind.String
            ? idElement.GetString()!
            : "openai-text-plan";
    }

    private sealed record OpenAiPlanResponse(string Summary, IReadOnlyList<OpenAiPlanItem> Items);

    private sealed record OpenAiPlanItem(string Title, string Brief, string PromptDraft);
}
