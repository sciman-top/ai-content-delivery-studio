# Preset Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add a provider-neutral preset recommendation layer so prompt directions can carry executable image type, style, recipe, and review recommendations without letting AI invent queue settings.

**Architecture:** Extend the current `Brief Studio` model with additive metadata. `ImageTypePreset` remains the stable delivery preset catalog, `PromptDirectionRecommendation` becomes the structured recommendation payload, fake planning emits recommendations first, and application services persist them through the existing creative brief JSON direction storage. Real OpenAI calls remain guarded.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, EF Core SQLite JSON conversions, xUnit, existing fake providers.

**Historical note:** This plan predates the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename. Old code and test paths below are preserved as historical implementation context, not current repository truth.

---

## Scope Check

This plan implements the first usable slice of `docs/superpowers/specs/2026-06-02-preset-governance-design.md`.

Included:

- Add governance metadata to the existing image type preset catalog.
- Add `PromptDirectionRecommendation` as a core domain value object.
- Attach recommendation metadata to `PromptDirection` and `PromptDirectionDraft`.
- Persist recommendation metadata through existing creative brief direction JSON storage.
- Make `FakeTextPlanningProvider` emit executable catalog IDs, reasons, confidence, warnings, and non-executable suggestions.
- Preserve the OpenAI real-call guard for brief direction planning.
- Update `docs/TASKS.md` with a preset governance phase.

Not included in this slice:

- Real OpenAI structured brief-direction output.
- Full WPF editing UI for recommendations.
- New image type preset IDs beyond the current catalog.
- Real image generation or paid provider calls.

## File Structure

- `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`: extend preset metadata and keep current IDs stable.
- `src/ImageSeriesStudio.Core/Projects/PromptDirectionRecommendation.cs`: new recommendation value object and validation rules.
- `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`: attach optional recommendation metadata to `PromptDirection`.
- `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`: extend `BriefPlanningRequest` and `PromptDirectionDraft`.
- `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`: emit structured recommendations from fake planning.
- `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`: map draft recommendations into persisted prompt directions.
- `tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs`: catalog governance invariants.
- `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`: recommendation value object and prompt direction storage coverage.
- `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`: SQLite round-trip coverage for recommendation metadata.
- `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`: fake provider recommendation coverage.
- `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`: service workflow persistence coverage.
- `docs/TASKS.md`: add preset governance tasks.

---

