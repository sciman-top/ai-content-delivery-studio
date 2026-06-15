using System.Text.Json;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Core.Styles;

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

    public static DocumentIllustrationPlanningResult ParseDocumentIllustrationPlan(JsonElement root)
    {
        var providerTraceId = ExtractTraceId(root);
        var outputText = ExtractOutputText(root);
        var response = JsonSerializer.Deserialize<OpenAiDocumentIllustrationResponse>(outputText, JsonOptions)
            ?? throw new InvalidOperationException("OpenAI document illustration planning response was empty.");

        var timestamp = DateTimeOffset.UtcNow;
        var brief = DocumentBrief.Create(
            Guid.NewGuid(),
            ParseDocumentSourceKind(response.Brief.SourceKind),
            response.Brief.SourceDisplayName,
            response.Brief.Title,
            ParseDocumentFamily(response.Brief.DocumentFamily),
            response.Brief.Audience,
            response.Brief.Sections,
            response.Brief.KeyClaims,
            response.Brief.VisualOpportunities,
            response.Brief.KnownConstraints,
            ParseStrictnessLevel(response.Brief.StrictnessLevel),
            timestamp);

        var targets = response.Plan.Targets
            .Select(target => IllustrationTarget.Create(
                brief.Id,
                target.Title,
                target.DocumentLocation,
                ParseIllustrationPurpose(target.Purpose),
                target.MustShow,
                target.MustNotShow,
                target.SourceEvidence,
                target.SuggestedImageTypePresetId,
                target.SuggestedReviewRubricTemplateId,
                ParseImageTextPolicy(target.TextPolicy),
                target.StrictnessNotes,
                timestamp))
            .ToArray();

        var plan = IllustrationPlan.Create(
            brief.ProjectId,
            brief.Id,
            response.Plan.Summary,
            targets,
            response.Plan.CoverageNotes,
            response.Plan.RiskNotes,
            timestamp);

        return new DocumentIllustrationPlanningResult(brief, plan, providerTraceId);
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

    private static DocumentSourceKind ParseDocumentSourceKind(string value)
    {
        return value.Trim() switch
        {
            "Docx" => DocumentSourceKind.Docx,
            "Pdf" => DocumentSourceKind.Pdf,
            "Markdown" => DocumentSourceKind.Markdown,
            "Text" => DocumentSourceKind.Text,
            _ => DocumentSourceKind.Paste,
        };
    }

    private static DocumentFamily ParseDocumentFamily(string value)
    {
        return value.Trim() switch
        {
            "Editorial" => DocumentFamily.Editorial,
            "ScholarlyDraft" => DocumentFamily.ScholarlyDraft,
            _ => DocumentFamily.Educational,
        };
    }

    private static IllustrationStrictnessLevel ParseStrictnessLevel(string value)
    {
        return value.Trim() switch
        {
            "Editorial" => IllustrationStrictnessLevel.Editorial,
            "ScholarlyDraft" => IllustrationStrictnessLevel.ScholarlyDraft,
            _ => IllustrationStrictnessLevel.Educational,
        };
    }

    private static IllustrationPurpose ParseIllustrationPurpose(string value)
    {
        return value.Trim() switch
        {
            "Cover" => IllustrationPurpose.Cover,
            "InlineIllustration" => IllustrationPurpose.InlineIllustration,
            "MechanismDiagram" => IllustrationPurpose.MechanismDiagram,
            "Timeline" => IllustrationPurpose.Timeline,
            "Comparison" => IllustrationPurpose.Comparison,
            "GraphicalAbstract" => IllustrationPurpose.GraphicalAbstract,
            "BackgroundPlate" => IllustrationPurpose.BackgroundPlate,
            _ => IllustrationPurpose.ConceptDiagram,
        };
    }

    private static ImageTextPolicy ParseImageTextPolicy(string value)
    {
        return value.Trim() switch
        {
            "ImageModelOnly" => ImageTextPolicy.ImageModelOnly,
            "Hybrid" => ImageTextPolicy.Hybrid,
            _ => ImageTextPolicy.DeterministicPostRender,
        };
    }

    private sealed record OpenAiPlanResponse(string Summary, IReadOnlyList<OpenAiPlanItem> Items);

    private sealed record OpenAiPlanItem(string Title, string Brief, string PromptDraft);

    private sealed record OpenAiDocumentIllustrationResponse(
        OpenAiDocumentIllustrationBrief Brief,
        OpenAiDocumentIllustrationPlan Plan);

    private sealed record OpenAiDocumentIllustrationBrief(
        string SourceKind,
        string SourceDisplayName,
        string Title,
        string DocumentFamily,
        string Audience,
        IReadOnlyList<string> Sections,
        IReadOnlyList<string> KeyClaims,
        IReadOnlyList<string> VisualOpportunities,
        IReadOnlyList<string> KnownConstraints,
        string StrictnessLevel);

    private sealed record OpenAiDocumentIllustrationPlan(
        string Summary,
        IReadOnlyList<string> CoverageNotes,
        IReadOnlyList<string> RiskNotes,
        IReadOnlyList<OpenAiDocumentIllustrationTarget> Targets);

    private sealed record OpenAiDocumentIllustrationTarget(
        string Title,
        string DocumentLocation,
        string Purpose,
        IReadOnlyList<string> MustShow,
        IReadOnlyList<string> MustNotShow,
        IReadOnlyList<string> SourceEvidence,
        string SuggestedImageTypePresetId,
        string SuggestedReviewRubricTemplateId,
        string TextPolicy,
        IReadOnlyList<string> StrictnessNotes);
}
