# Brief-First Image Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a `Brief Studio` workflow that turns loose user intent into a structured creative brief, multiple prompt directions, and promoted prompt versions before image generation.

**Architecture:** Add provider-neutral domain records in `ImageSeriesStudio.Core`, persist them with the existing SQLite model, extend text planning through fake-first contracts, and expose the workflow through `ProjectApplicationService` before adding WPF controls. Real image API calls remain outside this plan.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, EF Core SQLite, xUnit, existing fake providers.

---

## Scope Check

This plan implements the first usable slice of the spec in `docs/superpowers/specs/2026-06-02-brief-first-image-generation-design.md`.

Included:

- `CreativeBrief` and `PromptDirection` domain records.
- Persistence for briefs under a series.
- Fake-first prompt direction generation.
- Application service methods to create a brief, request directions, and promote a direction into `PromptVersion`.
- Minimal WPF `Brief` tab wiring and localization.

Deferred to later plans:

- Real OpenAI structured brief-direction calls.
- Mask/edit workflow.
- Reference image upload UI.
- Workflow export/import.
- Optional graph view.

## File Structure

- `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`: new domain records and validation.
- `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`: connect `CreativeBrief` to `ImageSeries`.
- `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`: add brief-direction planning request/result records and provider method.
- `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`: fake brief-direction output for tests and UI.
- `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs`: implement method as an explicit unsupported real-call guard in this slice.
- `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`: EF mapping for briefs and JSON prompt directions.
- `src/ImageSeriesStudio.Infrastructure/Persistence/EfProjectRepository.cs`: include briefs when loading projects.
- `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`: create/update brief, request directions, promote direction.
- `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`: add English and Chinese labels.
- `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`: expose brief state and commands.
- `src/ImageSeriesStudio.App/MainWindow.xaml`: add compact Brief tab view.
- `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`: domain tests.
- `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`: persistence coverage.
- `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`: fake direction coverage.
- `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`: service workflow coverage.
- `tests/ImageSeriesStudio.Tests/LocalizationTests.cs`: label coverage.

---

### Task 1: Add Creative Brief Domain Model

**Files:**

- Create: `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`
- Modify: `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`
- Test: `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`

- [x] **Step 1: Write failing domain tests**

Create `tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs`:

```csharp
using ImageSeriesStudio.Core.Projects;

namespace ImageSeriesStudio.Tests;

public sealed class CreativeBriefTests
{
    [Fact]
    public void Create_TrimsFieldsAndStoresPromptDirections()
    {
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var seriesId = Guid.NewGuid();

        var brief = CreativeBrief.Create(
            seriesId,
            " Article illustration set ",
            " teachers ",
            ImageTextPolicy.DeterministicPostRender,
            "clean educational style",
            ["formula labels"],
            ["fake historical scene"],
            timestamp);

        var direction = PromptDirection.Create(
            "conservative",
            "Conservative faithful",
            "Safest route for classroom accuracy.",
            "Create a clean visual background.",
            "No fake labels.",
            "Strong factual match.",
            "May feel restrained.",
            timestamp.AddMinutes(1));

        brief.ReplaceDirections([direction], timestamp.AddMinutes(2));

        Assert.Equal(seriesId, brief.SeriesId);
        Assert.Equal("Article illustration set", brief.Goal);
        Assert.Equal("teachers", brief.Audience);
        Assert.Equal(ImageTextPolicy.DeterministicPostRender, brief.TextPolicy);
        Assert.Equal("clean educational style", brief.StyleIntent);
        Assert.Equal("formula labels", Assert.Single(brief.MustInclude));
        Assert.Equal("fake historical scene", Assert.Single(brief.MustAvoid));
        Assert.Equal("conservative", Assert.Single(brief.PromptDirections).Key);
        Assert.Equal(timestamp.AddMinutes(2), brief.UpdatedAt);
    }

    [Fact]
    public void Create_RejectsBlankRequiredFields()
    {
        var timestamp = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            CreativeBrief.Create(
                Guid.NewGuid(),
                " ",
                "teachers",
                ImageTextPolicy.Hybrid,
                "style",
                [],
                [],
                timestamp));

        Assert.Throws<ArgumentException>(() =>
            PromptDirection.Create(
                " ",
                "Name",
                "Use",
                "Prompt",
                "Negative",
                "Strength",
                "Risk",
                timestamp));
    }

    [Fact]
    public void ImageSeries_AddCreativeBrief_AttachesBriefToSeries()
    {
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var series = ImageSeries.Create(Guid.NewGuid(), "Series", "Description", timestamp);

        var brief = series.AddCreativeBrief(
            "course poster",
            "middle school teachers",
            ImageTextPolicy.DeterministicPostRender,
            "editorial science style",
            ["accurate diagram"],
            ["unreadable text"],
            timestamp.AddMinutes(1));

        Assert.Equal(series.Id, brief.SeriesId);
        Assert.Equal("course poster", brief.Goal);
        Assert.Single(series.CreativeBriefs);
    }
}
```

