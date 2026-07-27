using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

internal static class OpenAiScientificUnderstandingMapper
{
    public const int MaxBlocks = 8;
    public const int MaxCharacters = 12_000;
    public const int MaxOutputTokens = 4_000;

    public const string Instructions =
        "Analyze only the supplied scientific source blocks for the bounded figure objective. "
        + "Return strict schema-conforming JSON. Preserve every sourceBlockId and quote exact source text. "
        + "For a claim directly supported by an exact supplied quote, use status Accepted and evidenceValidationState Validated; "
        + "Validated means the quote-to-block anchor is mechanically verified, not that a human Gate 1 decision has occurred. "
        + "Every item in limitations must use category Limitation; keep constraints in claims with category Constraint. "
        + "Do not invent evidence, resolve conflicts silently, or convert visual suggestions into scientific claims.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string BuildInput(ScientificUnderstandingChunkRequest request)
    {
        ValidateRequest(request);
        return JsonSerializer.Serialize(new
        {
            objective = request.Objective,
            chunkIndex = request.ChunkIndex,
            chunkCount = request.ChunkCount,
            sourceSha256 = request.Extraction.SourceSha256,
            blocks = request.Blocks.Select(block => new
            {
                sourceBlockId = block.BlockId,
                kind = block.Kind.ToString(),
                originalText = block.OriginalText,
                location = new
                {
                    pageNumber = block.Location.PageNumber,
                    section = block.Location.Section,
                    characterRange = block.Location.CharacterRange is null
                        ? null
                        : new
                        {
                            startOffset = block.Location.CharacterRange.StartOffset,
                            endOffset = block.Location.CharacterRange.EndOffset,
                        },
                    boundingRegion = block.Location.BoundingRegion is null
                        ? null
                        : new
                        {
                            x = block.Location.BoundingRegion.X,
                            y = block.Location.BoundingRegion.Y,
                            width = block.Location.BoundingRegion.Width,
                            height = block.Location.BoundingRegion.Height,
                        },
                },
            }),
        }, JsonOptions);
    }

    public static BinaryData CreateSchemaBinaryData()
    {
        using var document = JsonDocument.Parse(Schema);
        return BinaryData.FromString(document.RootElement.GetRawText());
    }

    public static ScientificUnderstandingChunkResult Parse(
        JsonElement responseBody,
        ScientificUnderstandingChunkRequest request)
    {
        var traceId = OpenAiTextPlanningResponseMapper.ExtractTraceId(responseBody);
        string outputText;
        try
        {
            outputText = OpenAiTextPlanningResponseMapper.ExtractOutputText(responseBody);
        }
        catch (InvalidOperationException)
        {
            return Blocked(traceId, "missing-output-text");
        }

        OpenAiScientificUnderstandingResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<OpenAiScientificUnderstandingResponse>(
                outputText,
                JsonOptions);
        }
        catch (JsonException)
        {
            return Blocked(traceId, "invalid-json");
        }

        if (response?.Claims is null
            || response.Terms is null
            || response.Limitations is null
            || response.Conflicts is null)
        {
            return Blocked(traceId, "invalid-structured-output");
        }

        var blocks = request.Blocks.ToDictionary(block => block.BlockId, StringComparer.Ordinal);
        var blockingCodes = new List<string>();
        var claims = response.Claims
            .Select(item => MapClaim(item, blocks, blockingCodes, requiredCategory: null))
            .Concat(response.Limitations.Select(item => MapClaim(
                item,
                blocks,
                blockingCodes,
                ScientificClaimCategory.Limitation)))
            .Where(item => item is not null)
            .Cast<ScientificUnderstandingClaimDraft>()
            .ToArray();
        if (claims.Length == 0)
        {
            blockingCodes.Add("no-scientific-claims");
        }
        var terms = response.Terms
            .Select(item => MapTerm(item, blocks, blockingCodes))
            .Where(item => item is not null)
            .Cast<ScientificUnderstandingTermDraft>()
            .ToArray();
        var conflicts = response.Conflicts.Select(item =>
        {
            if (string.IsNullOrWhiteSpace(item.ConflictId)
                || string.IsNullOrWhiteSpace(item.FirstMergeKey)
                || string.IsNullOrWhiteSpace(item.SecondMergeKey)
                || string.IsNullOrWhiteSpace(item.Description))
            {
                blockingCodes.Add("invalid-explicit-conflict");
            }
            else
            {
                blockingCodes.Add($"explicit-conflict:{item.ConflictId}");
            }

            return new ScientificUnderstandingConflictDraft(
                item.ConflictId ?? "invalid-conflict",
                item.FirstMergeKey ?? "unknown",
                item.SecondMergeKey ?? "unknown",
                item.Description ?? "Provider returned an invalid conflict.");
        }).ToArray();
        foreach (var group in claims.GroupBy(claim => claim.MergeKey, StringComparer.Ordinal))
        {
            if (group.Select(claim => claim.NormalizedStatement)
                .Distinct(StringComparer.Ordinal)
                .Skip(1)
                .Any())
            {
                blockingCodes.Add($"conflicting-claims:{group.Key}");
            }
        }

