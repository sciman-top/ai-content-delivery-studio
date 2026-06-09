using System.Text.Json;
using ImageSeriesStudio.Core.Providers;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

public static class OpenAiTextPlanningRequestMapper
{
    public const string Instructions =
        "You plan coherent image series. Return only valid JSON that matches the requested schema.";

    public static string BuildInput(PlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Goal: {request.Goal}",
                $"Audience: {request.Audience}",
                $"Item count: {Math.Max(1, request.ItemCount)}",
                $"Style brief: {request.StyleBrief}",
            ]);
    }

    public static Dictionary<string, object?> CreateResponsesPayload(
        OpenAiProviderOptions options,
        PlanningRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = options.TextPlanningModel,
            ["instructions"] = Instructions,
            ["input"] = BuildInput(request),
            ["store"] = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = CreateTextFormatPayload(),
            },
        };
    }

    public static BinaryData CreatePlanSchemaBinaryData()
    {
        return BinaryData.FromString(JsonSerializer.Serialize(CreatePlanSchema(), JsonOptions));
    }

    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    private static Dictionary<string, object?> CreateTextFormatPayload()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "json_schema",
            ["name"] = "image_series_plan",
            ["strict"] = true,
            ["schema"] = CreatePlanSchema(),
        };
    }

    private static Dictionary<string, object?> CreatePlanSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "summary", "items" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["summary"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["minLength"] = 1,
                },
                ["items"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new[] { "title", "brief", "promptDraft" },
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["title"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                            ["brief"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                            ["promptDraft"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["minLength"] = 1,
                            },
                        },
                    },
                },
            },
        };
    }
}
