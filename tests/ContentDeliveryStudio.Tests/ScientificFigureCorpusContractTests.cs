using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureCorpusContractTests
{
    private static readonly string[] RequiredCorpusItemFields =
    [
        "itemId",
        "category",
        "admissionStatus",
        "source",
        "figureObjective",
        "goldBaselinePath",
    ];

    private static readonly string[] RequiredSourceFields =
    [
        "sourceId",
        "title",
        "sourceType",
        "publicUrl",
        "license",
        "contentHash",
        "evidenceLocations",
        "localCacheKey",
        "extractionAssessment",
    ];

    private static readonly string[] RequiredExtractionAssessmentFields =
    [
        "status",
        "method",
        "assessedAt",
        "textHash",
        "anchorCoverage",
    ];

    private static readonly string[] RequiredBaselineFields =
    [
        "version",
        "itemId",
        "sourceHash",
        "figureObjective",
        "claims",
        "anchors",
        "elements",
        "relations",
        "allowedVariation",
        "mutations",
        "humanReview",
    ];

    [Fact]
    public void Schemas_RequireScientificCorpusAuthorityFields()
    {
        var corpusSchema = LoadObject("eval/scientific-figures/corpus.schema.json");
        var baselineSchema = LoadObject("eval/scientific-figures/gold-baseline.schema.json");

        Assert.Equal(
            RequiredCorpusItemFields.Order(),
            ReadRequiredFields(corpusSchema, "$defs", "corpusItem").Order());
        Assert.Equal(
            RequiredSourceFields.Order(),
            ReadRequiredFields(corpusSchema, "$defs", "source").Order());
        Assert.Equal(
            RequiredExtractionAssessmentFields.Order(),
            ReadRequiredFields(corpusSchema, "$defs", "extractionAssessment").Order());
        Assert.Equal(
            RequiredBaselineFields.Order(),
            ReadRequiredFields(baselineSchema).Order());
        Assert.Equal(
            ["status"],
            ReadRequiredFields(baselineSchema, "$defs", "humanReview"));

        var mutationCoverage = baselineSchema["properties"]?["mutations"]?["allOf"]?.AsArray();
        Assert.NotNull(mutationCoverage);
        Assert.Contains(mutationCoverage!, rule => ContainsMutationCategory(rule, "scientific"));
        Assert.Contains(mutationCoverage!, rule => ContainsMutationCategory(rule, "visual"));
    }

    [Fact]
    public void CorpusManifest_DeclaresTwelveItemBoundaryAndValidatesEveryAdmittedItem()
    {
        var manifest = LoadObject("eval/scientific-figures/corpus.json");

        Assert.Equal("./corpus.schema.json", manifest["$schema"]?.GetValue<string>());
        Assert.Equal(1, manifest["version"]?.GetValue<int>());
        Assert.Equal(12, manifest["requiredItemCount"]?.GetValue<int>());
        Assert.Equal("building", manifest["admissionState"]?.GetValue<string>());
        Assert.Equal(".cache", manifest["localCacheRoot"]?.GetValue<string>());

        var categoryRequirements = manifest["categoryRequirements"]?.AsObject();
        Assert.NotNull(categoryRequirements);
        Assert.Equal(4, categoryRequirements!["mechanism-process"]?.GetValue<int>());
        Assert.Equal(4, categoryRequirements["concept-comparison"]?.GetValue<int>());
        Assert.Equal(4, categoryRequirements["graphical-abstract"]?.GetValue<int>());

        Assert.Empty(ValidateCorpusManifest(manifest));
        var items = manifest["items"]!.AsArray();
        foreach (var node in items)
        {
            var item = Assert.IsType<JsonObject>(node);
            Assert.Empty(ValidateCorpusItem(item));

            var baselinePath = item["goldBaselinePath"]!.GetValue<string>();
            var baseline = LoadObject(baselinePath);
            Assert.Empty(ValidateGoldBaseline(baseline));
            Assert.Equal(item["itemId"]!.GetValue<string>(), baseline["itemId"]!.GetValue<string>());
            Assert.Equal(
                item["source"]!["contentHash"]!.GetValue<string>(),
                baseline["sourceHash"]!.GetValue<string>());
        }
    }

    [Fact]
    public void CorpusContract_RejectsIncompleteHumanApprovedCorpus()
    {
        var manifest = LoadObject("eval/scientific-figures/corpus.json");
        manifest["admissionState"] = "human-approved";

        Assert.Contains("items.count", ValidateCorpusManifest(manifest));
    }

    public static TheoryData<string> MissingCorpusAuthorityCases => new()
    {
        "source.license",
        "source.contentHash",
        "source.evidenceLocations",
        "source.extractionAssessment",
    };

    [Theory]
    [MemberData(nameof(MissingCorpusAuthorityCases))]
    public void CorpusContract_RejectsMissingSourceAuthority(string missingPath)
    {
        var item = CreateValidCorpusItem();
        RemovePath(item, missingPath);

        Assert.Contains(missingPath, ValidateCorpusItem(item));
    }

    [Fact]
    public void GoldBaselineContract_RejectsMissingEvidenceLocation()
    {
        var baseline = CreateValidGoldBaseline();
        baseline["anchors"]![0]!.AsObject().Remove("location");

        Assert.Contains("anchors[0].location", ValidateGoldBaseline(baseline));
    }

    [Theory]
    [InlineData("scientific")]
    [InlineData("visual")]
    public void GoldBaselineContract_RejectsMissingMutationCoverage(string category)
    {
        var baseline = CreateValidGoldBaseline();
        var mutations = baseline["mutations"]!.AsArray();
        var mutation = mutations.Single(node => node!["category"]!.GetValue<string>() == category);
        mutations.Remove(mutation);

        Assert.Contains($"mutations.{category}", ValidateGoldBaseline(baseline));
    }

    [Fact]
    public void GoldBaselineContract_RejectsAcceptedReviewWithoutReviewer()
    {
        var baseline = CreateValidGoldBaseline();
        var humanReview = baseline["humanReview"]!.AsObject();
        humanReview.Remove("reviewer");
        humanReview.Remove("reviewedAt");

        Assert.Contains("humanReview.reviewer", ValidateGoldBaseline(baseline));
        Assert.Contains("humanReview.reviewedAt", ValidateGoldBaseline(baseline));
    }

    [Fact]
    public void GoldBaselineContract_AllowsDraftWithoutFabricatedReviewer()
    {
        var baseline = CreateValidGoldBaseline();
        baseline["humanReview"] = JsonNode.Parse("""{"status":"draft"}""");

        Assert.Empty(ValidateGoldBaseline(baseline));
    }

    private static IReadOnlyCollection<string> ValidateCorpusItem(JsonObject item)
    {
        var errors = new List<string>();
        AddMissingFields(errors, item, RequiredCorpusItemFields, string.Empty);

        if (item["source"] is not JsonObject source)
        {
            errors.Add("source");
            return errors;
        }

        AddMissingFields(errors, source, RequiredSourceFields, "source.");
        if (source["license"] is not JsonObject license
            || !HasValue(license, "spdxOrExpression")
            || !HasValue(license, "redistribution")
            || !HasValue(license, "sourceUrl")
            || !HasValue(license, "reviewedAt"))
        {
            errors.Add("source.license");
        }

        if (!HasSha256(source["contentHash"]))
        {
            errors.Add("source.contentHash");
        }

        if (source["evidenceLocations"] is not JsonArray { Count: > 0 })
        {
            errors.Add("source.evidenceLocations");
        }

        if (source["extractionAssessment"] is not JsonObject extractionAssessment)
        {
            errors.Add("source.extractionAssessment");
        }
        else
        {
            AddMissingFields(
                errors,
                extractionAssessment,
                RequiredExtractionAssessmentFields,
                "source.extractionAssessment.");
            if (!HasSha256(extractionAssessment["textHash"]))
            {
                errors.Add("source.extractionAssessment.textHash");
            }

            if (item["admissionStatus"]?.GetValue<string>() == "accepted"
                && (extractionAssessment["status"]?.GetValue<string>() != "passed"
                    || extractionAssessment["anchorCoverage"]?.GetValue<string>() != "complete"))
            {
                errors.Add("source.extractionAssessment.acceptance");
            }
        }

        return errors;
    }

    private static IReadOnlyCollection<string> ValidateCorpusManifest(JsonObject manifest)
    {
        var errors = new List<string>();
        if (manifest["items"] is not JsonArray items)
        {
            errors.Add("items");
            return errors;
        }

        if (items.Count > 12)
        {
            errors.Add("items.count");
        }

        if (manifest["admissionState"]?.GetValue<string>() != "human-approved")
        {
            return errors;
        }

        if (items.Count != 12)
        {
            errors.Add("items.count");
        }

        foreach (var category in new[] { "mechanism-process", "concept-comparison", "graphical-abstract" })
        {
            if (items.Count(node => node?["category"]?.GetValue<string>() == category) != 4)
            {
                errors.Add($"items.category.{category}");
            }
        }

        if (items.Any(node => node?["admissionStatus"]?.GetValue<string>() != "accepted"))
        {
            errors.Add("items.admissionStatus");
        }

        return errors;
    }

    private static IReadOnlyCollection<string> ValidateGoldBaseline(JsonObject baseline)
    {
        var errors = new List<string>();
        AddMissingFields(errors, baseline, RequiredBaselineFields, string.Empty);

        if (!HasSha256(baseline["sourceHash"]))
        {
            errors.Add("sourceHash");
        }

        if (baseline["anchors"] is not JsonArray { Count: > 0 } anchors)
        {
            errors.Add("anchors");
        }
        else
        {
            for (var index = 0; index < anchors.Count; index++)
            {
                if (anchors[index] is not JsonObject anchor || !HasValue(anchor, "location"))
                {
                    errors.Add($"anchors[{index}].location");
                }
            }
        }

        if (baseline["mutations"] is not JsonArray mutations)
        {
            errors.Add("mutations.scientific");
            errors.Add("mutations.visual");
        }
        else
        {
            foreach (var category in new[] { "scientific", "visual" })
            {
                if (!mutations.Any(node => node?["category"]?.GetValue<string>() == category))
                {
                    errors.Add($"mutations.{category}");
                }
            }
        }

        if (baseline["humanReview"] is not JsonObject humanReview
            || !HasValue(humanReview, "status"))
        {
            errors.Add("humanReview.status");
        }
        else if (humanReview["status"]?.GetValue<string>() is "accepted" or "rejected")
        {
            if (!HasValue(humanReview, "reviewer"))
            {
                errors.Add("humanReview.reviewer");
            }

            if (!HasValue(humanReview, "reviewedAt"))
            {
                errors.Add("humanReview.reviewedAt");
            }
        }

        return errors;
    }

    private static JsonObject CreateValidCorpusItem()
    {
        return JsonNode.Parse(
            """
            {
              "itemId": "fixture-mechanism-01",
              "category": "mechanism-process",
              "admissionStatus": "accepted",
              "source": {
                "sourceId": "fixture-source-01",
                "title": "Redistribution-safe mechanics fixture",
                "sourceType": "repo-fixture",
                "publicUrl": "https://example.invalid/fixture",
                "license": {
                  "spdxOrExpression": "CC0-1.0",
                  "redistribution": "allowed",
                  "sourceUrl": "https://example.invalid/license",
                  "reviewedAt": "2026-07-26"
                },
                "contentHash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "evidenceLocations": ["fixture.md: paragraph 1"],
                "localCacheKey": "fixture-mechanism-01.md",
                "extractionAssessment": {
                  "status": "passed",
                  "method": "repository fixture direct-text read",
                  "assessedAt": "2026-07-26",
                  "textHash": "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "anchorCoverage": "complete"
                }
              },
              "figureObjective": "Explain a balanced-force mechanism.",
              "goldBaselinePath": "eval/scientific-figures/fixtures/fixture-mechanism-01.json"
            }
            """)!.AsObject();
    }

    private static JsonObject CreateValidGoldBaseline()
    {
        return JsonNode.Parse(
            """
            {
              "version": 1,
              "itemId": "fixture-mechanism-01",
              "sourceHash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              "figureObjective": "Explain a balanced-force mechanism.",
              "claims": [{"claimId": "claim-1", "text": "The forces balance.", "anchorIds": ["anchor-1"]}],
              "anchors": [{"anchorId": "anchor-1", "location": "fixture.md: paragraph 1", "exactQuote": "The forces balance."}],
              "elements": [
                {"elementId": "body", "meaning": "The body", "anchorIds": ["anchor-1"]},
                {"elementId": "left-force", "meaning": "The leftward force", "anchorIds": ["anchor-1"]},
                {"elementId": "right-force", "meaning": "The rightward force", "anchorIds": ["anchor-1"]}
              ],
              "relations": [{"relationId": "balance", "sourceElementId": "left-force", "targetElementId": "right-force", "relationType": "balances", "anchorIds": ["anchor-1"]}],
              "allowedVariation": [{"scope": "layout", "rule": "Equivalent spacing is allowed."}],
              "mutations": [
                {"mutationId": "scientific-1", "category": "scientific", "description": "Reverse a force.", "expectedOutcome": "block"},
                {"mutationId": "visual-1", "category": "visual", "description": "Clip a label.", "expectedOutcome": "block"}
              ],
              "humanReview": {"status": "accepted", "reviewer": "fixture", "reviewedAt": "2026-07-26"}
            }
            """)!.AsObject();
    }

    private static void AddMissingFields(
        ICollection<string> errors,
        JsonObject target,
        IEnumerable<string> fields,
        string prefix)
    {
        foreach (var field in fields)
        {
            if (!HasValue(target, field))
            {
                errors.Add($"{prefix}{field}");
            }
        }
    }

    private static bool HasValue(JsonObject target, string field)
    {
        return target.TryGetPropertyValue(field, out var value)
            && value is not null
            && (value is not JsonValue jsonValue
                || !jsonValue.TryGetValue<string>(out var text)
                || !string.IsNullOrWhiteSpace(text));
    }

    private static bool HasSha256(JsonNode? node)
    {
        var value = node?.GetValue<string>();
        return value is not null
            && value.StartsWith("sha256:", StringComparison.Ordinal)
            && value.Length == "sha256:".Length + 64
            && value["sha256:".Length..].All(Uri.IsHexDigit);
    }

    private static void RemovePath(JsonObject root, string path)
    {
        var segments = path.Split('.');
        var parent = root;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            parent = parent[segments[index]]!.AsObject();
        }

        parent.Remove(segments[^1]);
    }

    private static IReadOnlyCollection<string> ReadRequiredFields(
        JsonObject schema,
        params string[] path)
    {
        JsonNode? node = schema;
        foreach (var segment in path)
        {
            node = node?[segment];
        }

        return node?["required"]?.AsArray()
            .Select(value => value!.GetValue<string>())
            .ToArray()
            ?? [];
    }

    private static bool ContainsMutationCategory(JsonNode? node, string category)
    {
        return node?["contains"]?["properties"]?["category"]?["const"]?.GetValue<string>() == category;
    }

    private static JsonObject LoadObject(string relativePath)
    {
        var fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return JsonNode.Parse(File.ReadAllText(fullPath))?.AsObject()
            ?? throw new InvalidDataException($"JSON object was not found: {relativePath}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find ContentDeliveryStudio.sln from the test output path.");
    }
}