        var proposal = MapProposal(response.FigureProposal, blocks, blockingCodes);
        return new ScientificUnderstandingChunkResult(claims, traceId)
        {
            Terms = terms,
            Conflicts = conflicts,
            FigureProposal = proposal,
            BlockingCodes = Array.AsReadOnly(blockingCodes.Distinct(StringComparer.Ordinal).ToArray()),
        };
    }

    private static ScientificUnderstandingClaimDraft? MapClaim(
        OpenAiScientificClaim item,
        IReadOnlyDictionary<string, ScientificSourceBlock> blocks,
        ICollection<string> blockingCodes,
        ScientificClaimCategory? requiredCategory)
    {
        if (string.IsNullOrWhiteSpace(item.SourceBlockId)
            || !blocks.TryGetValue(item.SourceBlockId, out var block))
        {
            blockingCodes.Add($"unknown-source-block:{item.SourceBlockId ?? "<null>"}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(item.QuotedText)
            || block.OriginalText?.Contains(item.QuotedText, StringComparison.Ordinal) != true)
        {
            blockingCodes.Add($"quote-not-in-source:{item.SourceBlockId}");
            return null;
        }

        if (!TryEnum(item.Category, out ScientificClaimCategory category)
            || !TryEnum(item.Status, out ScientificClaimStatus status)
            || !TryEnum(item.EvidenceRole, out ClaimEvidenceRole evidenceRole)
            || !TryEnum(item.EvidenceValidationState, out EvidenceValidationState validationState)
            || !double.IsFinite(item.Confidence)
            || item.Confidence is < 0 or > 1
            || string.IsNullOrWhiteSpace(item.MergeKey)
            || string.IsNullOrWhiteSpace(item.NormalizedStatement)
            || string.IsNullOrWhiteSpace(item.SourceWording))
        {
            blockingCodes.Add($"unsupported-claim:{item.MergeKey ?? item.SourceBlockId}");
            return null;
        }

        if (requiredCategory is not null && category != requiredCategory)
        {
            blockingCodes.Add($"unsupported-limitation-category:{item.MergeKey}");
        }

        if (status != ScientificClaimStatus.Accepted
            || validationState != EvidenceValidationState.Validated
            || evidenceRole == ClaimEvidenceRole.Contradiction)
        {
            blockingCodes.Add($"claim-not-accepted:{item.MergeKey}");
        }

        return new ScientificUnderstandingClaimDraft(
            item.MergeKey,
            category,
            item.NormalizedStatement,
            item.SourceWording,
            item.Confidence,
            status,
            item.SourceBlockId,
            item.QuotedText,
            evidenceRole,
            validationState);
    }

    private static ScientificUnderstandingTermDraft? MapTerm(
        OpenAiScientificTerm item,
        IReadOnlyDictionary<string, ScientificSourceBlock> blocks,
        ICollection<string> blockingCodes)
    {
        if (string.IsNullOrWhiteSpace(item.SourceBlockId)
            || !blocks.ContainsKey(item.SourceBlockId)
            || string.IsNullOrWhiteSpace(item.TermId)
            || string.IsNullOrWhiteSpace(item.CanonicalTerm)
            || string.IsNullOrWhiteSpace(item.Definition)
            || item.Aliases is null)
        {
            blockingCodes.Add($"invalid-term:{item.TermId ?? "<null>"}");
            return null;
        }

        return new ScientificUnderstandingTermDraft(
            item.TermId,
            item.CanonicalTerm,
            item.Definition,
            item.Aliases,
            item.SourceBlockId);
    }

    private static ScientificFigureProposalDraft? MapProposal(
        OpenAiScientificFigureProposal? proposal,
        IReadOnlyDictionary<string, ScientificSourceBlock> blocks,
        ICollection<string> blockingCodes)
    {
        if (proposal is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(proposal.CentralMessage)
            || proposal.Elements is null
            || proposal.Relations is null)
        {
            blockingCodes.Add("invalid-figure-proposal");
            return null;
        }

        var elements = proposal.Elements.Select(item =>
        {
            ValidateProposalAuthority(item.ProposalId, item.SourceBlockIds, blocks, blockingCodes);
            return new ScientificFigureElementProposalDraft(
                item.ProposalId,
                item.Meaning,
                item.SourceBlockIds ?? []);
        }).ToArray();
        var elementIds = elements.Select(item => item.ProposalId)
            .ToHashSet(StringComparer.Ordinal);
        var relations = proposal.Relations.Select(item =>
        {
            ValidateProposalAuthority(item.ProposalId, item.SourceBlockIds, blocks, blockingCodes);
            if (!elementIds.Contains(item.SourceProposalId)
                || !elementIds.Contains(item.TargetProposalId)
                || string.Equals(item.SourceProposalId, item.TargetProposalId, StringComparison.Ordinal)
                || item.RelationClass is not ("causal" or "directional" or "comparative" or "associative-non-causal"))
            {
                blockingCodes.Add($"unsupported-proposal-relation:{item.ProposalId}");
            }

            return new ScientificFigureRelationProposalDraft(
                item.ProposalId,
                item.SourceProposalId,
                item.TargetProposalId,
                item.RelationClass,
                item.SourceBlockIds ?? []);
        }).ToArray();
        return new ScientificFigureProposalDraft(proposal.CentralMessage, elements, relations);
    }

    private static void ValidateProposalAuthority(
        string? proposalId,
        IReadOnlyList<string>? sourceBlockIds,
        IReadOnlyDictionary<string, ScientificSourceBlock> blocks,
        ICollection<string> blockingCodes)
    {
        if (string.IsNullOrWhiteSpace(proposalId)
            || sourceBlockIds is null
            || sourceBlockIds.Count == 0
            || sourceBlockIds.Any(sourceBlockId => !blocks.ContainsKey(sourceBlockId)))
        {
            blockingCodes.Add($"unsupported-proposal:{proposalId ?? "<null>"}");
        }
    }

    private static ScientificUnderstandingChunkResult Blocked(string traceId, string code)
    {
        return new ScientificUnderstandingChunkResult([], traceId)
        {
            BlockingCodes = [code],
        };
    }

    private static bool TryEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: false, out result) && Enum.IsDefined(result);
    }

    private static void ValidateRequest(ScientificUnderstandingChunkRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Blocks is null
            || request.Blocks.Count is < 1 or > MaxBlocks
            || request.Blocks.Sum(block => block.OriginalText?.Length ?? 0) > MaxCharacters
            || string.IsNullOrWhiteSpace(request.Objective))
        {
            throw new ArgumentException(
                $"Scientific understanding chunks require 1-{MaxBlocks} blocks and at most {MaxCharacters} source characters.",
                nameof(request));
        }

        if (request.ChunkIndex < 0
            || request.ChunkCount < 1
            || request.ChunkIndex >= request.ChunkCount
            || request.Blocks.Any(block => !request.Extraction.Blocks.Contains(block)))
        {
            throw new ArgumentException(
                "Scientific understanding chunk authority or index is invalid.",
                nameof(request));
        }
    }

    private sealed record OpenAiScientificUnderstandingResponse(
        IReadOnlyList<OpenAiScientificTerm> Terms,
        IReadOnlyList<OpenAiScientificClaim> Claims,
        IReadOnlyList<OpenAiScientificClaim> Limitations,
        IReadOnlyList<OpenAiScientificConflict> Conflicts,
        OpenAiScientificFigureProposal? FigureProposal);

    private sealed record OpenAiScientificTerm(
        string TermId,
        string CanonicalTerm,
        string Definition,
        IReadOnlyList<string> Aliases,
        string SourceBlockId);

    private sealed record OpenAiScientificClaim(
        string MergeKey,
        string Category,
        string NormalizedStatement,
        string SourceWording,
        double Confidence,
        string Status,
        string SourceBlockId,
        string QuotedText,
        string EvidenceRole,
        string EvidenceValidationState);

    private sealed record OpenAiScientificConflict(
        string ConflictId,
        string FirstMergeKey,
        string SecondMergeKey,
        string Description);

    private sealed record OpenAiScientificFigureProposal(
        string CentralMessage,
        IReadOnlyList<OpenAiScientificElementProposal> Elements,
        IReadOnlyList<OpenAiScientificRelationProposal> Relations);

    private sealed record OpenAiScientificElementProposal(
        string ProposalId,
        string Meaning,
        IReadOnlyList<string> SourceBlockIds);

    private sealed record OpenAiScientificRelationProposal(
        string ProposalId,
        string SourceProposalId,
        string TargetProposalId,
        string RelationClass,
        IReadOnlyList<string> SourceBlockIds);

    private const string Schema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "terms": {"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"termId":{"type":"string"},"canonicalTerm":{"type":"string"},"definition":{"type":"string"},"aliases":{"type":"array","items":{"type":"string"}},"sourceBlockId":{"type":"string"}},"required":["termId","canonicalTerm","definition","aliases","sourceBlockId"]}},
            "claims": {"type":"array","items":{"$ref":"#/$defs/claim"}},
            "limitations": {"type":"array","items":{"$ref":"#/$defs/limitation"}},
            "conflicts": {"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"conflictId":{"type":"string"},"firstMergeKey":{"type":"string"},"secondMergeKey":{"type":"string"},"description":{"type":"string"}},"required":["conflictId","firstMergeKey","secondMergeKey","description"]}},
            "figureProposal": {"anyOf":[{"type":"object","additionalProperties":false,"properties":{"centralMessage":{"type":"string"},"elements":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"proposalId":{"type":"string"},"meaning":{"type":"string"},"sourceBlockIds":{"type":"array","items":{"type":"string"}}},"required":["proposalId","meaning","sourceBlockIds"]}},"relations":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"proposalId":{"type":"string"},"sourceProposalId":{"type":"string"},"targetProposalId":{"type":"string"},"relationClass":{"type":"string","enum":["causal","directional","comparative","associative-non-causal"]},"sourceBlockIds":{"type":"array","items":{"type":"string"}}},"required":["proposalId","sourceProposalId","targetProposalId","relationClass","sourceBlockIds"]}}},"required":["centralMessage","elements","relations"]},{"type":"null"}]}
          },
          "required": ["terms","claims","limitations","conflicts","figureProposal"],
          "$defs": {
            "claim":{"type":"object","additionalProperties":false,"properties":{"mergeKey":{"type":"string"},"category":{"type":"string","enum":["Definition","Mechanism","CausalRelation","ProcessStep","Comparison","QuantitativeResult","Constraint","Limitation","Uncertainty"]},"normalizedStatement":{"type":"string"},"sourceWording":{"type":"string"},"confidence":{"type":"number","minimum":0,"maximum":1},"status":{"type":"string","enum":["Draft","Accepted","Rejected"]},"sourceBlockId":{"type":"string"},"quotedText":{"type":"string"},"evidenceRole":{"type":"string","enum":["Support","Definition","Qualification","Contradiction"]},"evidenceValidationState":{"type":"string","enum":["Draft","Validated","Rejected"]}},"required":["mergeKey","category","normalizedStatement","sourceWording","confidence","status","sourceBlockId","quotedText","evidenceRole","evidenceValidationState"]},
            "limitation":{"type":"object","additionalProperties":false,"properties":{"mergeKey":{"type":"string"},"category":{"type":"string","enum":["Limitation"]},"normalizedStatement":{"type":"string"},"sourceWording":{"type":"string"},"confidence":{"type":"number","minimum":0,"maximum":1},"status":{"type":"string","enum":["Draft","Accepted","Rejected"]},"sourceBlockId":{"type":"string"},"quotedText":{"type":"string"},"evidenceRole":{"type":"string","enum":["Support","Definition","Qualification","Contradiction"]},"evidenceValidationState":{"type":"string","enum":["Draft","Validated","Rejected"]}},"required":["mergeKey","category","normalizedStatement","sourceWording","confidence","status","sourceBlockId","quotedText","evidenceRole","evidenceValidationState"]}
          }
        }
        """;
}
