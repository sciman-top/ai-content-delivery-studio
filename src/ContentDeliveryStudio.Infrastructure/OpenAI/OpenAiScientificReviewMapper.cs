using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal static class OpenAiScientificReviewMapper
{
    public const int MaxOutputTokens = 3_000;

    public const string SemanticInstructions =
        "Review the rendered scientific structure only against the supplied approved claims, exact evidence, and approved figure specification. "
        + "Report only actionable element- or relation-level mismatches. Never add outside scientific knowledge or silently correct the approved authority.";

    public const string VisualInstructions =
        "Review the original full-resolution scientific PNG and every supplied typed crop. "
        + "Report only visible element-, relation-, formula-, or legend-level defects. Do not infer scientific facts beyond the supplied item metadata.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Dictionary<string, object?> CreateSemanticPayload(
        ScientificSemanticReviewRequest request,
        string model)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreatePayload(
            model,
            SemanticInstructions,
            [TextPart(BuildSemanticInput(request))]);
    }

    public static Dictionary<string, object?> CreateVisualPayload(
        ScientificVisualReviewRequest request,
        string model)
    {
        ArgumentNullException.ThrowIfNull(request);
        ScientificReviewExecutionPolicy.ValidateFullResolutionArtifact(
            request.FullResolutionOutput.PixelWidth,
            request.FullResolutionOutput.PixelHeight,
            request.FullResolutionOutput.Bytes.Length);
        ScientificReviewExecutionPolicy.ValidateCropPlan(request.RegionCrops.Count);
        foreach (var crop in request.RegionCrops)
        {
            ScientificReviewExecutionPolicy.ValidateCropBytes(crop.CropId, crop.Bytes.Length);
        }

        var content = new List<Dictionary<string, object?>>
        {
            TextPart(BuildVisualMetadata(request)),
            ImagePart(request.FullResolutionOutput.MimeType, request.FullResolutionOutput.Bytes),
        };
        content.AddRange(request.RegionCrops.Select(crop => ImagePart(crop.MimeType, crop.Bytes)));
        return CreatePayload(model, VisualInstructions, content);
    }

    public static ScientificProviderReviewResult Parse(
        JsonElement responseBody,
        IReadOnlySet<string> allowedResponsibleItemIds,
        string layerResponsibleItemId)
    {
        var traceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(responseBody);
        string outputText;
        try
        {
            outputText = OpenAiTextPlanningResponseMapper.ExtractOutputText(responseBody);
        }
        catch (InvalidOperationException)
        {
            return Blocked(traceId, "invalid-provider-output", layerResponsibleItemId);
        }

        OpenAiScientificReviewResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<OpenAiScientificReviewResponse>(outputText, JsonOptions);
        }
        catch (JsonException)
        {
            return Blocked(traceId, "invalid-provider-output", layerResponsibleItemId);
        }

        if (response?.Findings is null
            || !Enum.TryParse(response.Verdict, ignoreCase: true, out ScientificReviewVerdict verdict)
            || !Enum.IsDefined(verdict))
        {
            return Blocked(traceId, "invalid-provider-output", layerResponsibleItemId);
        }

        if (verdict == ScientificReviewVerdict.Fail && response.Findings.Count == 0)
        {
            return Blocked(traceId, "missing-provider-findings", layerResponsibleItemId);
        }

        var findings = new List<ScientificProviderFinding>();
        foreach (var item in response.Findings)
        {
            if (string.IsNullOrWhiteSpace(item.Code)
                || string.IsNullOrWhiteSpace(item.ResponsibleItemId)
                || string.IsNullOrWhiteSpace(item.Evidence)
                || !Enum.TryParse(item.Kind, ignoreCase: true, out ScientificProviderFindingKind kind)
                || !Enum.IsDefined(kind))
            {
                return Blocked(traceId, "invalid-provider-output", layerResponsibleItemId);
            }

            if (!allowedResponsibleItemIds.Contains(item.ResponsibleItemId))
            {
                return Blocked(traceId, "unknown-responsible-item", item.ResponsibleItemId);
            }

            findings.Add(new ScientificProviderFinding(
                item.Code,
                kind,
                item.ResponsibleItemId,
                item.Evidence));
        }

        return new ScientificProviderReviewResult(
            verdict,
            Array.AsReadOnly(findings.ToArray()),
            traceId);
    }

    private static string BuildSemanticInput(ScientificSemanticReviewRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            approvedClaims = request.ApprovedClaims.Select(claim => new
            {
                claim.ClaimId,
                category = claim.Category.ToString(),
                claim.NormalizedStatement,
                evidence = claim.Evidence.Select(item => new
                {
                    item.SourceBlockId,
                    item.QuotedText,
                    role = item.Role.ToString(),
                    item.Location.PageNumber,
                    item.Location.Section,
                    item.Location.CharacterRange,
                    item.Location.BoundingRegion,
                }),
            }),
            specification = new
            {
                request.Specification.SpecificationId,
                request.Specification.Version,
                request.Specification.Purpose,
                request.Specification.CentralMessage,
                request.Specification.Audience,
                request.Specification.IsSchematic,
                riskLevel = request.Specification.RiskLevel.ToString(),
                elements = request.Specification.Elements.Select(item => new
                {
                    item.ElementId,
                    kind = item.Kind.ToString(),
                    item.ScientificMeaning,
                    item.LabelOrFormula,
                    requirement = item.Requirement.ToString(),
                    item.IsCritical,
                }),
                relations = request.Specification.Relations.Select(item => new
                {
                    item.RelationId,
                    item.SourceElementId,
                    item.TargetElementId,
                    kind = item.Kind.ToString(),
                    direction = item.Direction.ToString(),
                    item.Label,
                    item.ScientificMeaning,
                    item.RepresentationConstraint,
                    requirement = item.Requirement.ToString(),
                    item.IsCritical,
                }),
            },
            renderSummary = request.RenderSummary,
        }, JsonOptions);
    }

    private static string BuildVisualMetadata(ScientificVisualReviewRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            fullResolution = new
            {
                request.FullResolutionOutput.Format,
                request.FullResolutionOutput.MimeType,
                request.FullResolutionOutput.Sha256,
                request.FullResolutionOutput.PixelWidth,
                request.FullResolutionOutput.PixelHeight,
            },
            typedCrops = request.RegionCrops.Select(crop => new
            {
                crop.CropId,
                kind = crop.Kind.ToString(),
                crop.ResponsibleItemId,
                crop.X,
                crop.Y,
                crop.Width,
                crop.Height,
            }),
            imageInputOrder = "fullResolution, then typedCrops in listed order",
        }, JsonOptions);
    }

    private static Dictionary<string, object?> CreatePayload(
        string model,
        string instructions,
        IReadOnlyList<Dictionary<string, object?>> content)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = model,
            ["instructions"] = instructions,
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = content,
                },
            },
            ["store"] = false,
            ["max_output_tokens"] = MaxOutputTokens,
            ["text"] = new Dictionary<string, object?>
            {
                ["format"] = new Dictionary<string, object?>
                {
                    ["type"] = "json_schema",
                    ["name"] = "scientific_figure_review",
                    ["strict"] = true,
                    ["schema"] = CreateSchema(),
                },
            },
        };
    }

    private static Dictionary<string, object?> TextPart(string text)
    {
        return new Dictionary<string, object?> { ["type"] = "input_text", ["text"] = text };
    }

    private static Dictionary<string, object?> ImagePart(string mimeType, byte[] bytes)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "input_image",
            ["image_url"] = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}",
            ["detail"] = "high",
        };
    }

    private static Dictionary<string, object?> CreateSchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "verdict", "findings" },
            ["properties"] = new Dictionary<string, object?>
            {
                ["verdict"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = Enum.GetNames<ScientificReviewVerdict>(),
                },
                ["findings"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new[] { "code", "kind", "responsibleItemId", "evidence" },
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["code"] = new Dictionary<string, object?> { ["type"] = "string" },
                            ["kind"] = new Dictionary<string, object?>
                            {
                                ["type"] = "string",
                                ["enum"] = Enum.GetNames<ScientificProviderFindingKind>(),
                            },
                            ["responsibleItemId"] = new Dictionary<string, object?> { ["type"] = "string" },
                            ["evidence"] = new Dictionary<string, object?> { ["type"] = "string" },
                        },
                    },
                },
            },
        };
    }

    private static ScientificProviderReviewResult Blocked(
        string traceId,
        string code,
        string responsibleItemId)
    {
        return new ScientificProviderReviewResult(
            ScientificReviewVerdict.Fail,
            [new ScientificProviderFinding(
                code,
                ScientificProviderFindingKind.ScientificMismatch,
                responsibleItemId,
                "Provider output did not satisfy the scientific review contract.")],
            traceId);
    }

    private sealed record OpenAiScientificReviewResponse(
        string Verdict,
        IReadOnlyList<OpenAiScientificReviewFinding> Findings);

    private sealed record OpenAiScientificReviewFinding(
        string Code,
        string Kind,
        string ResponsibleItemId,
        string Evidence);
}