- [x] **Step 2: Run the failing tests**

Run:

```powershell
dotnet test --filter CreativeBriefTests
```

Expected: fails because `CreativeBrief`, `PromptDirection`, and `ImageSeries.CreativeBriefs` do not exist.

- [x] **Step 3: Add domain records**

Create `src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs`:

```csharp
namespace ImageSeriesStudio.Core.Projects;

public sealed class CreativeBrief
{
    private CreativeBrief()
    {
        Goal = string.Empty;
        Audience = string.Empty;
        StyleIntent = string.Empty;
        MustInclude = [];
        MustAvoid = [];
        PromptDirections = [];
    }

    private CreativeBrief(
        Guid id,
        Guid seriesId,
        string goal,
        string audience,
        ImageTextPolicy textPolicy,
        string styleIntent,
        IReadOnlyList<string> mustInclude,
        IReadOnlyList<string> mustAvoid,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeriesId = seriesId;
        Goal = RequireText(goal, nameof(goal));
        Audience = RequireText(audience, nameof(audience));
        TextPolicy = textPolicy;
        StyleIntent = styleIntent.Trim();
        MustInclude = NormalizeList(mustInclude);
        MustAvoid = NormalizeList(mustAvoid);
        PromptDirections = [];
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeriesId { get; private set; }

    public string Goal { get; private set; }

    public string Audience { get; private set; }

    public ImageTextPolicy TextPolicy { get; private set; }

    public string StyleIntent { get; private set; }

    public IReadOnlyList<string> MustInclude { get; private set; }

    public IReadOnlyList<string> MustAvoid { get; private set; }

    public IReadOnlyList<PromptDirection> PromptDirections { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CreativeBrief Create(
        Guid seriesId,
        string goal,
        string audience,
        ImageTextPolicy textPolicy,
        string styleIntent,
        IReadOnlyList<string> mustInclude,
        IReadOnlyList<string> mustAvoid,
        DateTimeOffset createdAt)
    {
        return new CreativeBrief(
            Guid.NewGuid(),
            seriesId,
            goal,
            audience,
            textPolicy,
            styleIntent,
            mustInclude,
            mustAvoid,
            createdAt);
    }

    public void ReplaceDirections(IReadOnlyList<PromptDirection> directions, DateTimeOffset timestamp)
    {
        if (directions.Count == 0)
        {
            throw new ArgumentException("At least one prompt direction is required.", nameof(directions));
        }

        var duplicateKey = directions
            .GroupBy(direction => direction.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateKey is not null)
        {
            throw new ArgumentException($"Prompt direction key must be unique: {duplicateKey.Key}", nameof(directions));
        }

        PromptDirections = directions.ToArray();
        UpdatedAt = timestamp;
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
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

public sealed record PromptDirection(
    string Key,
    string Name,
    string IntendedUse,
    string PromptText,
    string NegativeConstraints,
    string ExpectedStrength,
    string ExpectedRisk,
    DateTimeOffset CreatedAt)
{
    public static PromptDirection Create(
        string key,
        string name,
        string intendedUse,
        string promptText,
        string negativeConstraints,
        string expectedStrength,
        string expectedRisk,
        DateTimeOffset createdAt)
    {
        return new PromptDirection(
            RequireText(key, nameof(key)),
            RequireText(name, nameof(name)),
            RequireText(intendedUse, nameof(intendedUse)),
            RequireText(promptText, nameof(promptText)),
            negativeConstraints.Trim(),
            expectedStrength.Trim(),
            expectedRisk.Trim(),
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
}
```

