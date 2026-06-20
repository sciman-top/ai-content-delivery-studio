# Document Illustration Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Build the first document-driven illustration planning workflow for markdown/text inputs using fake providers, strict review policies, and the existing image-series pipeline.

**Architecture:** Add document illustration concepts in the domain core, extend presets and rubrics, then route fake document planning through the existing application service into `SeriesItem` and `PromptVersion` records. The first implementation deliberately supports `markdown`, `text`, and `paste`; `docx` and `pdf` are represented in the enum but blocked by the application service until extraction is added in a separate slice.

**Tech Stack:** .NET 10, C#, WPF, MVVM Toolkit, EF Core SQLite, xUnit, fake providers, existing provider-neutral contracts.

**Historical note:** This plan predates the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename. Old code and test paths below are preserved as historical implementation context, not current repository truth.

---

## Scope Check

This plan implements one testable subsystem: document-driven illustration planning. It does not implement real OpenAI document analysis, binary `docx` or `pdf` extraction, data-chart generation, or export back into source documents.

The implementation produces working software on its own:

- User text can be converted to a document brief.
- Fake planner can propose illustration targets.
- Approved targets become existing plan rows and prompt rows.
- Scholarly strictness blocks fabricated evidence targets before queueing.

## File Structure

Create:

- `src/ImageSeriesStudio.Core/Documents/DocumentIllustration.cs`: domain concepts for document briefs, illustration plans, targets, strictness, and approval state.
- `tests/ImageSeriesStudio.Tests/DocumentIllustrationModelTests.cs`: validation and strictness tests for the new domain model.
- `tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs`: application-level tests for fake document planning and conversion to series items.

Modify:

- `src/ImageSeriesStudio.Core/ImageSeriesStudio.Core.csproj`: include the new domain namespace automatically through SDK-style compile.
- `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`: store document briefs and illustration plans under a project.
- `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`: map document briefs and illustration plans with JSON fields for list values and target collections.
- `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`: add document illustration presets.
- `src/ImageSeriesStudio.Core/Projects/ReviewRubricTemplates.cs`: add document-specific rubrics.
- `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`: add document illustration planning request and result records to the text planning provider contract.
- `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`: implement deterministic fake document planning.
- `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`: add document illustration use cases and approval-to-series conversion.
- `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`: add bilingual UI strings.
- `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`: add a minimal fake document illustration command and rows.
- `src/ImageSeriesStudio.App/MainWindow.xaml`: expose document illustration controls inside the inspector.
- Existing tests under `tests/ImageSeriesStudio.Tests/`: extend preset, rubric, fake provider, persistence, and localization tests.

## Task 1: Core Document Illustration Model

**Files:**

- Create: `src/ImageSeriesStudio.Core/Documents/DocumentIllustration.cs`
- Create: `tests/ImageSeriesStudio.Tests/DocumentIllustrationModelTests.cs`

- [x] **Step 1: Write failing domain tests**

Create `tests/ImageSeriesStudio.Tests/DocumentIllustrationModelTests.cs`:

```csharp
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Tests;

public sealed class DocumentIllustrationModelTests
{
    [Fact]
    public void DocumentBrief_Create_NormalizesListsAndRequiresTitle()
    {
        var projectId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Parse("2026-06-02T08:00:00Z");

        var brief = DocumentBrief.Create(
            projectId,
            DocumentSourceKind.Markdown,
            "article.md",
            "  Quantum teaching note  ",
            DocumentFamily.Educational,
            "teachers",
            [" intro ", " ", "Methods"],
            ["  Superposition needs a visual analogy.  "],
            ["cover image", "concept diagram"],
            ["avoid fake lab data"],
            IllustrationStrictnessLevel.Educational,
            timestamp);

        Assert.Equal(projectId, brief.ProjectId);
        Assert.Equal("Quantum teaching note", brief.Title);
        Assert.Equal(["intro", "Methods"], brief.Sections);
        Assert.Equal(["Superposition needs a visual analogy."], brief.KeyClaims);
        Assert.Equal(["cover image", "concept diagram"], brief.VisualOpportunities);
        Assert.Equal(["avoid fake lab data"], brief.KnownConstraints);

        Assert.Throws<ArgumentException>(() =>
            DocumentBrief.Create(
                projectId,
                DocumentSourceKind.Text,
                "source.txt",
                " ",
                DocumentFamily.Editorial,
                "readers",
                [],
                [],
                [],
                [],
                IllustrationStrictnessLevel.Editorial,
                timestamp));
    }

    [Fact]
    public void IllustrationPlan_ApprovesAndRejectsTargets()
    {
        var projectId = Guid.NewGuid();
        var briefId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Parse("2026-06-02T08:10:00Z");
        var target = IllustrationTarget.Create(
            briefId,
            "Opening cover",
            "section:intro",
            IllustrationPurpose.Cover,
            ["visualize the central idea"],
            ["do not show fake data"],
            ["intro paragraph 1"],
            ImageTypePresetCatalog.ArticleCover,
            "editorial-illustration",
            ImageTextPolicy.Hybrid,
            ["metaphor is allowed"],
            timestamp);

        var plan = IllustrationPlan.Create(
            projectId,
            briefId,
            "One cover target.",
            [target],
            ["intro is covered"],
            ["no data chart requested"],
            timestamp);

        var approved = plan.ApproveTarget(target.Id, timestamp.AddMinutes(1));

        Assert.Equal(IllustrationTargetApprovalState.Approved, approved.Targets.Single().ApprovalState);
        Assert.Single(approved.ApprovedTargets);

        var rejected = approved.RejectTarget(target.Id, timestamp.AddMinutes(2));
        Assert.Empty(rejected.ApprovedTargets);
        Assert.Equal(IllustrationTargetApprovalState.Rejected, rejected.Targets.Single().ApprovalState);
    }

    [Fact]
    public void ScholarlyTarget_BlocksFabricatedEvidencePurpose()
    {
        var briefId = Guid.NewGuid();
        var timestamp = DateTimeOffset.Parse("2026-06-02T08:20:00Z");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            IllustrationTarget.Create(
                briefId,
                "Microscope result",
                "section:results",
                IllustrationPurpose.ExperimentalEvidence,
                ["show microscopy evidence"],
                ["do not fabricate data"],
                ["results section"],
                "scholarly-schematic",
                "scholarly-schematic",
                ImageTextPolicy.DeterministicPostRender,
                ["scholarly_draft"],
                timestamp));

        Assert.Contains("Experimental evidence targets are blocked", exception.Message);
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter DocumentIllustrationModelTests
```

Expected: fails because `ImageSeriesStudio.Core.Documents` types do not exist.

