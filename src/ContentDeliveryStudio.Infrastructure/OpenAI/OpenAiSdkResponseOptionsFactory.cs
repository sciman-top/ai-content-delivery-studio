using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;
using OpenAI.Responses;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

#pragma warning disable OPENAI001 // SDK Responses APIs are adopted behind ADR 0009 parity gates.
public static class OpenAiSdkResponseOptionsFactory
{
    public static CreateResponseOptions CreateTextPlanningOptions(
        OpenAiProviderOptions options,
        PlanningRequest request)
    {
        var route = OpenAiTaskModelRouter.ForPlanning(options, request);
        return new CreateResponseOptions(
            route.Model,
            [ResponseItem.CreateUserMessageItem(OpenAiTextPlanningRequestMapper.BuildInput(request))])
        {
            Instructions = OpenAiTextPlanningRequestMapper.Instructions,
            StoredOutputEnabled = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            ReasoningOptions = OpenAiReasoningOptionsFactory.Create(route.ReasoningEffort),
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "image_series_plan",
                    OpenAiTextPlanningRequestMapper.CreatePlanSchemaBinaryData(),
                    jsonSchemaIsStrict: true),
            },
        };
    }

    public static CreateResponseOptions CreateDocumentIllustrationPlanningOptions(
        OpenAiProviderOptions options,
        DocumentIllustrationPlanningRequest request)
    {
        var route = OpenAiTaskModelRouter.ForDocumentPlanning(options, request);
        return new CreateResponseOptions(
            route.Model,
            [ResponseItem.CreateUserMessageItem(OpenAiTextPlanningRequestMapper.BuildDocumentIllustrationInput(request))])
        {
            Instructions = "You plan document-grounded illustration targets. Return only valid JSON that matches the requested schema. Do not fabricate evidence, experimental results, or unsupported factual claims.",
            StoredOutputEnabled = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            ReasoningOptions = OpenAiReasoningOptionsFactory.Create(route.ReasoningEffort),
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "document_illustration_plan",
                    OpenAiTextPlanningRequestMapper.CreateDocumentIllustrationPlanSchemaBinaryData(),
                    jsonSchemaIsStrict: true),
            },
        };
    }

    public static CreateResponseOptions CreateScientificUnderstandingOptions(
        OpenAiProviderOptions options,
        ScientificUnderstandingChunkRequest request)
    {
        var route = OpenAiTaskModelRouter.ForScientificUnderstanding(options, request);
        return new CreateResponseOptions(
            route.Model,
            [ResponseItem.CreateUserMessageItem(OpenAiScientificUnderstandingMapper.BuildInput(request))])
        {
            Instructions = OpenAiScientificUnderstandingMapper.Instructions,
            StoredOutputEnabled = false,
            ReasoningOptions = OpenAiReasoningOptionsFactory.Create(route.ReasoningEffort),
            MaxOutputTokenCount = OpenAiScientificUnderstandingMapper.MaxOutputTokens,
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "scientific_understanding_chunk",
                    OpenAiScientificUnderstandingMapper.CreateSchemaBinaryData(),
                    jsonSchemaIsStrict: true),
            },
        };
    }
}

internal static class OpenAiReasoningOptionsFactory
{
    public static ResponseReasoningOptions Create(string effort) => new()
    {
        ReasoningEffortLevel = new ResponseReasoningEffortLevel(effort),
    };
}
#pragma warning restore OPENAI001