- [x] **Step 4: Connect briefs to series**

Modify `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs` inside `ImageSeries`:

```csharp
private readonly List<CreativeBrief> _creativeBriefs = [];
```

Add this property:

```csharp
public IReadOnlyCollection<CreativeBrief> CreativeBriefs => _creativeBriefs.AsReadOnly();
```

Add this method:

```csharp
public CreativeBrief AddCreativeBrief(
    string goal,
    string audience,
    ImageTextPolicy textPolicy,
    string styleIntent,
    IReadOnlyList<string> mustInclude,
    IReadOnlyList<string> mustAvoid,
    DateTimeOffset timestamp)
{
    var brief = CreativeBrief.Create(
        Id,
        goal,
        audience,
        textPolicy,
        styleIntent,
        mustInclude,
        mustAvoid,
        timestamp);
    _creativeBriefs.Add(brief);
    UpdatedAt = timestamp;
    return brief;
}
```

- [x] **Step 5: Verify domain tests pass**

Run:

```powershell
dotnet test --filter CreativeBriefTests
```

Expected: all `CreativeBriefTests` pass.

- [x] **Step 6: Commit domain slice**

Run:

```powershell
git add src/ImageSeriesStudio.Core/Projects/CreativeBrief.cs src/ImageSeriesStudio.Core/Projects/ProjectModel.cs tests/ImageSeriesStudio.Tests/CreativeBriefTests.cs
git commit -m "feat: 添加生图需求设计领域模型"
```

---

### Task 2: Persist Creative Briefs

**Files:**

- Modify: `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/Persistence/EfProjectRepository.cs`
- Test: `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`

- [x] **Step 1: Extend persistence test**

In `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`, inside `AppDbContext_SavesAndLoadsCompleteFakeProject`, add after series creation:

```csharp
var brief = series.AddCreativeBrief(
    "Physics classroom poster",
    "middle school teachers",
    ImageTextPolicy.DeterministicPostRender,
    "clean editorial science style",
    ["accurate formula area"],
    ["model-rendered small text"],
    timestamp.AddMinutes(2));
brief.ReplaceDirections(
    [
        PromptDirection.Create(
            "conservative",
            "Conservative faithful",
            "Use for accurate classroom delivery.",
            "Create a clean science background.",
            "No unreadable formula text.",
            "Accurate and easy to review.",
            "Less dramatic than a cover image.",
            timestamp.AddMinutes(3)),
    ],
    timestamp.AddMinutes(4));
```

In the load assertion block, change the include chain:

```csharp
.Include(project => project.Series)
.ThenInclude(series => series.CreativeBriefs)
.Include(project => project.Series)
.ThenInclude(series => series.Items)
.ThenInclude(item => item.PromptVersions)
```

Then assert:

```csharp
var loadedBrief = Assert.Single(loadedSeries.CreativeBriefs);
Assert.Equal("Physics classroom poster", loadedBrief.Goal);
Assert.Equal("conservative", Assert.Single(loadedBrief.PromptDirections).Key);
```

- [x] **Step 2: Run persistence test and see mapping failure**

Run:

```powershell
dotnet test --filter PersistenceTests
```

Expected: fails because EF mapping for `CreativeBrief` is missing.

- [x] **Step 3: Add EF mapping**

Modify `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`.

Add DbSet:

```csharp
public DbSet<CreativeBrief> CreativeBriefs => Set<CreativeBrief>();
```

Inside `ImageSeries` mapping, add:

```csharp
entity.HasMany(series => series.CreativeBriefs)
    .WithOne()
    .HasForeignKey(brief => brief.SeriesId)
    .OnDelete(DeleteBehavior.Cascade);
entity.Navigation(series => series.CreativeBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
```

Add entity mapping:

```csharp
modelBuilder.Entity<CreativeBrief>(entity =>
{
    entity.HasKey(brief => brief.Id);
    entity.Property(brief => brief.Goal).IsRequired();
    entity.Property(brief => brief.Audience).IsRequired();
    entity.Property(brief => brief.StyleIntent).IsRequired();
    entity.Property(brief => brief.MustInclude)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(brief => brief.MustAvoid)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
    entity.Property(brief => brief.PromptDirections)
        .HasConversion(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<PromptDirection>>(json, JsonOptions) ?? new List<PromptDirection>());
});
```

- [x] **Step 4: Include briefs in repository loads**

In `src/ImageSeriesStudio.Infrastructure/Persistence/EfProjectRepository.cs`, update the project load query to include:

```csharp
.Include(project => project.Series)
.ThenInclude(series => series.CreativeBriefs)
```

Keep the existing includes for items, prompts, generation tasks, candidates, and provider profiles.

- [x] **Step 5: Verify persistence**

Run:

```powershell
dotnet test --filter PersistenceTests
```

Expected: all persistence tests pass.

- [ ] **Step 6: Commit persistence slice**

Run:

```powershell
git add src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs src/ImageSeriesStudio.Infrastructure/Persistence/EfProjectRepository.cs tests/ImageSeriesStudio.Tests/PersistenceTests.cs
git commit -m "feat: 持久化生图需求设计"
```

---

### Task 3: Add Fake-First Prompt Direction Planning

**Files:**

- Modify: `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`
- Modify: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs`
- Test: `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`
- Test: `tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs`

- [x] **Step 1: Write failing fake provider test**

Add to `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`:

```csharp
[Fact]
public async Task FakeTextPlanningProvider_CreatesPromptDirectionsForBrief()
{
    var provider = new FakeTextPlanningProvider();

    var result = await provider.CreatePromptDirectionsAsync(
        new BriefPlanningRequest(
            "article illustration",
            "teachers",
            "clean editorial",
            ["accurate subject"],
            ["unreadable text"],
            DirectionCount: 3),
        CancellationToken.None);

    Assert.Equal("fake-text-brief", result.ProviderTraceId);
    Assert.Equal(3, result.Directions.Count);
    Assert.Contains(result.Assumptions, assumption => assumption.Contains("draft", StringComparison.OrdinalIgnoreCase));
    Assert.Equal("conservative", result.Directions[0].Key);
    Assert.Contains("article illustration", result.Directions[0].PromptText);
}
```

- [x] **Step 2: Write real provider guard test**

Add to `tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs`:

```csharp
[Fact]
public async Task TextPlanningProvider_BlocksPromptDirectionsUntilRealImplementationExists()
{
    using var handler = new CaptureHandler(_ => JsonResponse("{}"));
    using var httpClient = new HttpClient(handler);
    var provider = CreateTextProvider(httpClient);

    var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
        provider.CreatePromptDirectionsAsync(
            new BriefPlanningRequest(
                "poster",
                "teachers",
                "clean style",
                ["accurate diagram"],
                ["tiny text"],
                DirectionCount: 2),
            CancellationToken.None));

    Assert.Contains("Brief direction planning is not implemented for OpenAI", exception.Message);
    Assert.Equal(0, handler.CallCount);
}
```

- [x] **Step 3: Run failing tests**

Run:

```powershell
dotnet test --filter "FakeTextPlanningProvider_CreatesPromptDirectionsForBrief|TextPlanningProvider_BlocksPromptDirectionsUntilRealImplementationExists"
```

Expected: fails because the new request/result and provider method do not exist.

- [x] **Step 4: Extend provider contract**

In `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`, add method to `ITextPlanningProvider`:

```csharp
Task<BriefPlanningResult> CreatePromptDirectionsAsync(BriefPlanningRequest request, CancellationToken cancellationToken);
```

Add records near `PlanningRequest`:

```csharp
public sealed record BriefPlanningRequest(
    string Goal,
    string Audience,
    string StyleIntent,
    IReadOnlyList<string> MustInclude,
    IReadOnlyList<string> MustAvoid,
    int DirectionCount = 3);