- [x] **Step 3: Add the document illustration domain model**

Create `src/ImageSeriesStudio.Core/Documents/DocumentIllustration.cs`:

```csharp
using ImageSeriesStudio.Core.Styles;

namespace ImageSeriesStudio.Core.Documents;

public enum DocumentSourceKind
{
    Docx = 0,
    Pdf = 1,
    Markdown = 2,
    Text = 3,
    Paste = 4,
}

public enum DocumentFamily
{
    Editorial = 0,
    Educational = 1,
    ScholarlyDraft = 2,
}

public enum IllustrationStrictnessLevel
{
    Editorial = 0,
    Educational = 1,
    ScholarlyDraft = 2,
}

public enum IllustrationPurpose
{
    Cover = 0,
    InlineIllustration = 1,
    ConceptDiagram = 2,
    MechanismDiagram = 3,
    Timeline = 4,
    Comparison = 5,
    GraphicalAbstract = 6,
    BackgroundPlate = 7,
    ExperimentalEvidence = 8,
}

public enum IllustrationTargetApprovalState
{
    Draft = 0,
    Approved = 1,
    Rejected = 2,
}

public sealed record DocumentBrief
{
    private DocumentBrief(
        Guid id,
        Guid projectId,
        DocumentSourceKind sourceKind,
        string sourceDisplayName,
        string title,
        DocumentFamily documentFamily,
        string audience,
        IReadOnlyList<string> sections,
        IReadOnlyList<string> keyClaims,
        IReadOnlyList<string> visualOpportunities,
        IReadOnlyList<string> knownConstraints,
        IllustrationStrictnessLevel strictnessLevel,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        SourceKind = sourceKind;
        SourceDisplayName = sourceDisplayName;
        Title = title;
        DocumentFamily = documentFamily;
        Audience = audience;
        Sections = sections;
        KeyClaims = keyClaims;
        VisualOpportunities = visualOpportunities;
        KnownConstraints = knownConstraints;
        StrictnessLevel = strictnessLevel;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public DocumentSourceKind SourceKind { get; }
    public string SourceDisplayName { get; }
    public string Title { get; }
    public DocumentFamily DocumentFamily { get; }
    public string Audience { get; }
    public IReadOnlyList<string> Sections { get; }
    public IReadOnlyList<string> KeyClaims { get; }
    public IReadOnlyList<string> VisualOpportunities { get; }
    public IReadOnlyList<string> KnownConstraints { get; }
    public IllustrationStrictnessLevel StrictnessLevel { get; }
    public DateTimeOffset CreatedAt { get; }

    public static DocumentBrief Create(
        Guid projectId,
        DocumentSourceKind sourceKind,
        string sourceDisplayName,
        string title,
        DocumentFamily documentFamily,
        string audience,
        IReadOnlyList<string> sections,
        IReadOnlyList<string> keyClaims,
        IReadOnlyList<string> visualOpportunities,
        IReadOnlyList<string> knownConstraints,
        IllustrationStrictnessLevel strictnessLevel,
        DateTimeOffset createdAt)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        return new DocumentBrief(
            Guid.NewGuid(),
            projectId,
            sourceKind,
            RequireText(sourceDisplayName, nameof(sourceDisplayName)),
            RequireText(title, nameof(title)),
            documentFamily,
            RequireText(audience, nameof(audience)),
            NormalizeList(sections),
            NormalizeList(keyClaims),
            NormalizeList(visualOpportunities),
            NormalizeList(knownConstraints),
            strictnessLevel,
            createdAt);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record IllustrationPlan
{
    private IllustrationPlan(
        Guid id,
        Guid projectId,
        Guid documentBriefId,
        string summary,
        IReadOnlyList<IllustrationTarget> targets,
        IReadOnlyList<string> coverageNotes,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        ProjectId = projectId;
        DocumentBriefId = documentBriefId;
        Summary = summary;
        Targets = targets;
        CoverageNotes = coverageNotes;
        RiskNotes = riskNotes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public Guid DocumentBriefId { get; }
    public string Summary { get; }
    public IReadOnlyList<IllustrationTarget> Targets { get; }
    public IReadOnlyList<IllustrationTarget> ApprovedTargets => Targets
        .Where(target => target.ApprovalState is IllustrationTargetApprovalState.Approved)
        .ToArray();
    public IReadOnlyList<string> CoverageNotes { get; }
    public IReadOnlyList<string> RiskNotes { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static IllustrationPlan Create(
        Guid projectId,
        Guid documentBriefId,
        string summary,
        IReadOnlyList<IllustrationTarget> targets,
        IReadOnlyList<string> coverageNotes,
        IReadOnlyList<string> riskNotes,
        DateTimeOffset createdAt)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (documentBriefId == Guid.Empty)
        {
            throw new ArgumentException("Document brief id cannot be empty.", nameof(documentBriefId));
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one target is required.", nameof(targets));
        }

        return new IllustrationPlan(
            Guid.NewGuid(),
            projectId,
            documentBriefId,
            RequireText(summary, nameof(summary)),
            targets,
            NormalizeList(coverageNotes),
            NormalizeList(riskNotes),
            createdAt,
            createdAt);
    }

    public IllustrationPlan ApproveTarget(Guid targetId, DateTimeOffset timestamp)
    {
        return UpdateTarget(targetId, target => target.Approve(timestamp), timestamp);
    }

    public IllustrationPlan RejectTarget(Guid targetId, DateTimeOffset timestamp)
    {
        return UpdateTarget(targetId, target => target.Reject(timestamp), timestamp);
    }

    private IllustrationPlan UpdateTarget(
        Guid targetId,
        Func<IllustrationTarget, IllustrationTarget> update,
        DateTimeOffset timestamp)
    {
        if (!Targets.Any(target => target.Id == targetId))
        {
            throw new InvalidOperationException($"Illustration target not found: {targetId}");
        }

        return this with
        {
            Targets = Targets
                .Select(target => target.Id == targetId ? update(target) : target)
                .ToArray(),
            UpdatedAt = timestamp,
        };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record IllustrationTarget
{
    private IllustrationTarget(
        Guid id,
        Guid documentBriefId,
        string title,
        string documentLocation,
        IllustrationPurpose purpose,
        IReadOnlyList<string> mustShow,
        IReadOnlyList<string> mustNotShow,
        IReadOnlyList<string> sourceEvidence,
        string suggestedImageTypePresetId,
        string suggestedReviewRubricTemplateId,
        ImageTextPolicy textPolicy,
        IReadOnlyList<string> strictnessNotes,
        IllustrationTargetApprovalState approvalState,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        DocumentBriefId = documentBriefId;
        Title = title;
        DocumentLocation = documentLocation;
        Purpose = purpose;
        MustShow = mustShow;
        MustNotShow = mustNotShow;
        SourceEvidence = sourceEvidence;
        SuggestedImageTypePresetId = suggestedImageTypePresetId;
        SuggestedReviewRubricTemplateId = suggestedReviewRubricTemplateId;
        TextPolicy = textPolicy;
        StrictnessNotes = strictnessNotes;
        ApprovalState = approvalState;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid DocumentBriefId { get; }
    public string Title { get; }
    public string DocumentLocation { get; }
    public IllustrationPurpose Purpose { get; }
    public IReadOnlyList<string> MustShow { get; }
    public IReadOnlyList<string> MustNotShow { get; }
    public IReadOnlyList<string> SourceEvidence { get; }
    public string SuggestedImageTypePresetId { get; }
    public string SuggestedReviewRubricTemplateId { get; }
    public ImageTextPolicy TextPolicy { get; }
    public IReadOnlyList<string> StrictnessNotes { get; }
    public IllustrationTargetApprovalState ApprovalState { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static IllustrationTarget Create(
        Guid documentBriefId,
        string title,
        string documentLocation,
        IllustrationPurpose purpose,
        IReadOnlyList<string> mustShow,
        IReadOnlyList<string> mustNotShow,
        IReadOnlyList<string> sourceEvidence,
        string suggestedImageTypePresetId,
        string suggestedReviewRubricTemplateId,
        ImageTextPolicy textPolicy,
        IReadOnlyList<string> strictnessNotes,
        DateTimeOffset createdAt)
    {
        if (documentBriefId == Guid.Empty)
        {
            throw new ArgumentException("Document brief id cannot be empty.", nameof(documentBriefId));
        }

        if (purpose is IllustrationPurpose.ExperimentalEvidence)
        {
            throw new InvalidOperationException("Experimental evidence targets are blocked in document illustration planning.");
        }

        var normalizedEvidence = NormalizeRequiredList(sourceEvidence, nameof(sourceEvidence));

        return new IllustrationTarget(
            Guid.NewGuid(),
            documentBriefId,
            RequireText(title, nameof(title)),
            RequireText(documentLocation, nameof(documentLocation)),
            purpose,
            NormalizeRequiredList(mustShow, nameof(mustShow)),
            NormalizeList(mustNotShow),
            normalizedEvidence,
            RequireText(suggestedImageTypePresetId, nameof(suggestedImageTypePresetId)),
            RequireText(suggestedReviewRubricTemplateId, nameof(suggestedReviewRubricTemplateId)),
            textPolicy,
            NormalizeList(strictnessNotes),
            IllustrationTargetApprovalState.Draft,
            createdAt,
            createdAt);
    }

    public IllustrationTarget Approve(DateTimeOffset timestamp)
    {
        return this with { ApprovalState = IllustrationTargetApprovalState.Approved, UpdatedAt = timestamp };
    }

    public IllustrationTarget Reject(DateTimeOffset timestamp)
    {
        return this with { ApprovalState = IllustrationTargetApprovalState.Rejected, UpdatedAt = timestamp };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeRequiredList(IReadOnlyList<string> values, string parameterName)
    {
        var normalized = NormalizeList(values);
        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run:

```powershell
dotnet test --filter DocumentIllustrationModelTests
```

Expected: all `DocumentIllustrationModelTests` pass.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Documents/DocumentIllustration.cs tests/ImageSeriesStudio.Tests/DocumentIllustrationModelTests.cs
git commit -m "feat: 增加文稿配图领域模型"
```

