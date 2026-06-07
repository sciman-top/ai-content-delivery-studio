using ImageSeriesStudio.Core.Providers;
using OpenAI.Responses;

namespace ImageSeriesStudio.Infrastructure.OpenAI;

#pragma warning disable OPENAI001 // SDK Responses APIs are adopted behind ADR 0009 parity gates.
public static class OpenAiSdkResponseOptionsFactory
{
    public static CreateResponseOptions CreateTextPlanningOptions(
        OpenAiProviderOptions options,
        PlanningRequest request)
    {
        return new CreateResponseOptions(
            options.TextPlanningModel,
            [ResponseItem.CreateUserMessageItem(OpenAiTextPlanningRequestMapper.BuildInput(request))])
        {
            Instructions = OpenAiTextPlanningRequestMapper.Instructions,
            StoredOutputEnabled = OpenAiRoutingDefaults.StoreRemoteStateByDefault,
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "image_series_plan",
                    OpenAiTextPlanningRequestMapper.CreatePlanSchemaBinaryData(),
                    jsonSchemaIsStrict: true),
            },
        };
    }
}
#pragma warning restore OPENAI001