public sealed record BriefPlanningResult(
    IReadOnlyList<PromptDirectionDraft> Directions,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ClarifyingQuestions,
    string ProviderTraceId);

public sealed record PromptDirectionDraft(
    string Key,
    string Name,
    string IntendedUse,
    string PromptText,
    string NegativeConstraints,
    string ExpectedStrength,
    string ExpectedRisk);
```

- [x] **Step 5: Implement fake directions**

In `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`, add this method to `FakeTextPlanningProvider`:

```csharp
public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
    BriefPlanningRequest request,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var count = Math.Clamp(request.DirectionCount, 1, 4);
    var templates = new[]
    {
        new { Key = "conservative", Name = "Conservative faithful", Use = "Safest match to the brief.", Strength = "High requirement match.", Risk = "Less visually dramatic." },
        new { Key = "visual-impact", Name = "Visual impact", Use = "Stronger composition and contrast.", Strength = "More engaging first impression.", Risk = "May need closer factual review." },
        new { Key = "minimal-clean", Name = "Minimal clean", Use = "Low visual noise and easy review.", Strength = "Good for text composition.", Risk = "May feel plain." },
        new { Key = "experimental", Name = "Experimental alternate", Use = "Explores a less obvious direction.", Strength = "Can reveal a stronger style.", Risk = "Higher mismatch risk." },
    };

    var directions = templates
        .Take(count)
        .Select(template => new PromptDirectionDraft(
            template.Key,
            template.Name,
            template.Use,
            $"Create {request.Goal} for {request.Audience}. Style: {request.StyleIntent}. Include: {string.Join(", ", request.MustInclude)}.",
            $"Avoid: {string.Join(", ", request.MustAvoid)}.",
            template.Strength,
            template.Risk))
        .ToArray();

    return Task.FromResult(new BriefPlanningResult(
        directions,
        ["Use draft generation before final quality."],
        ["Confirm whether final text should be composed in app."],
        "fake-text-brief"));
}
```

- [x] **Step 6: Add OpenAI explicit guard for this slice**

In `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs`, add:

```csharp
public Task<BriefPlanningResult> CreatePromptDirectionsAsync(
    BriefPlanningRequest request,
    CancellationToken cancellationToken)
{
    throw new NotSupportedException("Brief direction planning is not implemented for OpenAI in this slice. Use FakeTextPlanningProvider first.");
}
```

- [x] **Step 7: Verify provider tests**

Run:

```powershell
dotnet test --filter "FakeTextPlanningProvider_CreatesPromptDirectionsForBrief|TextPlanningProvider_BlocksPromptDirectionsUntilRealImplementationExists"
```

Expected: both tests pass.

- [x] **Step 8: Commit provider slice**

Run:

```powershell
git add src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs tests/ImageSeriesStudio.Tests/FakeProviderTests.cs tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs
git commit -m "feat: 添加假数据提示词方向规划"
```

---

### Task 4: Add Application Service Workflow

**Files:**

- Modify: `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`
- Test: `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`

- [x] **Step 1: Write failing service test**

Add to `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`:

```csharp
[Fact]
public async Task ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion()
{
    var databaseDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));
    var databasePath = Path.Combine(databaseDirectory, "project-brief.sqlite");
    Directory.CreateDirectory(databaseDirectory);

    try
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var service = new ProjectApplicationService(
            new EfProjectRepository(new AppDbContext(options)),
            new FakeTextPlanningProvider());
        var timestamp = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var project = await service.CreateProjectAsync("Brief demo", timestamp, CancellationToken.None);
        var series = await service.AddSeriesAsync(project.Id, "Article images", "Series", timestamp.AddMinutes(1), CancellationToken.None);
        var item = await service.AddItemAsync(project.Id, series.Id, "Opening", "Opening visual", timestamp.AddMinutes(2), CancellationToken.None);

        var brief = await service.CreateCreativeBriefAsync(
            project.Id,
            series.Id,
            "article illustration",
            "teachers",
            ImageTextPolicy.DeterministicPostRender,
            "clean editorial",
            ["accurate visual"],
            ["small fake text"],
            timestamp.AddMinutes(3),
            CancellationToken.None);

        var planned = await service.CreatePromptDirectionsAsync(
            project.Id,
            brief.Id,
            timestamp.AddMinutes(4),
            CancellationToken.None);

        var promoted = await service.PromotePromptDirectionAsync(
            project.Id,
            item.Id,
            brief.Id,
            "conservative",
            new GenerationSettings(1024, 1024, "standard", "png"),
            timestamp.AddMinutes(5),
            CancellationToken.None);

        var loaded = await service.LoadProjectAsync(project.Id, CancellationToken.None);
        var loadedBrief = loaded!.Series.Single().CreativeBriefs.Single();
        var loadedPrompt = loaded.Series.Single().Items.Single().PromptVersions.Single();

        Assert.Equal(brief.Id, loadedBrief.Id);
        Assert.Equal(3, planned.PromptDirections.Count);
        Assert.Equal(promoted.Id, loadedPrompt.Id);
        Assert.Contains("article illustration", loadedPrompt.PromptText);
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