## Task 2: Presets And Rubrics For Document Images

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`
- Modify: `src/ImageSeriesStudio.Core/Projects/ReviewRubricTemplates.cs`
- Modify: `tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs`
- Modify: `tests/ImageSeriesStudio.Tests/ReviewRubricTemplateTests.cs`

- [x] **Step 1: Write failing catalog tests**

Append this test to `ImageTypePresetTests`:

```csharp
[Fact]
public void Catalog_IncludesDocumentIllustrationPresets()
{
    var presets = ImageTypePresetCatalog.Defaults.Select(preset => preset.Id).ToArray();

    Assert.Contains(ImageTypePresetCatalog.ArticleInlineIllustration, presets);
    Assert.Contains(ImageTypePresetCatalog.ConceptDiagram, presets);
    Assert.Contains(ImageTypePresetCatalog.GraphicalAbstract, presets);
    Assert.Contains(ImageTypePresetCatalog.ScholarlySchematic, presets);

    var scholarly = ImageTypePresetCatalog.GetById(ImageTypePresetCatalog.ScholarlySchematic);
    Assert.Equal(ImageTextPolicy.DeterministicPostRender, scholarly.TextPolicy);
    Assert.Equal(ReviewRubricTemplateCatalog.ScholarlySchematic, scholarly.ReviewRubricTemplateId);
}
```

Append this test to `ReviewRubricTemplateTests`:

```csharp
[Fact]
public void Catalog_IncludesDocumentIllustrationRubrics()
{
    var templates = ReviewRubricTemplateCatalog.All.Select(template => template.Id).ToArray();

    Assert.Contains(ReviewRubricTemplateCatalog.EditorialIllustration, templates);
    Assert.Contains(ReviewRubricTemplateCatalog.EducationalAccuracy, templates);
    Assert.Contains(ReviewRubricTemplateCatalog.ScholarlySchematic, templates);

    var scholarly = ReviewRubricTemplateCatalog.GetById(ReviewRubricTemplateCatalog.ScholarlySchematic);
    Assert.Contains(scholarly.Dimensions, dimension => dimension.Name == "no_fake_evidence");
    Assert.Contains(scholarly.Dimensions, dimension => dimension.Name == "source_evidence_fit");
}
```

- [x] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test --filter "ImageTypePresetTests|ReviewRubricTemplateTests"
```

Expected: fails because the new constants and catalog entries do not exist.

- [x] **Step 3: Add image type preset constants and entries**

Modify `ImageTypePresetCatalog` in `src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs`:

```csharp
public const string ArticleInlineIllustration = "article-inline-illustration";

public const string ConceptDiagram = "concept-diagram";

public const string GraphicalAbstract = "graphical-abstract";

public const string ScholarlySchematic = "scholarly-schematic";
```

Add these entries after `ArticleCover` in `Presets`:

```csharp
ImageTypePreset.Create(
    ArticleInlineIllustration,
    "Article inline illustration",
    "Inline article illustration tied to a specific section or claim.",
    new AspectRatio(16, 9),
    "png",
    ImageTextPolicy.Hybrid,
    ReviewRubricTemplateCatalog.EditorialIllustration,
    "{series}/inline-{item-number}-{item-slug}"),
ImageTypePreset.Create(
    ConceptDiagram,
    "Concept diagram",
    "Educational concept diagram with clear structure and deterministic label support.",
    new AspectRatio(16, 9),
    "png",
    ImageTextPolicy.DeterministicPostRender,
    ReviewRubricTemplateCatalog.EducationalAccuracy,
    "{series}/concept-{item-number}-{item-slug}"),
ImageTypePreset.Create(
    GraphicalAbstract,
    "Graphical abstract",
    "Schematic graphical abstract for a scholarly or educational draft.",
    new AspectRatio(16, 9),
    "png",
    ImageTextPolicy.DeterministicPostRender,
    ReviewRubricTemplateCatalog.ScholarlySchematic,
    "{series}/graphical-abstract-{item-slug}"),
ImageTypePreset.Create(
    ScholarlySchematic,
    "Scholarly schematic",
    "Concept-level scholarly schematic that avoids fabricated evidence imagery.",
    new AspectRatio(16, 9),
    "png",
    ImageTextPolicy.DeterministicPostRender,
    ReviewRubricTemplateCatalog.ScholarlySchematic,
    "{series}/schematic-{item-number}-{item-slug}"),
```

- [x] **Step 4: Add rubric constants and entries**

Modify `ReviewRubricTemplateCatalog` in `src/ImageSeriesStudio.Core/Projects/ReviewRubricTemplates.cs`:

```csharp
public const string EditorialIllustration = "editorial-illustration";

public const string EducationalAccuracy = "educational-accuracy";

public const string ScholarlySchematic = "scholarly-schematic";
```

Add these templates after `GeneralImage`:

```csharp
new ReviewRubricTemplate(
    EditorialIllustration,
    "Editorial illustration",
    "Rubric for article covers and inline editorial images.",
    [
        new ReviewRubricDimensionTemplate("requirement_match", "Candidate should match the document-backed illustration target.", 3),
        new ReviewRubricDimensionTemplate("source_evidence_fit", "Candidate should fit the cited document evidence without adding unsupported claims.", 3),
        new ReviewRubricDimensionTemplate("visual_hierarchy", "Composition should be clear for article reading context.", 2),
        new ReviewRubricDimensionTemplate("delivery_readiness", "Output should fit the intended article slot and audience.", 1),
    ]),
new ReviewRubricTemplate(
    EducationalAccuracy,
    "Educational accuracy",
    "Rubric for educational diagrams and explanatory illustrations.",
    [
        new ReviewRubricDimensionTemplate("concept_accuracy", "Candidate should preserve the correct concept relationship.", 4),
        new ReviewRubricDimensionTemplate("source_evidence_fit", "Candidate should stay within the source document evidence.", 3),
        new ReviewRubricDimensionTemplate("text_policy", "Layout should support deterministic labels, formulas, and legends.", 3),
        new ReviewRubricDimensionTemplate("diagram_clarity", "Visual structure should be easy to teach from.", 2),
    ]),
new ReviewRubricTemplate(
    ScholarlySchematic,
    "Scholarly schematic",
    "Rubric for schematic scholarly draft visuals that must avoid fake evidence.",
    [
        new ReviewRubricDimensionTemplate("no_fake_evidence", "Candidate must not imply real experimental, clinical, archival, or field evidence.", 5),
        new ReviewRubricDimensionTemplate("source_evidence_fit", "Candidate should clearly derive from the cited source section.", 4),
        new ReviewRubricDimensionTemplate("schematic_clarity", "Candidate should read as a schematic or concept-level visual.", 3),
        new ReviewRubricDimensionTemplate("text_policy", "Required labels should be reserved for deterministic composition.", 2),
    ]),
```

- [x] **Step 5: Run tests to verify they pass**

Run:

```powershell
dotnet test --filter "ImageTypePresetTests|ReviewRubricTemplateTests"
```

Expected: all selected tests pass.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Styles/ImageTypePreset.cs src/ImageSeriesStudio.Core/Projects/ReviewRubricTemplates.cs tests/ImageSeriesStudio.Tests/ImageTypePresetTests.cs tests/ImageSeriesStudio.Tests/ReviewRubricTemplateTests.cs
git commit -m "feat: 增加文稿配图预设和评审规则"
```

## Task 3: Provider Contract And Fake Document Planner

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`
- Modify: `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`

- [x] **Step 1: Write failing fake provider test**

Append this test to `FakeProviderTests`:

```csharp
[Fact]
public async Task FakeTextPlanningProvider_CreatesDocumentIllustrationPlan()
{
    var provider = new FakeTextPlanningProvider();

    var result = await provider.CreateDocumentIllustrationPlanAsync(
        new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction", "Classroom analogy"],
            ["Superposition needs a visual analogy."],
            ["avoid fake lab data"]),
        CancellationToken.None);

    Assert.Equal("fake-document-plan", result.ProviderTraceId);
    Assert.Equal(DocumentFamily.Educational, result.Brief.DocumentFamily);
    Assert.NotEmpty(result.Plan.Targets);
    Assert.All(result.Plan.Targets, target => Assert.NotEmpty(target.SourceEvidence));
    Assert.Contains(result.Plan.Targets, target => target.Purpose == IllustrationPurpose.ConceptDiagram);
}
```

Add these usings to `FakeProviderTests`:

```csharp
using ImageSeriesStudio.Core.Documents;
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter FakeTextPlanningProvider_CreatesDocumentIllustrationPlan
```

Expected: fails because the provider contract does not expose document illustration planning.

- [x] **Step 3: Extend text planning provider contract**

Modify `ITextPlanningProvider` in `AiProviderContracts.cs`:

```csharp
Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
    DocumentIllustrationPlanningRequest request,
    CancellationToken cancellationToken);
```

Add these records to `AiProviderContracts.cs`:

```csharp
using ImageSeriesStudio.Core.Documents;
```

```csharp
public sealed record DocumentIllustrationPlanningRequest(
    string Title,
    string SourceText,
    string Audience,
    DocumentFamily DocumentFamily,
    IllustrationStrictnessLevel StrictnessLevel,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> KeyClaims,
    IReadOnlyList<string> KnownConstraints);

public sealed record DocumentIllustrationPlanningResult(
    DocumentBrief Brief,
    IllustrationPlan Plan,
    string ProviderTraceId);
```

- [x] **Step 4: Implement fake document planning**

Add this method to `FakeTextPlanningProvider`:

```csharp
public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
    DocumentIllustrationPlanningRequest request,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var timestamp = DateTimeOffset.UtcNow;
    var projectId = Guid.Empty;
    var brief = DocumentBrief.Create(
        projectId == Guid.Empty ? Guid.NewGuid() : projectId,
        DocumentSourceKind.Paste,
        $"{request.Title.Trim()}.txt",
        request.Title,
        request.DocumentFamily,
        request.Audience,
        request.Sections,
        request.KeyClaims,
        request.Sections.Select(section => $"Illustrate {section}").ToArray(),
        request.KnownConstraints,
        request.StrictnessLevel,
        timestamp);

    var target = request.StrictnessLevel is IllustrationStrictnessLevel.ScholarlyDraft
        ? IllustrationTarget.Create(
            brief.Id,
            "Graphical abstract schematic",
            "document:summary",
            IllustrationPurpose.GraphicalAbstract,
            ["summarize the central concept as a schematic"],
            ["do not imply real experimental evidence"],
            request.KeyClaims.Count == 0 ? [request.SourceText] : request.KeyClaims,
            "graphical-abstract",
            "scholarly-schematic",
            ImageTextPolicy.DeterministicPostRender,
            ["scholarly draft mode"],
            timestamp)
        : IllustrationTarget.Create(
            brief.Id,
            "Concept illustration",
            "document:introduction",
            IllustrationPurpose.ConceptDiagram,
            ["explain the central idea visually"],
            request.KnownConstraints,
            request.KeyClaims.Count == 0 ? [request.SourceText] : request.KeyClaims,
            "concept-diagram",
            "educational-accuracy",
            ImageTextPolicy.DeterministicPostRender,
            ["fake provider"],
            timestamp);

    var plan = IllustrationPlan.Create(
        brief.ProjectId,
        brief.Id,
        $"Fake document illustration plan for {request.Title.Trim()}.",
        [target],
        ["Central concept covered."],
        request.KnownConstraints,
        timestamp);

    return Task.FromResult(new DocumentIllustrationPlanningResult(brief, plan, "fake-document-plan"));
}
```

Add this using to `FakeProviders.cs`:

```csharp
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Styles;
```

- [x] **Step 5: Update OpenAI text provider to keep compile safety**

Modify `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs` by adding a method with the same signature. The first implementation should throw an explicit opt-in message:

```csharp
public Task<DocumentIllustrationPlanningResult> CreateDocumentIllustrationPlanAsync(
    DocumentIllustrationPlanningRequest request,
    CancellationToken cancellationToken)
{
    throw new InvalidOperationException("OpenAI document illustration planning is not enabled in the first fake-provider implementation.");
}
```

Add this using:

```csharp
using ImageSeriesStudio.Core.Documents;
```

- [x] **Step 6: Run tests to verify they pass**

Run:

```powershell
dotnet test --filter FakeProviderTests
```

Expected: all `FakeProviderTests` pass.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs tests/ImageSeriesStudio.Tests/FakeProviderTests.cs
git commit -m "feat: 增加假文稿配图规划 Provider"
```

## Task 4: Application Workflow From Document Plan To Series Items

**Files:**

- Modify: `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`
- Create: `tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs`

- [x] **Step 1: Write failing application workflow test**

Create `tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs`:

```csharp
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Infrastructure.Fakes;

namespace ImageSeriesStudio.Tests;

public sealed class DocumentIllustrationWorkflowTests
{
    [Fact]
    public async Task CreateDocumentIllustrationPlanWithProvider_AddsApprovedTargetsToProject()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectApplicationService(repository, new FakeTextPlanningProvider());
        var timestamp = DateTimeOffset.Parse("2026-06-02T09:00:00Z");
        var project = await service.CreateProjectAsync("Document demo", timestamp, CancellationToken.None);

        var result = await service.CreateDocumentIllustrationPlanWithProviderAsync(
            project.Id,
            new DocumentIllustrationPlanningRequest(
                "Teaching note",
                "Teachers need a clear concept diagram.",
                "teachers",
                DocumentFamily.Educational,
                IllustrationStrictnessLevel.Educational,
                ["Introduction"],
                ["A concept diagram should explain the central idea."],
                ["avoid fake lab data"]),
            approveAllTargets: true,
            timestamp.AddMinutes(1),
            CancellationToken.None);

        var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
        var series = Assert.Single(loaded!.Series);
        var item = Assert.Single(series.Items);
        var prompt = Assert.Single(item.PromptVersions);