### Task 1: Add Preset Catalog Governance Metadata

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`
- Test: `tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs`

- [x] **Step 1: Write failing catalog governance tests**

Add these tests to `tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs`:

```csharp
[Fact]
public void Catalog_ContainsGovernanceMetadataForEveryPreset()
{
    foreach (var preset in ImageTypePresetCatalog.Defaults)
    {
        Assert.Equal(ImageTypePresetCatalog.CatalogVersion, preset.CatalogVersion);
        Assert.False(string.IsNullOrWhiteSpace(preset.DeliveryFamily));
        Assert.Contains(preset.DefaultAspectRatio, preset.SupportedAspectRatios);
        Assert.False(string.IsNullOrWhiteSpace(preset.DefaultQualityBand));
        Assert.NotEmpty(preset.WorkflowModes);
        Assert.NotEmpty(preset.StyleDimensionHints);
        Assert.NotEmpty(preset.RequiredBriefFields);
        Assert.NotEmpty(preset.CommonFailureModes);
        Assert.NotEmpty(preset.CapabilityRequirements);
        Assert.All(preset.WorkflowModes, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(preset.StyleDimensionHints, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(preset.RequiredBriefFields, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(preset.CommonFailureModes, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(preset.CapabilityRequirements, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        _ = ReviewRubricTemplateCatalog.GetById(preset.ReviewRubricTemplateId);
    }
}

[Fact]
public void Catalog_TextHeavyPresetsUseDeterministicTextPolicy()
{
    var textHeavyPresetIds = new[]
    {
        ImageTypePresetCatalog.EducationalPoster,
        ImageTypePresetCatalog.ConceptDiagram,
        ImageTypePresetCatalog.GraphicalAbstract,
        ImageTypePresetCatalog.ScholarlySchematic,
        ImageTypePresetCatalog.BackgroundPlate,
    };

    foreach (var presetId in textHeavyPresetIds)
    {
        var preset = ImageTypePresetCatalog.GetById(presetId);

        Assert.Equal(ImageTextPolicy.DeterministicPostRender, preset.TextPolicy);
        Assert.Contains(preset.CapabilityRequirements, value =>
            value.Contains("deterministic", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [x] **Step 2: Run the failing catalog tests**

Run:

```powershell
dotnet test --filter "Catalog_ContainsGovernanceMetadataForEveryPreset|Catalog_TextHeavyPresetsUseDeterministicTextPolicy"
```

Expected: fails because the new governance metadata properties do not exist.

- [x] **Step 3: Extend the preset record**

In `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`, extend the private constructor, public properties, and `Create` method.

Add these constructor parameters after `deliveryNamingPolicy`:

```csharp
string catalogVersion,
string deliveryFamily,
IReadOnlyList<AspectRatio> supportedAspectRatios,
ImageBackgroundMode defaultBackgroundMode,
string defaultQualityBand,
IReadOnlyList<string> workflowModes,
IReadOnlyList<string> styleDimensionHints,
IReadOnlyList<string> requiredBriefFields,
IReadOnlyList<string> commonFailureModes,
IReadOnlyList<string> capabilityRequirements,
bool isDeprecated
```

Add these properties:

```csharp
public string CatalogVersion { get; }

public string DeliveryFamily { get; }

public IReadOnlyList<AspectRatio> SupportedAspectRatios { get; }

public ImageBackgroundMode DefaultBackgroundMode { get; }

public string DefaultQualityBand { get; }

public IReadOnlyList<string> WorkflowModes { get; }

public IReadOnlyList<string> StyleDimensionHints { get; }

public IReadOnlyList<string> RequiredBriefFields { get; }

public IReadOnlyList<string> CommonFailureModes { get; }

public IReadOnlyList<string> CapabilityRequirements { get; }

public bool IsDeprecated { get; }
```

Inside the constructor, assign the values like this:

```csharp
CatalogVersion = catalogVersion;
DeliveryFamily = deliveryFamily;
SupportedAspectRatios = supportedAspectRatios;
DefaultBackgroundMode = defaultBackgroundMode;
DefaultQualityBand = defaultQualityBand;
WorkflowModes = workflowModes;
StyleDimensionHints = styleDimensionHints;
RequiredBriefFields = requiredBriefFields;
CommonFailureModes = commonFailureModes;
CapabilityRequirements = capabilityRequirements;
IsDeprecated = isDeprecated;
```

Update `ImageTypePreset.Create` to accept the same arguments and call helpers:

```csharp
ArgumentNullException.ThrowIfNull(defaultAspectRatio);

var normalizedSupportedRatios = NormalizeAspectRatios(supportedAspectRatios, defaultAspectRatio);

return new ImageTypePreset(
    RequireText(id, nameof(id)),
    RequireText(displayName, nameof(displayName)),
    description.Trim(),
    defaultAspectRatio,
    RequireText(defaultOutputFormat, nameof(defaultOutputFormat)).ToLowerInvariant(),
    textPolicy,
    RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)),
    RequireText(deliveryNamingPolicy, nameof(deliveryNamingPolicy)),
    RequireText(catalogVersion, nameof(catalogVersion)),
    RequireText(deliveryFamily, nameof(deliveryFamily)),
    normalizedSupportedRatios,
    defaultBackgroundMode,
    RequireText(defaultQualityBand, nameof(defaultQualityBand)).ToLowerInvariant(),
    NormalizeTextList(workflowModes, nameof(workflowModes)),
    NormalizeTextList(styleDimensionHints, nameof(styleDimensionHints)),
    NormalizeTextList(requiredBriefFields, nameof(requiredBriefFields)),
    NormalizeTextList(commonFailureModes, nameof(commonFailureModes)),
    NormalizeTextList(capabilityRequirements, nameof(capabilityRequirements)),
    isDeprecated);
```

Add these helpers inside `ImageTypePreset`:

```csharp
private static IReadOnlyList<AspectRatio> NormalizeAspectRatios(
    IReadOnlyList<AspectRatio> supportedAspectRatios,
    AspectRatio defaultAspectRatio)
{
    var ratios = supportedAspectRatios
        .Append(defaultAspectRatio)
        .Distinct()
        .ToArray();

    return ratios.Length == 0 ? [defaultAspectRatio] : ratios;
}

private static IReadOnlyList<string> NormalizeTextList(
    IReadOnlyList<string> values,
    string parameterName)
{
    var normalized = values
        .Select(value => value.Trim())
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (normalized.Length == 0)
    {
        throw new ArgumentException("At least one value is required.", parameterName);
    }

    return normalized;
}
```

- [x] **Step 4: Add catalog version and update preset entries**

In `ImageTypePresetCatalog`, add:

```csharp
public const string CatalogVersion = "2026-06-02";
```

Update each existing `ImageTypePreset.Create` call with governance metadata. Use these exact value groups:

```csharp
// educational-poster
CatalogVersion,
"education",
[new AspectRatio(4, 5), new AspectRatio(16, 9)],
ImageBackgroundMode.Opaque,
"draft",
["text-to-image", "background-plate"],
["educational", "diagram", "poster"],
["goal", "audience", "must_include", "text_policy"],
["unreadable small text", "crowded layout", "formula hallucination"],
["deterministic text composition", "provider size support"],
isDeprecated: false
```

```csharp
// article-cover
CatalogVersion,
"editorial",
[new AspectRatio(16, 9), new AspectRatio(4, 5), new AspectRatio(1, 1)],
ImageBackgroundMode.Auto,
"draft",
["text-to-image"],
["editorial", "cover", "hero"],
["goal", "audience", "delivery_context"],
["weak first impression", "unsupported headline text"],
["provider size support", "hybrid text review"],
isDeprecated: false
```

```csharp
// article-inline-illustration
CatalogVersion,
"editorial",
[new AspectRatio(16, 9), new AspectRatio(4, 3), new AspectRatio(1, 1)],
ImageBackgroundMode.Auto,
"draft",
["text-to-image", "reference-image"],
["editorial", "inline", "explanatory"],
["goal", "audience", "source_evidence"],
["adds unsupported claims", "poor section fit"],
["provider size support", "source evidence review"],
isDeprecated: false
```

```csharp
// concept-diagram
CatalogVersion,
"education",
[new AspectRatio(16, 9), new AspectRatio(4, 3)],
ImageBackgroundMode.Opaque,
"draft",
["text-to-image", "background-plate"],
["conceptual", "diagram", "teaching"],
["goal", "audience", "must_include", "must_avoid"],
["incorrect concept relationship", "unreadable labels"],
["deterministic text composition", "concept accuracy review"],
isDeprecated: false
```

```csharp
// graphical-abstract
CatalogVersion,
"scholarly",
[new AspectRatio(16, 9), new AspectRatio(4, 3)],
ImageBackgroundMode.Opaque,
"draft",
["text-to-image", "background-plate"],
["schematic", "summary", "scholarly"],
["goal", "source_evidence", "must_avoid"],
["implies fake evidence", "overly decorative layout"],
["deterministic text composition", "scholarly evidence review"],
isDeprecated: false
```

```csharp
// scholarly-schematic
CatalogVersion,
"scholarly",
[new AspectRatio(16, 9), new AspectRatio(4, 3)],
ImageBackgroundMode.Opaque,
"draft",
["text-to-image", "background-plate"],
["schematic", "concept-level", "evidence-safe"],
["goal", "source_evidence", "known_constraints"],
["fabricated experimental evidence", "ambiguous causal relationship"],
["deterministic text composition", "scholarly evidence review"],
isDeprecated: false
```

```csharp
// social-square
CatalogVersion,
"social",
[new AspectRatio(1, 1), new AspectRatio(4, 5), new AspectRatio(16, 9)],
ImageBackgroundMode.Auto,
"draft",
["text-to-image"],
["social", "compact", "high-contrast"],
["goal", "audience", "delivery_context"],
["poor thumbnail readability", "too much small text"],
["provider size support", "hybrid text review"],
isDeprecated: false
```

```csharp
// background-plate
CatalogVersion,
"layout",
[new AspectRatio(16, 9), new AspectRatio(4, 5), new AspectRatio(1, 1)],
ImageBackgroundMode.Opaque,
"draft",
["background-plate", "text-to-image"],
["background", "clean-space", "layout-safe"],
["goal", "text_policy", "layout_constraints"],
["insufficient text space", "visual clutter"],
["deterministic text composition", "layout review"],
isDeprecated: false
```

- [x] **Step 5: Verify catalog governance tests pass**

Run:

```powershell
dotnet test --filter "Catalog_ContainsGovernanceMetadataForEveryPreset|Catalog_TextHeavyPresetsUseDeterministicTextPolicy"
```

Expected: both tests pass.

- Commit catalog slice.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs
git commit -m "feat: 添加图片预设治理元数据"
```

---

### Task 2: Add Prompt Direction Recommendation Domain Model

**Files:**

- Create: `src/ImageSeriesStudio.Core/Projects/PromptDirectionRecommendation.cs`
- Modify: `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`
- Test: `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`
- Test: `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`

- [x] **Step 1: Write failing domain test**

Add this test to `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`:

```csharp
[Fact]
public void PromptDirection_StoresStructuredRecommendation()
{
    var timestamp = new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero);
    var recommendation = PromptDirectionRecommendation.Create(
        ImageTypePresetCatalog.EducationalPoster,
        ImageTextPolicy.DeterministicPostRender,
        "clean classroom poster",
        new AspectRatio(4, 5),
        1024,
        1280,
        "draft",
        "png",
        ImageBackgroundMode.Opaque,
        ReviewRubricTemplateCatalog.TextHeavyPoster,
        draftCount: 2,
        finalCount: 1,
        "Text-heavy educational output needs deterministic labels.",
        confidence: 0.9,
        ["model text can be unreliable"],
        ["reserve a formula area"]);

    var direction = PromptDirection.Create(
        "conservative",
        "Conservative faithful",
        "Use for accurate classroom delivery.",
        "Create a clean educational poster background.",
        "No unreadable formula text.",
        "Accurate and easy to review.",
        "Less dramatic than a cover image.",
        timestamp,
        recommendation);

    Assert.NotNull(direction.Recommendation);
    Assert.Equal(ImageTypePresetCatalog.EducationalPoster, direction.Recommendation.ImageTypePresetId);
    Assert.Equal(ImageTextPolicy.DeterministicPostRender, direction.Recommendation.TextPolicy);
    Assert.Equal(new AspectRatio(4, 5), direction.Recommendation.AspectRatio);
    Assert.Equal("draft", direction.Recommendation.QualityBand);
    Assert.Equal("png", direction.Recommendation.OutputFormat);
    Assert.Equal(0.9, direction.Recommendation.Confidence);
    Assert.Equal("model text can be unreliable", Assert.Single(direction.Recommendation.CapabilityWarnings));
    Assert.Equal("reserve a formula area", Assert.Single(direction.Recommendation.NonExecutableSuggestions));
}
```

- [x] **Step 2: Write failing validation test**

Add this test to `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`:

```csharp
[Fact]
public void PromptDirectionRecommendation_RejectsInvalidExecutableValues()
{
    Assert.Throws<InvalidOperationException>(() =>
        PromptDirectionRecommendation.Create(
            "missing-preset",
            ImageTextPolicy.Hybrid,
            "style",
            new AspectRatio(1, 1),
            1024,
            1024,
            "draft",
            "png",
            ImageBackgroundMode.Auto,
            ReviewRubricTemplateCatalog.GeneralImage,
            draftCount: 1,
            finalCount: 1,
            "reason",
            confidence: 0.7,
            [],
            []));

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        PromptDirectionRecommendation.Create(
            ImageTypePresetCatalog.ArticleCover,
            ImageTextPolicy.Hybrid,
            "style",
            new AspectRatio(16, 9),
            0,
            1024,
            "draft",
            "png",
            ImageBackgroundMode.Auto,
            ReviewRubricTemplateCatalog.GeneralImage,
            draftCount: 1,
            finalCount: 1,
            "reason",
            confidence: 0.7,
            [],
            []));

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        PromptDirectionRecommendation.Create(
            ImageTypePresetCatalog.ArticleCover,
            ImageTextPolicy.Hybrid,
            "style",
            new AspectRatio(16, 9),
            1536,
            1024,
            "draft",
            "png",
            ImageBackgroundMode.Auto,
            ReviewRubricTemplateCatalog.GeneralImage,
            draftCount: 1,
            finalCount: 1,
            "reason",
            confidence: 1.2,
            [],
            []));
}
```

- [x] **Step 3: Run the failing domain tests**

Run:

```powershell
dotnet test --filter "PromptDirection_StoresStructuredRecommendation|PromptDirectionRecommendation_RejectsInvalidExecutableValues"
```

Expected: fails because `PromptDirectionRecommendation` and the new `PromptDirection.Create` overload do not exist.

- [x] **Step 4: Add recommendation value object**

Create `src/ImageSeriesStudio.Core/Projects/PromptDirectionRecommendation.cs`:

```csharp
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Projects;

public sealed record PromptDirectionRecommendation(
    string ImageTypePresetId,
    ImageTextPolicy TextPolicy,
    string StyleIntent,
    AspectRatio AspectRatio,
    int Width,
    int Height,
    string QualityBand,
    string OutputFormat,
    ImageBackgroundMode BackgroundMode,
    string ReviewRubricTemplateId,
    int DraftCount,
    int FinalCount,
    string RecommendationReason,
    double Confidence,
    IReadOnlyList<string> CapabilityWarnings,
    IReadOnlyList<string> NonExecutableSuggestions)
{
    public static PromptDirectionRecommendation Create(
        string imageTypePresetId,
        ImageTextPolicy textPolicy,
        string styleIntent,
        AspectRatio aspectRatio,
        int width,
        int height,
        string qualityBand,
        string outputFormat,
        ImageBackgroundMode backgroundMode,
        string reviewRubricTemplateId,
        int draftCount,
        int finalCount,
        string recommendationReason,
        double confidence,
        IReadOnlyList<string> capabilityWarnings,
        IReadOnlyList<string> nonExecutableSuggestions)
    {
        ArgumentNullException.ThrowIfNull(aspectRatio);
        _ = ImageTypePresetCatalog.GetById(RequireText(imageTypePresetId, nameof(imageTypePresetId)));
        _ = ReviewRubricTemplateCatalog.GetById(RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)));

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        if (draftCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(draftCount), "Draft count cannot be negative.");
        }

        if (finalCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(finalCount), "Final count must be greater than zero.");
        }

        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        return new PromptDirectionRecommendation(
            RequireText(imageTypePresetId, nameof(imageTypePresetId)),
            textPolicy,
            styleIntent.Trim(),
            aspectRatio,
            width,
            height,
            RequireText(qualityBand, nameof(qualityBand)).ToLowerInvariant(),
            RequireText(outputFormat, nameof(outputFormat)).ToLowerInvariant(),
            backgroundMode,
            RequireText(reviewRubricTemplateId, nameof(reviewRubricTemplateId)),
            draftCount,
            finalCount,
            RequireText(recommendationReason, nameof(recommendationReason)),
            confidence,
            NormalizeList(capabilityWarnings),
            NormalizeList(nonExecutableSuggestions));
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
```

- [x] **Step 5: Attach recommendations to prompt directions**

Modify `PromptDirection` in `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`.

In the private constructor, add:

```csharp
Recommendation = null;
```

Update the public constructor signature:

```csharp
public PromptDirection(
    Guid id,
    string key,
    string name,
    string intendedUse,
    string promptText,
    string negativePrompt,
    string strength,
    string risk,
    DateTimeOffset createdAt,
    PromptDirectionRecommendation? recommendation = null)
```

Inside the constructor, add:

```csharp
Recommendation = recommendation;
```

Add this property:

```csharp
public PromptDirectionRecommendation? Recommendation { get; private set; }
```

Update `PromptDirection.Create` signature:

```csharp
public static PromptDirection Create(
    string key,
    string name,
    string intendedUse,
    string promptText,
    string negativePrompt,
    string strength,
    string risk,
    DateTimeOffset createdAt,
    PromptDirectionRecommendation? recommendation = null)
```

Update its constructor call:

```csharp
return new PromptDirection(
    Guid.NewGuid(),
    key,
    name,
    intendedUse,
    promptText,
    negativePrompt,
    strength,
    risk,
    createdAt,
    recommendation);
```

- [x] **Step 6: Add persistence round-trip assertion**

In `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`, create a recommendation before the existing persisted prompt direction:

```csharp
var recommendation = PromptDirectionRecommendation.Create(
    ImageTypePresetCatalog.EducationalPoster,
    ImageTextPolicy.DeterministicPostRender,
    "clean editorial science style",
    new AspectRatio(4, 5),
    1024,
    1280,
    "draft",
    "png",
    ImageBackgroundMode.Opaque,
    ReviewRubricTemplateCatalog.TextHeavyPoster,
    draftCount: 2,
    finalCount: 1,
    "Educational poster output needs post-render text.",
    confidence: 0.9,
    ["model-rendered small text is risky"],
    ["reserve label space"]);
```

Pass it into the existing `PromptDirection.Create` call:

```csharp
timestamp.AddMinutes(3),
recommendation)
```

Add this assertion after loading `loadedBrief`:

```csharp
var loadedDirection = Assert.Single(loadedBrief.PromptDirections);
Assert.Equal("conservative", loadedDirection.Key);
Assert.NotNull(loadedDirection.Recommendation);
Assert.Equal(ImageTypePresetCatalog.EducationalPoster, loadedDirection.Recommendation.ImageTypePresetId);
Assert.Equal(1024, loadedDirection.Recommendation.Width);
Assert.Equal(1280, loadedDirection.Recommendation.Height);
Assert.Equal("model-rendered small text is risky", Assert.Single(loadedDirection.Recommendation.CapabilityWarnings));
```

Remove the older single-line assertion:

```csharp
Assert.Equal("conservative", Assert.Single(loadedBrief.PromptDirections).Key);
```

- [x] **Step 7: Verify domain and persistence tests pass**

Run:

```powershell
dotnet test --filter "PromptDirection_StoresStructuredRecommendation|PromptDirectionRecommendation_RejectsInvalidExecutableValues|AppDbContext_SavesAndLoadsCompleteFakeProject"
```

Expected: all selected tests pass.

- Commit recommendation domain slice.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Projects/PromptDirectionRecommendation.cs src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs tests/ImageSeriesStudio.Tests/PersistenceTests.cs
git commit -m "feat: 添加提示词方向推荐模型"
```

---

### Task 3: Extend Fake-First Prompt Direction Planning

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`
- Test: `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`

- [x] **Step 1: Write failing fake recommendation test**

Extend `FakeTextPlanningProvider_CreatesPromptDirectionsForBrief` in `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs` with these assertions:

```csharp
var recommendation = result.Directions[0].Recommendation;

Assert.NotNull(recommendation);
Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, recommendation.ImageTypePresetId);
Assert.Equal(ImageTextPolicy.Hybrid, recommendation.TextPolicy);
Assert.Equal(new AspectRatio(16, 9), recommendation.AspectRatio);
Assert.Equal(1536, recommendation.Width);
Assert.Equal(1024, recommendation.Height);
Assert.Equal("draft", recommendation.QualityBand);
Assert.Equal("png", recommendation.OutputFormat);
Assert.Equal(ReviewRubricTemplateCatalog.EditorialIllustration, recommendation.ReviewRubricTemplateId);
Assert.InRange(recommendation.Confidence, 0.65, 1);
Assert.Contains(recommendation.RecommendationReason, value => value.Contains("article", StringComparison.OrdinalIgnoreCase));
Assert.Contains(recommendation.CapabilityWarnings, value => value.Contains("fake", StringComparison.OrdinalIgnoreCase));
Assert.Contains(recommendation.NonExecutableSuggestions, value => value.Contains("style", StringComparison.OrdinalIgnoreCase));
```

- [x] **Step 2: Run the failing fake recommendation test**

Run:

```powershell
dotnet test --filter FakeTextPlanningProvider_CreatesPromptDirectionsForBrief
```

Expected: fails because `PromptDirectionDraft.Recommendation` does not exist.

- [x] **Step 3: Extend provider contracts**

Modify `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`.

Update `BriefPlanningRequest`:

```csharp
public sealed record BriefPlanningRequest(
    string Goal,
    string Audience,
    string StyleIntent,
    IReadOnlyList<string> MustInclude,
    IReadOnlyList<string> MustAvoid,
    int DirectionCount = 3,
    ImageTextPolicy TextPolicy = ImageTextPolicy.Hybrid);
```

Update `PromptDirectionDraft`:

```csharp
public sealed record PromptDirectionDraft(
    string Key,
    string Name,
    string IntendedUse,
    string PromptText,
    string NegativePrompt,
    string Strength,
    string Risk,
    PromptDirectionRecommendation? Recommendation = null);
```

- [x] **Step 4: Add fake provider recommendation helper**

In `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`, update the direction projection so the `PromptDirectionDraft` constructor receives a recommendation:

```csharp
var directions = templates
    .Take(count)
    .Select(template => new PromptDirectionDraft(
        template.Key,
        template.Name,
        template.Use,
        $"Create {request.Goal} for {request.Audience}. Style: {request.StyleIntent}. Include: {string.Join(", ", request.MustInclude)}.",
        $"Avoid: {string.Join(", ", request.MustAvoid)}.",
        template.Strength,
        template.Risk,
        CreateRecommendation(request, template.Key)))
    .ToArray();
```

Add these helper methods inside `FakeTextPlanningProvider`:

```csharp
private static PromptDirectionRecommendation CreateRecommendation(
    BriefPlanningRequest request,
    string directionKey)
{
    var presetId = SelectPresetId(request.Goal);
    var preset = ImageTypePresetCatalog.GetById(presetId);
    var textPolicy = request.TextPolicy is ImageTextPolicy.ImageModelOnly
        ? preset.TextPolicy
        : request.TextPolicy;
    var (width, height) = GetDefaultSize(preset.DefaultAspectRatio);

    return PromptDirectionRecommendation.Create(
        preset.Id,
        textPolicy,
        request.StyleIntent,
        preset.DefaultAspectRatio,
        width,
        height,
        preset.DefaultQualityBand,
        preset.DefaultOutputFormat,
        preset.DefaultBackgroundMode,
        preset.ReviewRubricTemplateId,
        draftCount: 2,
        finalCount: 1,
        $"Fake recommendation selected {preset.DisplayName} for {request.Goal}. Direction: {directionKey}.",
        confidence: directionKey.Equals("experimental", StringComparison.OrdinalIgnoreCase) ? 0.68 : 0.84,
        ["fake provider warning: verify real provider capabilities before generation"],
        [$"Refine style intent before final generation: {request.StyleIntent}".Trim()]);
}

private static string SelectPresetId(string goal)
{
    if (goal.Contains("poster", StringComparison.OrdinalIgnoreCase))
    {
        return ImageTypePresetCatalog.EducationalPoster;
    }

    if (goal.Contains("cover", StringComparison.OrdinalIgnoreCase))
    {
        return ImageTypePresetCatalog.ArticleCover;
    }

    if (goal.Contains("diagram", StringComparison.OrdinalIgnoreCase)
        || goal.Contains("concept", StringComparison.OrdinalIgnoreCase))
    {
        return ImageTypePresetCatalog.ConceptDiagram;
    }

    if (goal.Contains("social", StringComparison.OrdinalIgnoreCase)
        || goal.Contains("square", StringComparison.OrdinalIgnoreCase))
    {
        return ImageTypePresetCatalog.SocialSquare;
    }

    return ImageTypePresetCatalog.ArticleInlineIllustration;
}

private static (int Width, int Height) GetDefaultSize(AspectRatio aspectRatio)
{
    if (aspectRatio.WidthUnits == aspectRatio.HeightUnits)
    {
        return (1024, 1024);
    }

    return aspectRatio.WidthUnits > aspectRatio.HeightUnits
        ? (1536, 1024)
        : (1024, 1280);
}
```

- [x] **Step 5: Verify fake provider recommendation test passes**

Run:

```powershell
dotnet test --filter FakeTextPlanningProvider_CreatesPromptDirectionsForBrief
```

Expected: test passes.

- Commit fake provider slice.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs tests/ImageSeriesStudio.Tests/FakeProviderTests.cs
git commit -m "feat: 为假规划器添加推荐参数"
```

---

### Task 4: Persist Recommendations Through Application Service

**Files:**

- Modify: `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`
- Test: `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`

- [x] **Step 1: Write failing service workflow assertion**

In `ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion`, add these assertions after `loadedBrief` is assigned:

```csharp
var loadedDirection = loadedBrief.PromptDirections.Single();

Assert.NotNull(loadedDirection.Recommendation);
Assert.Equal(ImageTypePresetCatalog.ArticleInlineIllustration, loadedDirection.Recommendation.ImageTypePresetId);
Assert.Equal(1536, loadedDirection.Recommendation.Width);
Assert.Equal(1024, loadedDirection.Recommendation.Height);
Assert.Equal(ReviewRubricTemplateCatalog.EditorialIllustration, loadedDirection.Recommendation.ReviewRubricTemplateId);
```

- [x] **Step 2: Run the failing service test**

Run:

```powershell
dotnet test --filter ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion
```

Expected: fails because the application service does not pass draft recommendations into `PromptDirection.Create`.

- [x] **Step 3: Map recommendations into prompt directions**

In `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`, update the `PromptDirection.Create` call in `CreatePromptDirectionsAsync`:

```csharp
brief.ReplaceDirections(
    result.Directions
        .Select(direction => PromptDirection.Create(
            direction.Key,
            direction.Name,
            direction.IntendedUse,
            direction.PromptText,
            direction.NegativePrompt,
            direction.Strength,
            direction.Risk,
            timestamp,
            direction.Recommendation))
        .ToArray(),
    timestamp);
```

- [x] **Step 4: Verify service workflow test passes**

Run:

```powershell
dotnet test --filter ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion
```

Expected: test passes and persisted directions contain recommendation metadata.

- Commit service mapping slice.

Run:

```powershell
git add src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs
git commit -m "feat: 持久化提示词方向推荐"
```

---

### Task 5: Update Task Checklist

**Files:**

- Modify: `docs/TASKS.md`

- [x] **Step 1: Add preset governance phase**

Append this section after Phase 7 and before Phase 8 in `docs/TASKS.md`:

```markdown
## Phase 7A: Preset Governance

- [x] Record preset governance design spec.
- [x] Record preset governance implementation plan.
- [x] Add governance metadata to image type presets.
- [x] Add structured prompt direction recommendation model.
- [x] Add fake-first recommendation output.
- [x] Persist recommendations through the application service.
- [x] Add catalog invariant tests.
- [x] Run full build, test, and format gates for the preset governance slice.
```

- [x] **Step 2: Verify task checklist contains the new phase**

Run:

```powershell
rg -n "Phase 7A: Preset Governance|structured prompt direction recommendation" docs/TASKS.md
```

Expected: output includes the new phase heading and structured recommendation task.

- Commit checklist slice.

Run:

```powershell
git add docs/TASKS.md
git commit -m "docs: 更新预设治理任务清单"
```

---

### Task 6: Final Verification

**Files:**

- Review all modified source, tests, and docs from this plan.

- [x] **Step 1: Run full build**

Run:

```powershell
dotnet build
```

Expected: build succeeds with 0 errors.

- [x] **Step 2: Run full tests**

Run:

```powershell
dotnet test
```

Expected: all tests pass.

- [x] **Step 3: Run format verification**

Run:

```powershell
dotnet format --verify-no-changes
```

Expected: command succeeds without formatting changes.

- [x] **Step 4: Run focused placeholder scan**

Run:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOL[D]ER)" docs/superpowers/plans/2026-06-02-preset-governance.md src tests docs/TASKS.md
```

Expected: no unresolved placeholders in files touched by this plan.

- [x] **Step 5: Inspect git status**

Run:

```powershell
git status --short
```

Expected: no uncommitted changes from the preset governance slice. If unrelated user changes exist, leave them unstaged and report them.

- Commit final corrections if needed.

If formatting or documentation cleanup changes are produced by the verification steps, commit only the preset governance files:

```powershell
git add src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs src/ImageSeriesStudio.Core/Projects/PromptDirectionRecommendation.cs src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs tests/ImageSeriesStudio.Tests/PersistenceTests.cs tests/ImageSeriesStudio.Tests/FakeProviderTests.cs tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs docs/TASKS.md
git commit -m "chore: 通过预设治理门禁"
```

Expected: no commit is created if there are no final corrections.

## Self-Review

Spec coverage:

- Fixed, constrained, and free dimensions are covered by Task 1 catalog metadata and Task 2 recommendation model.
- AI recommendation overlay is covered by Task 3 fake planning and Task 4 application service persistence.
- Evidence-backed catalog expansion is represented by invariant tests and the task checklist, without adding new preset IDs.
- Provider capability warnings are covered by recommendation fields and fake provider warning output.
- Real provider integration remains guarded and outside this slice.

Gaps intentionally left for a separate plan:

- Full WPF recommendation editing and display.
- Real OpenAI structured recommendation output.
- A separate research evidence table under `docs/research/`.

Placeholder scan target:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOL[D]ER)" docs/superpowers/plans/2026-06-02-preset-governance.md
```