- [x] **Step 2: Run failing service test**

Run:

```powershell
dotnet test --filter ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion
```

Expected: fails because service methods do not exist.

- [x] **Step 3: Add service methods**

In `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`, add:

```csharp
public async Task<CreativeBrief> CreateCreativeBriefAsync(
    Guid projectId,
    Guid seriesId,
    string goal,
    string audience,
    ImageTextPolicy textPolicy,
    string styleIntent,
    IReadOnlyList<string> mustInclude,
    IReadOnlyList<string> mustAvoid,
    DateTimeOffset timestamp,
    CancellationToken cancellationToken)
{
    var project = await RequireProjectAsync(projectId, cancellationToken);
    var series = project.Series.SingleOrDefault(series => series.Id == seriesId)
        ?? throw new InvalidOperationException($"Series not found: {seriesId}");
    var brief = series.AddCreativeBrief(goal, audience, textPolicy, styleIntent, mustInclude, mustAvoid, timestamp);
    await _repository.SaveAsync(project, cancellationToken);
    return brief;
}

public async Task<CreativeBrief> CreatePromptDirectionsAsync(
    Guid projectId,
    Guid creativeBriefId,
    DateTimeOffset timestamp,
    CancellationToken cancellationToken)
{
    if (_textPlanningProvider is null)
    {
        throw new InvalidOperationException("Text planning provider is not registered.");
    }

    var project = await RequireProjectAsync(projectId, cancellationToken);
    var brief = project.Series
        .SelectMany(series => series.CreativeBriefs)
        .SingleOrDefault(brief => brief.Id == creativeBriefId)
        ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");

    var result = await _textPlanningProvider.CreatePromptDirectionsAsync(
        new BriefPlanningRequest(
            brief.Goal,
            brief.Audience,
            brief.StyleIntent,
            brief.MustInclude,
            brief.MustAvoid,
            DirectionCount: 3),
        cancellationToken);

    brief.ReplaceDirections(
        result.Directions
            .Select(direction => PromptDirection.Create(
                direction.Key,
                direction.Name,
                direction.IntendedUse,
                direction.PromptText,
                direction.NegativeConstraints,
                direction.ExpectedStrength,
                direction.ExpectedRisk,
                timestamp))
            .ToArray(),
        timestamp);

    await _repository.SaveAsync(project, cancellationToken);
    return brief;
}

public async Task<PromptVersion> PromotePromptDirectionAsync(
    Guid projectId,
    Guid seriesItemId,
    Guid creativeBriefId,
    string directionKey,
    GenerationSettings settings,
    DateTimeOffset timestamp,
    CancellationToken cancellationToken)
{
    var project = await RequireProjectAsync(projectId, cancellationToken);
    var item = project.Series
        .SelectMany(series => series.Items)
        .SingleOrDefault(item => item.Id == seriesItemId)
        ?? throw new InvalidOperationException($"Series item not found: {seriesItemId}");
    var brief = project.Series
        .SelectMany(series => series.CreativeBriefs)
        .SingleOrDefault(brief => brief.Id == creativeBriefId)
        ?? throw new InvalidOperationException($"Creative brief not found: {creativeBriefId}");
    var direction = brief.PromptDirections.SingleOrDefault(direction =>
        direction.Key.Equals(directionKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Prompt direction not found: {directionKey}");

    var providerProfile = ResolveProviderProfile(project, providerProfileId: null, timestamp);
    var prompt = item.AddPromptVersion(direction.PromptText, settings, providerProfile.Id, timestamp);
    await _repository.SaveAsync(project, cancellationToken);
    return prompt;
}
```

