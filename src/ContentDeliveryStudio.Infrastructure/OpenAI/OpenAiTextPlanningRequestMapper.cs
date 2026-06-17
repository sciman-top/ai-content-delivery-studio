using System.Text.Json;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

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

    public static string BuildDocumentIllustrationInput(DocumentIllustrationPlanningRequest request)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Title: {request.Title}",
                $"Audience: {request.Audience}",
                $"Document family: {request.DocumentFamily}",
                $"Strictness level: {request.StrictnessLevel}",
                $"Sections: {FormatList(request.Sections)}",
                $"Key claims: {FormatList(request.KeyClaims)}",
                $"Known constraints: {FormatList(request.KnownConstraints)}",
                "Source text:",
                request.SourceText,
            ]);
    }

    public static Dictionary<string, object?> CreateDocumentIllustrationResponsesPayload(
        OpenAiProviderOptions options,
        DocumentIllustrationPlanningRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = options.TextPlanningModel,
            ["instructions"] = "You plan document-grounded illustration targets. Return only valid JSON that matches the requested schema. Do not fabricate evidence, experimental results, or unsupported factual claims.",
            ["input"] = BuildDocumentIllustrationInput(request),
            ["store"] = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = new Dictionary<string, object?>
                {
                    ["type"] = "json_schema",
                    ["name"] = "document_illustration_plan",
                    ["strict"] = true,
                    ["schema"] = CreateDocumentIllustrationPlanSchema(),
                },
            },
        };
    }

    public static BinaryData CreatePlanSchemaBinaryData()
    {
        return BinaryData.FromString(JsonSerializer.Serialize(CreatePlanSchema(), JsonOptions));
    }

    public static BinaryData CreateDocumentIllustrationPlanSchemaBinaryData()
    {
        return BinaryData.FromString(JsonSerializer.Serialize(CreateDocumentIllustrationPlanSchema(), JsonOptions));
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

    private static Dictionary<string, object?> CreateDocumentIllustrationPlanSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "brief", "plan" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["brief"] = new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[]
                    {
                        "sourceKind",
                        "sourceDisplayName",
                        "title",
                        "documentFamily",
                        "audience",
                        "sections",
                        "keyClaims",
                        "visualOpportunities",
                        "knownConstraints",
                        "strictnessLevel",
                    },
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["sourceKind"] = CreateEnumStringSchema("Paste", "Text", "Markdown"),
                        ["sourceDisplayName"] = CreateStringSchema(),
                        ["title"] = CreateStringSchema(),
                        ["documentFamily"] = CreateEnumStringSchema("Editorial", "Educational", "ScholarlyDraft"),
                        ["audience"] = CreateStringSchema(),
                        ["sections"] = CreateStringArraySchema(),
                        ["keyClaims"] = CreateStringArraySchema(),
                        ["visualOpportunities"] = CreateStringArraySchema(),
                        ["knownConstraints"] = CreateStringArraySchema(),
                        ["strictnessLevel"] = CreateEnumStringSchema("Editorial", "Educational", "ScholarlyDraft"),
                    },
                },
                ["plan"] = new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new[] { "summary", "coverageNotes", "riskNotes", "targets" },
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["summary"] = CreateStringSchema(),
                        ["coverageNotes"] = CreateStringArraySchema(),
                        ["riskNotes"] = CreateStringArraySchema(),
                        ["targets"] = new Dictionary<string, object?>
                        {
                            ["type"] = "array",
                            ["minItems"] = 1,
                            ["items"] = new Dictionary<string, object?>
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = false,
                                ["required"] = new[]
                                {
                                    "title",
                                    "documentLocation",
                                    "purpose",
                                    "mustShow",
                                    "mustNotShow",
                                    "sourceEvidence",
                                    "suggestedImageTypePresetId",
                                    "suggestedReviewRubricTemplateId",
                                    "textPolicy",
                                    "strictnessNotes",
                                },
                                ["properties"] = new Dictionary<string, object?>
                                {
                                    ["title"] = CreateStringSchema(),
                                    ["documentLocation"] = CreateStringSchema(),
                                    ["purpose"] = CreateEnumStringSchema(
                                        "Cover",
                                        "InlineIllustration",
                                        "ConceptDiagram",
                                        "MechanismDiagram",
                                        "Timeline",
                                        "Comparison",
                                        "GraphicalAbstract",
                                        "BackgroundPlate"),
                                    ["mustShow"] = CreateStringArraySchema(),
                                    ["mustNotShow"] = CreateStringArraySchema(),
                                    ["sourceEvidence"] = CreateStringArraySchema(minItems: 1),
                                    ["suggestedImageTypePresetId"] = CreateStringSchema(),
                                    ["suggestedReviewRubricTemplateId"] = CreateStringSchema(),
                                    ["textPolicy"] = CreateEnumStringSchema("ImageModelOnly", "DeterministicPostRender", "Hybrid"),
                                    ["strictnessNotes"] = CreateStringArraySchema(),
                                },
                            },
                        },
                    },
                },
            },
        };
    }

    private static Dictionary<string, object?> CreateStringSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["minLength"] = 1,
        };
    }

    private static Dictionary<string, object?> CreateEnumStringSchema(params string[] values)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["enum"] = values,
        };
    }

    private static Dictionary<string, object?> CreateStringArraySchema(int minItems = 0)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "array",
            ["minItems"] = minItems,
            ["items"] = CreateStringSchema(),
        };
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        var normalized = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .ToArray();

        return normalized.Length == 0
            ? "None"
            : string.Join("; ", normalized);
    }
}