        Assert.Equal(result.SeriesId, series.Id);
        Assert.Contains("Teaching note", series.Title);
        Assert.Contains("source evidence", item.Brief, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not imply", prompt.PromptText, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, ImageSeriesStudio.Core.Projects.ImageProject> _projects = [];

        public Task SaveAsync(ImageSeriesStudio.Core.Projects.ImageProject project, CancellationToken cancellationToken)
        {
            _projects[project.Id] = project;
            return Task.CompletedTask;
        }

        public Task<ImageSeriesStudio.Core.Projects.ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
        {
            _projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListProjectsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProjectSummary>>([]);
        }

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProjectSummary>>([]);
        }
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter DocumentIllustrationWorkflowTests
```

Expected: fails because `CreateDocumentIllustrationPlanWithProviderAsync` does not exist.

- [x] **Step 3: Add application workflow result record**

Add this record near the other public records at the bottom of `ProjectApplicationService.cs`:

```csharp
public sealed record DocumentIllustrationWorkflowResult(
    Guid DocumentBriefId,
    Guid IllustrationPlanId,
    Guid? SeriesId,
    int ApprovedTargetCount);
```

- [x] **Step 4: Add the document illustration workflow method**

Add this method to `ProjectApplicationService`:

```csharp
public async Task<DocumentIllustrationWorkflowResult> CreateDocumentIllustrationPlanWithProviderAsync(
    Guid projectId,
    DocumentIllustrationPlanningRequest request,
    bool approveAllTargets,
    DateTimeOffset timestamp,
    CancellationToken cancellationToken)
{
    if (_textPlanningProvider is null)
    {
        throw new InvalidOperationException("Text planning provider is not registered.");
    }

    if (request.DocumentFamily is DocumentFamily.ScholarlyDraft
        && request.KnownConstraints.Any(value => value.Contains("fake evidence", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Scholarly draft planning cannot request fake evidence imagery.");
    }

    var project = await RequireProjectAsync(projectId, cancellationToken);
    var providerResult = await _textPlanningProvider.CreateDocumentIllustrationPlanAsync(request, cancellationToken);
    var plan = approveAllTargets
        ? providerResult.Plan.Targets.Aggregate(
            providerResult.Plan,
            (current, target) => current.ApproveTarget(target.Id, timestamp))
        : providerResult.Plan;

    Guid? seriesId = null;
    if (approveAllTargets && plan.ApprovedTargets.Count > 0)
    {
        var providerProfile = ResolveProviderProfile(project, providerProfileId: null, timestamp);
        var series = project.AddSeries(
            $"Document illustrations: {providerResult.Brief.Title}",
            plan.Summary,
            timestamp);
        seriesId = series.Id;

        foreach (var target in plan.ApprovedTargets)
        {
            var item = series.AddItem(
                target.Title,
                BuildDocumentTargetBrief(target),
                timestamp);
            item.AddPromptVersion(
                BuildDocumentTargetPrompt(target),
                CreateDefaultGenerationSettings(),
                providerProfile.Id,
                timestamp);
        }
    }

    await _repository.SaveAsync(project, cancellationToken);
    return new DocumentIllustrationWorkflowResult(
        providerResult.Brief.Id,
        plan.Id,
        seriesId,
        plan.ApprovedTargets.Count);
}
```

Add these helper methods to `ProjectApplicationService`:

```csharp
private static string BuildDocumentTargetBrief(IllustrationTarget target)
{
    return string.Join(
        Environment.NewLine,
        [
            $"Purpose: {target.Purpose}",
            $"Location: {target.DocumentLocation}",
            $"Must show: {string.Join("; ", target.MustShow)}",
            $"Must not show: {string.Join("; ", target.MustNotShow)}",
            $"Source evidence: {string.Join("; ", target.SourceEvidence)}",
            $"Strictness: {string.Join("; ", target.StrictnessNotes)}",
        ]);
}

private static string BuildDocumentTargetPrompt(IllustrationTarget target)
{
    return string.Join(
        Environment.NewLine,
        [
            $"Create a {target.Purpose} for a document illustration workflow.",
            $"Must show: {string.Join("; ", target.MustShow)}",
            $"Must not show: {string.Join("; ", target.MustNotShow)}",
            $"Use this source evidence: {string.Join("; ", target.SourceEvidence)}",
            "Do not imply real experimental, clinical, archival, or field evidence unless the user provided that evidence explicitly.",
            $"Text policy: {target.TextPolicy}.",
        ]);
}
```

Add these usings:

```csharp
using ImageSeriesStudio.Core.Documents;
```

- [x] **Step 5: Run tests to verify they pass**

Run:

```powershell
dotnet test --filter DocumentIllustrationWorkflowTests
```

Expected: all `DocumentIllustrationWorkflowTests` pass.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs
git commit -m "feat: 接入文稿配图应用工作流"
```

## Task 5: Persistence Mapping For Document Planning Evidence

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`

- [x] **Step 1: Write failing persistence test**

Append this test to `PersistenceTests`:

```csharp
[Fact]
public async Task PersistsDocumentBriefsAndIllustrationPlans()
{
    var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
    var databasePath = Path.Combine(databaseDirectory, "document-illustration.sqlite");
    Directory.CreateDirectory(databaseDirectory);

    try
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        var timestamp = DateTimeOffset.Parse("2026-06-02T10:00:00Z");
        var project = ImageProject.Create("Document persistence", timestamp);
        var brief = DocumentBrief.Create(
            project.Id,
            DocumentSourceKind.Markdown,
            "article.md",
            "Article",
            DocumentFamily.Editorial,
            "readers",
            ["intro"],
            ["central claim"],
            ["cover"],
            ["avoid fake data"],
            IllustrationStrictnessLevel.Editorial,
            timestamp);
        var target = IllustrationTarget.Create(
            brief.Id,
            "Cover",
            "intro",
            IllustrationPurpose.Cover,
            ["central claim"],
            ["fake data"],
            ["central claim"],
            ImageTypePresetCatalog.ArticleCover,
            ReviewRubricTemplateCatalog.EditorialIllustration,
            ImageTextPolicy.Hybrid,
            ["editorial"],
            timestamp);
        var plan = IllustrationPlan.Create(
            project.Id,
            brief.Id,
            "Cover plan",
            [target],
            ["intro covered"],
            ["no data chart"],
            timestamp);
        project.AddDocumentBrief(brief, timestamp);
        project.AddIllustrationPlan(plan, timestamp);

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Projects.Add(project);
            await setup.SaveChangesAsync();
        }

        await using (var read = new AppDbContext(options))
        {
            var loaded = await read.Projects
                .Include(value => value.DocumentBriefs)
                .Include(value => value.IllustrationPlans)
                .SingleAsync();

            Assert.Single(loaded.DocumentBriefs);
            Assert.Single(loaded.IllustrationPlans);
            Assert.Equal("Article", loaded.DocumentBriefs.Single().Title);
            Assert.Equal("Cover plan", loaded.IllustrationPlans.Single().Summary);
        }
    }
    finally
    {
        if (Directory.Exists(databaseDirectory))
        {
            Directory.Delete(databaseDirectory, recursive: true);
        }
    }
}
```

Add these usings to `PersistenceTests`:

```csharp
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Styles;
```

- [x] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test --filter PersistsDocumentBriefsAndIllustrationPlans
```

Expected: fails because `ImageProject` does not expose document brief or illustration plan collections.

- [x] **Step 3: Add document collections to project model**

Modify `ImageProject` in `ProjectModel.cs`:

```csharp
private readonly List<DocumentBrief> _documentBriefs = [];
private readonly List<IllustrationPlan> _illustrationPlans = [];
```

Add public collections:

```csharp
public IReadOnlyCollection<DocumentBrief> DocumentBriefs => _documentBriefs.AsReadOnly();

public IReadOnlyCollection<IllustrationPlan> IllustrationPlans => _illustrationPlans.AsReadOnly();
```

Add methods:

```csharp
public void AddDocumentBrief(DocumentBrief brief, DateTimeOffset timestamp)
{
    ArgumentNullException.ThrowIfNull(brief);
    if (brief.ProjectId != Id)
    {
        throw new InvalidOperationException("Document brief belongs to a different project.");
    }

    _documentBriefs.Add(brief);
    UpdatedAt = timestamp;
}

public void AddIllustrationPlan(IllustrationPlan plan, DateTimeOffset timestamp)
{
    ArgumentNullException.ThrowIfNull(plan);
    if (!_documentBriefs.Any(brief => brief.Id == plan.DocumentBriefId))
    {
        throw new InvalidOperationException("Illustration plan requires a document brief in the same project.");
    }

    _illustrationPlans.Add(plan);
    UpdatedAt = timestamp;
}
```

Add this using:

```csharp
using ImageSeriesStudio.Core.Documents;
```

- [x] **Step 4: Map document entities in EF Core**

Modify `AppDbContext.cs`:

```csharp
public DbSet<DocumentBrief> DocumentBriefs => Set<DocumentBrief>();

public DbSet<IllustrationPlan> IllustrationPlans => Set<IllustrationPlan>();
```

Add this using:

```csharp
using ImageSeriesStudio.Core.Documents;
```

Add entity mapping in `OnModelCreating`:

```csharp
modelBuilder.Entity<DocumentBrief>(entity =>
{
    entity.HasKey(brief => brief.Id);
    entity.Property(brief => brief.SourceDisplayName).IsRequired();
    entity.Property(brief => brief.Title).IsRequired();
    entity.Property(brief => brief.Audience).IsRequired();
    entity.Property(brief => brief.Sections)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(brief => brief.KeyClaims)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(brief => brief.VisualOpportunities)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(brief => brief.KnownConstraints)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
});

modelBuilder.Entity<IllustrationPlan>(entity =>
{
    entity.HasKey(plan => plan.Id);
    entity.Property(plan => plan.Summary).IsRequired();
    entity.Property(plan => plan.Targets)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<IllustrationTarget>>(json, JsonOptions) ?? new List<IllustrationTarget>());
    entity.Property(plan => plan.CoverageNotes)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(plan => plan.RiskNotes)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
});
```

Extend the `ImageProject` mapping:

```csharp
entity.HasMany(project => project.DocumentBriefs)
    .WithOne()
    .HasForeignKey(brief => brief.ProjectId)
    .OnDelete(DeleteBehavior.Cascade);
entity.HasMany(project => project.IllustrationPlans)
    .WithOne()
    .HasForeignKey(plan => plan.ProjectId)
    .OnDelete(DeleteBehavior.Cascade);
entity.Navigation(project => project.DocumentBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
entity.Navigation(project => project.IllustrationPlans).UsePropertyAccessMode(PropertyAccessMode.Field);
```

`IllustrationPlan.ProjectId` is part of the Task 1 model, so the project relationship is explicit before EF mapping begins.

- [x] **Step 5: Run persistence test**

Run:

```powershell
dotnet test --filter PersistsDocumentBriefsAndIllustrationPlans
```

Expected: selected persistence test passes.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Core/Projects/ProjectModel.cs src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs tests/ImageSeriesStudio.Tests/PersistenceTests.cs
git commit -m "feat: 持久化文稿配图规划证据"
```

## Task 6: Minimal WPF Entry Point For Fake Document Planning

**Files:**

- Modify: `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`
- Modify: `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ImageSeriesStudio.App/MainWindow.xaml`
- Modify: `tests/ImageSeriesStudio.Tests/LocalizationTests.cs`

- [x] **Step 1: Write failing localization test**

Append this assertion group to the existing localization coverage:

```csharp
Assert.NotEmpty(chinese.GetText(LocalizationKey.DocumentIllustrationTitle));
Assert.NotEmpty(chinese.GetText(LocalizationKey.DocumentSourceText));
Assert.NotEmpty(chinese.GetText(LocalizationKey.RunFakeDocumentPlanning));
Assert.NotEmpty(english.GetText(LocalizationKey.DocumentIllustrationTitle));
Assert.NotEmpty(english.GetText(LocalizationKey.DocumentSourceText));
Assert.NotEmpty(english.GetText(LocalizationKey.RunFakeDocumentPlanning));
```

- [x] **Step 2: Run localization test to verify it fails**

Run:

```powershell
dotnet test --filter LocalizationTests
```

Expected: fails because the new localization keys do not exist.

- [x] **Step 3: Add localization keys and strings**

Add enum members:

```csharp
DocumentIllustrationTitle,
DocumentSourceText,
DocumentAudience,
DocumentStrictness,
RunFakeDocumentPlanning,
DocumentPlanningResult,
```

Add English strings:

```csharp
[LocalizationKey.DocumentIllustrationTitle] = "Document illustration",
[LocalizationKey.DocumentSourceText] = "Document text",
[LocalizationKey.DocumentAudience] = "Document audience",
[LocalizationKey.DocumentStrictness] = "Strictness",
[LocalizationKey.RunFakeDocumentPlanning] = "Run fake document planning",
[LocalizationKey.DocumentPlanningResult] = "Document illustration targets were added to the plan.",
```

Add Chinese strings:

```csharp
[LocalizationKey.DocumentIllustrationTitle] = "文稿配图",
[LocalizationKey.DocumentSourceText] = "文稿文本",
[LocalizationKey.DocumentAudience] = "文稿受众",
[LocalizationKey.DocumentStrictness] = "严谨性",
[LocalizationKey.RunFakeDocumentPlanning] = "运行假文稿配图规划",
[LocalizationKey.DocumentPlanningResult] = "文稿配图目标已加入计划。",
```

- [x] **Step 4: Add view-model state and command**

In `MainWindowViewModel.cs`, add backing fields:

```csharp
private string _documentIllustrationTitle = string.Empty;
private string _documentSourceTextLabel = string.Empty;
private string _documentAudienceLabel = string.Empty;
private string _documentStrictnessLabel = string.Empty;
private string _runFakeDocumentPlanningText = string.Empty;
private string _newDocumentSourceText = "Teachers need a clear concept diagram for the central idea.";
private string _newDocumentAudience = "teachers";
private IllustrationStrictnessLevel _selectedDocumentStrictness = IllustrationStrictnessLevel.Educational;
```

Add properties:

```csharp
public string DocumentIllustrationTitle
{
    get => _documentIllustrationTitle;
    private set => SetProperty(ref _documentIllustrationTitle, value);
}

public string DocumentSourceTextLabel
{
    get => _documentSourceTextLabel;
    private set => SetProperty(ref _documentSourceTextLabel, value);
}

public string DocumentAudienceLabel
{
    get => _documentAudienceLabel;
    private set => SetProperty(ref _documentAudienceLabel, value);
}

public string DocumentStrictnessLabel
{
    get => _documentStrictnessLabel;
    private set => SetProperty(ref _documentStrictnessLabel, value);
}

public string RunFakeDocumentPlanningText
{
    get => _runFakeDocumentPlanningText;
    private set => SetProperty(ref _runFakeDocumentPlanningText, value);
}

public string NewDocumentSourceText
{
    get => _newDocumentSourceText;
    set
    {
        if (SetProperty(ref _newDocumentSourceText, value))
        {
            RunFakeDocumentPlanningCommand.NotifyCanExecuteChanged();
        }
    }
}

public string NewDocumentAudience
{
    get => _newDocumentAudience;
    set => SetProperty(ref _newDocumentAudience, value);
}

public IReadOnlyList<IllustrationStrictnessLevel> DocumentStrictnessOptions { get; } =
[
    IllustrationStrictnessLevel.Editorial,
    IllustrationStrictnessLevel.Educational,
    IllustrationStrictnessLevel.ScholarlyDraft,
];

public IllustrationStrictnessLevel SelectedDocumentStrictness
{
    get => _selectedDocumentStrictness;
    set => SetProperty(ref _selectedDocumentStrictness, value);
}
```

Add command:

```csharp
[RelayCommand(CanExecute = nameof(CanRunFakeDocumentPlanning))]
private async Task RunFakeDocumentPlanningAsync()
{
    if (SelectedProject is null)
    {
        return;
    }

    var family = SelectedDocumentStrictness switch
    {
        IllustrationStrictnessLevel.ScholarlyDraft => DocumentFamily.ScholarlyDraft,
        IllustrationStrictnessLevel.Editorial => DocumentFamily.Editorial,
        _ => DocumentFamily.Educational,
    };

    await _projectService.CreateDocumentIllustrationPlanWithProviderAsync(
        SelectedProject.Id,
        new DocumentIllustrationPlanningRequest(
            SelectedProject.Name,
            NewDocumentSourceText,
            NewDocumentAudience,
            family,
            SelectedDocumentStrictness,
            ["Imported text"],
            [NewDocumentSourceText],
            ["do not imply fake evidence"]),
        approveAllTargets: true,
        DateTimeOffset.UtcNow,
        CancellationToken.None);

    await RefreshProjectsAsync();
    ActivityItems = [Text(LocalizationKey.DocumentPlanningResult), .. ActivityItems];
}

private bool CanRunFakeDocumentPlanning()
{
    return SelectedProject is not null && !string.IsNullOrWhiteSpace(NewDocumentSourceText);
}
```

Add usings:

```csharp
using ImageSeriesStudio.Core.Documents;
```

- [x] **Step 5: Bind controls in the inspector**

In `MainWindow.xaml`, place this block near the existing fake planning controls:

```xml
<Separator Margin="0,18,0,14" />

<TextBlock Text="{Binding DocumentIllustrationTitle}" FontWeight="SemiBold" Margin="0,0,0,8" />
<TextBlock Text="{Binding DocumentSourceTextLabel}" Margin="0,0,0,4" />
<TextBox Text="{Binding NewDocumentSourceText, UpdateSourceTrigger=PropertyChanged}"
         AcceptsReturn="True"
         TextWrapping="Wrap"
         MinHeight="88"
         Margin="0,0,0,6" />
<TextBlock Text="{Binding DocumentAudienceLabel}" Margin="0,0,0,4" />
<TextBox Text="{Binding NewDocumentAudience, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,6" />
<TextBlock Text="{Binding DocumentStrictnessLabel}" Margin="0,0,0,4" />
<ComboBox ItemsSource="{Binding DocumentStrictnessOptions}"
          SelectedItem="{Binding SelectedDocumentStrictness, Mode=TwoWay}"
          MinHeight="28"
          Margin="0,0,0,8" />
<Button Content="{Binding RunFakeDocumentPlanningText}"
        Command="{Binding RunFakeDocumentPlanningCommand}"
        HorizontalAlignment="Stretch"
        Margin="0,0,0,14" />
```

- [x] **Step 6: Run tests and build**

Run:

```powershell
dotnet test --filter LocalizationTests
dotnet build
```

Expected: localization tests pass and build succeeds.

- Commit.

Run:

```powershell
git add src/ImageSeriesStudio.Application/Localization/LocalizationService.cs src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs src/ImageSeriesStudio.App/MainWindow.xaml tests/ImageSeriesStudio.Tests/LocalizationTests.cs
git commit -m "feat: 增加文稿配图假规划入口"
```

## Task 7: Gates, Documentation, And Handoff

**Files:**

- Modify: `docs/TASKS.md`
- Modify: `docs/USER_GUIDE.md`

- [x] **Step 1: Update task checklist**

Add this section to `docs/TASKS.md`:

```markdown
## Phase 7: Document Illustration Workflow

- [x] Add document illustration design spec.
- [x] Add implementation plan.
- [x] Add document illustration domain model.
- [x] Add article, concept, graphical abstract, and scholarly schematic presets.
- [x] Add document-specific review rubrics.
- [x] Add fake document illustration planner.
- [x] Add application workflow from approved targets to series items.
- [x] Add persistence for document planning evidence.
- [x] Add minimal WPF entry point.
- [x] Add real provider and binary document extraction in later slices.
```

- [x] **Step 2: Update user guide**

Add this section to `docs/USER_GUIDE.md`:

```markdown
## Document Illustration

The document illustration workflow helps turn article or draft text into planned image targets. The first implementation uses fake providers by default and supports pasted or plain text content. It can create concept illustrations and graphical abstract drafts, then add approved targets to the existing Plan and Prompts workflow.

Scholarly draft mode blocks fake evidence imagery. Use it for schematic concepts, graphical abstracts, and background plates rather than fabricated data plots or experimental images.
```

- [x] **Step 3: Run full gates**

Run:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
dotnet build
dotnet test
dotnet format --verify-no-changes
git status --short
```

Expected:

- Placeholder scan has no new unresolved planning placeholders.
- Build succeeds.
- Tests pass.
- Format reports no changes.
- Git status shows only intended files before the final commit.

- Commit.

Run:

```powershell
git add docs/TASKS.md docs/USER_GUIDE.md
git commit -m "docs: 更新文稿配图工作流任务和指南"
```

## Implementation Order

Recommended order:

1. Task 1: domain model.
2. Task 2: presets and rubrics.
3. Task 3: fake provider planning.
4. Task 4: application workflow.
5. Task 5: persistence.
6. Task 6: minimal WPF entry point.
7. Task 7: docs and full gates.

This order keeps each commit independently understandable and keeps paid API calls out of the default path.

## Self-Review

Spec coverage:

- Document source and brief: Task 1, Task 3, Task 4.
- Illustration plan and targets: Task 1, Task 4.
- Strictness levels: Task 1, Task 3, Task 6.
- Presets and rubrics: Task 2.
- UI entry point: Task 6.
- Provider boundaries and fake-first rule: Task 3, Task 4, Task 7.
- Error handling for fake evidence: Task 1, Task 4.
- Testing and acceptance criteria: Tasks 1 through 7.

Implementation consistency note:

- `IllustrationPlan.ProjectId` is required from Task 1 onward so EF Core can map project-owned plans without guessing from `DocumentBriefId`.