- [x] **Step 4: Verify service workflow**

Run:

```powershell
dotnet test --filter ProjectApplicationService_CreatesBriefDirectionsAndPromotesPromptVersion
```

Expected: test passes.

- [ ] **Step 5: Commit service slice**

Run:

```powershell
git add src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs
git commit -m "feat: 串联需求设计与提示词晋级"
```

---

### Task 5: Add Minimal Brief Tab UI

**Files:**

- Modify: `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`
- Modify: `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ImageSeriesStudio.App/MainWindow.xaml`
- Test: `tests/ImageSeriesStudio.Tests/LocalizationTests.cs`

- [ ] **Step 1: Write localization test**

In `tests/ImageSeriesStudio.Tests/LocalizationTests.cs`, add:

```csharp
[Theory]
[InlineData("en-US", "Brief")]
[InlineData("zh-CN", "需求设计")]
public void LocalizationCatalog_ContainsBriefTab(string cultureName, string expected)
{
    var service = new LocalizationService();
    var culture = new CultureInfo(cultureName);

    Assert.Equal(expected, service.Text(LocalizationKey.BriefTab, culture));
    Assert.False(string.IsNullOrWhiteSpace(service.Text(LocalizationKey.CreateBrief, culture)));
    Assert.False(string.IsNullOrWhiteSpace(service.Text(LocalizationKey.GeneratePromptDirections, culture)));
    Assert.False(string.IsNullOrWhiteSpace(service.Text(LocalizationKey.PromotePromptDirection, culture)));
}
```

- [ ] **Step 2: Run failing localization test**

Run:

```powershell
dotnet test --filter LocalizationCatalog_ContainsBriefTab
```

Expected: fails because localization keys do not exist.

- [ ] **Step 3: Add localization keys**

In `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`, add enum values:

```csharp
BriefTab,
CreateBrief,
GeneratePromptDirections,
PromotePromptDirection,
BriefGoal,
BriefAudience,
BriefStyleIntent,
PromptDirectionsHeader,
```

Add English catalog entries:

```csharp
[LocalizationKey.BriefTab] = "Brief",
[LocalizationKey.CreateBrief] = "Create brief",
[LocalizationKey.GeneratePromptDirections] = "Generate directions",
[LocalizationKey.PromotePromptDirection] = "Promote direction",
[LocalizationKey.BriefGoal] = "Goal",
[LocalizationKey.BriefAudience] = "Audience",
[LocalizationKey.BriefStyleIntent] = "Style intent",
[LocalizationKey.PromptDirectionsHeader] = "Prompt directions",
```

Add Chinese catalog entries:

```csharp
[LocalizationKey.BriefTab] = "需求设计",
[LocalizationKey.CreateBrief] = "创建设计简报",
[LocalizationKey.GeneratePromptDirections] = "生成提示词方向",
[LocalizationKey.PromotePromptDirection] = "晋级为提示词版本",
[LocalizationKey.BriefGoal] = "目标",
[LocalizationKey.BriefAudience] = "受众",
[LocalizationKey.BriefStyleIntent] = "风格意图",
[LocalizationKey.PromptDirectionsHeader] = "提示词方向",
```

- [ ] **Step 4: Add ViewModel surface**

In `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`, extend workbench tab creation so Brief appears before Plan:

```csharp
new WorkbenchTabViewModel(Text(LocalizationKey.BriefTab), "Brief", IsBrief: true),
```

Extend `WorkbenchTabViewModel` with `bool IsBrief`.

Add bindable properties:

```csharp
public string BriefGoalLabel { get; private set; } = string.Empty;
public string BriefAudienceLabel { get; private set; } = string.Empty;
public string BriefStyleIntentLabel { get; private set; } = string.Empty;
public string PromptDirectionsHeader { get; private set; } = string.Empty;
public string CreateBriefText { get; private set; } = string.Empty;
public string GeneratePromptDirectionsText { get; private set; } = string.Empty;
public string PromotePromptDirectionText { get; private set; } = string.Empty;
```

Set them in the existing localization refresh method:

```csharp
BriefGoalLabel = Text(LocalizationKey.BriefGoal);
BriefAudienceLabel = Text(LocalizationKey.BriefAudience);
BriefStyleIntentLabel = Text(LocalizationKey.BriefStyleIntent);
PromptDirectionsHeader = Text(LocalizationKey.PromptDirectionsHeader);
CreateBriefText = Text(LocalizationKey.CreateBrief);
GeneratePromptDirectionsText = Text(LocalizationKey.GeneratePromptDirections);
PromotePromptDirectionText = Text(LocalizationKey.PromotePromptDirection);
```

- [ ] **Step 5: Add compact XAML**

In `src/ImageSeriesStudio.App/MainWindow.xaml`, add an `IsBrief` visibility trigger beside the existing Plan/Prompts/Queue/Gallery/Review/Delivery triggers and add a simple Brief panel:

```xml
<Grid Grid.Row="1" Margin="0,16,0,0">
    <Grid.Style>
        <Style TargetType="Grid">
            <Setter Property="Visibility" Value="Collapsed" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsBrief}" Value="True">
                    <Setter Property="Visibility" Value="Visible" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Grid.Style>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <UniformGrid Columns="3">
        <TextBlock Text="{Binding DataContext.BriefGoalLabel, RelativeSource={RelativeSource AncestorType=TabControl}}" />
        <TextBlock Text="{Binding DataContext.BriefAudienceLabel, RelativeSource={RelativeSource AncestorType=TabControl}}" />
        <TextBlock Text="{Binding DataContext.BriefStyleIntentLabel, RelativeSource={RelativeSource AncestorType=TabControl}}" />
    </UniformGrid>
    <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,12,0,0">
        <Button Content="{Binding DataContext.CreateBriefText, RelativeSource={RelativeSource AncestorType=TabControl}}" Margin="0,0,8,0" />
        <Button Content="{Binding DataContext.GeneratePromptDirectionsText, RelativeSource={RelativeSource AncestorType=TabControl}}" Margin="0,0,8,0" />
        <Button Content="{Binding DataContext.PromotePromptDirectionText, RelativeSource={RelativeSource AncestorType=TabControl}}" />
    </StackPanel>
    <TextBlock Grid.Row="2"
               Text="{Binding DataContext.PromptDirectionsHeader, RelativeSource={RelativeSource AncestorType=TabControl}}"
               Margin="0,16,0,0"
               FontWeight="SemiBold" />
</Grid>
```

- [ ] **Step 6: Verify localization test**

Run:

```powershell
dotnet test --filter LocalizationCatalog_ContainsBriefTab
```

Expected: test passes.

- [ ] **Step 7: Verify build**

Run:

```powershell
dotnet build
```

Expected: build succeeds.

- [ ] **Step 8: Commit UI slice**

Run:

```powershell
git add src/ImageSeriesStudio.Application/Localization/LocalizationService.cs src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs src/ImageSeriesStudio.App/MainWindow.xaml tests/ImageSeriesStudio.Tests/LocalizationTests.cs
git commit -m "feat: 添加需求设计工作台入口"
```

---

### Task 6: Final Verification

**Files:**

- Review all modified source and tests.

- [ ] **Step 1: Run full gate**

Run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Expected: all commands succeed.

- [ ] **Step 2: Run placeholder scan**

Run:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOL[D]ER)" .
```

Expected: no unresolved placeholder output from changed files.

- [ ] **Step 3: Inspect status**

Run:

```powershell
git status --short
```

Expected: only intentionally untracked or unrelated user files remain.

- [ ] **Step 4: Commit final corrections if needed**

If formatting changes occur, run:

```powershell
git add src tests
git commit -m "chore: 通过需求设计工作流门禁"
```

Expected: repository contains the Brief Studio implementation in small reviewable commits.
