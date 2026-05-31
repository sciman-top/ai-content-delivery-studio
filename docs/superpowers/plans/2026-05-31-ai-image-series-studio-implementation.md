# AI Image Series Studio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows-first desktop app that supports planning, prompt versioning, batch generation, review, regeneration, and final delivery for image series.

**Architecture:** Use a layered .NET solution. Keep WPF UI separate from domain and provider logic. Use fake providers first so the full loop is testable without network or API cost.

**Tech Stack:** .NET 10, WPF, C#, MVVM Toolkit, Microsoft.Extensions.Hosting, EF Core SQLite, OpenAI provider adapters, xUnit.

---

## File Structure

- Create: `src/ImageSeriesStudio.App/ImageSeriesStudio.App.csproj`
- Create: `src/ImageSeriesStudio.Core/ImageSeriesStudio.Core.csproj`
- Create: `src/ImageSeriesStudio.Infrastructure/ImageSeriesStudio.Infrastructure.csproj`
- Create: `tests/ImageSeriesStudio.Tests/ImageSeriesStudio.Tests.csproj`
- Create: `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`
- Create: `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`
- Create: `src/ImageSeriesStudio.Core/Generation/GenerationQueue.cs`
- Create: `src/ImageSeriesStudio.Core/Review/ReviewModel.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Delivery/DeliveryPackageWriter.cs`
- Create: `src/ImageSeriesStudio.App/App.xaml`
- Create: `src/ImageSeriesStudio.App/App.xaml.cs`
- Create: `src/ImageSeriesStudio.App/MainWindow.xaml`
- Create: `src/ImageSeriesStudio.App/MainWindow.xaml.cs`
- Create: `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`

## Task 1: Solution Skeleton

**Files:**

- Create: `ImageSeriesStudio.sln`
- Create: project files listed above.

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n ImageSeriesStudio
dotnet new wpf -n ImageSeriesStudio.App -o src/ImageSeriesStudio.App -f net10.0-windows
dotnet new classlib -n ImageSeriesStudio.Core -o src/ImageSeriesStudio.Core -f net10.0
dotnet new classlib -n ImageSeriesStudio.Infrastructure -o src/ImageSeriesStudio.Infrastructure -f net10.0
dotnet new xunit -n ImageSeriesStudio.Tests -o tests/ImageSeriesStudio.Tests -f net10.0
dotnet sln add src/ImageSeriesStudio.App/ImageSeriesStudio.App.csproj
dotnet sln add src/ImageSeriesStudio.Core/ImageSeriesStudio.Core.csproj
dotnet sln add src/ImageSeriesStudio.Infrastructure/ImageSeriesStudio.Infrastructure.csproj
dotnet sln add tests/ImageSeriesStudio.Tests/ImageSeriesStudio.Tests.csproj
dotnet add src/ImageSeriesStudio.App reference src/ImageSeriesStudio.Core
dotnet add src/ImageSeriesStudio.App reference src/ImageSeriesStudio.Infrastructure
dotnet add src/ImageSeriesStudio.Infrastructure reference src/ImageSeriesStudio.Core
dotnet add tests/ImageSeriesStudio.Tests reference src/ImageSeriesStudio.Core
dotnet add tests/ImageSeriesStudio.Tests reference src/ImageSeriesStudio.Infrastructure
```

Expected: all projects are added to the solution.

- [ ] **Step 2: Add packages**

Run:

```powershell
dotnet add src/ImageSeriesStudio.App package CommunityToolkit.Mvvm
dotnet add src/ImageSeriesStudio.App package Microsoft.Extensions.Hosting
dotnet add src/ImageSeriesStudio.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/ImageSeriesStudio.Infrastructure package Microsoft.Extensions.Http
dotnet add tests/ImageSeriesStudio.Tests package Microsoft.NET.Test.Sdk
```

Expected: package restore succeeds.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet build
dotnet test
```

Expected: build and tests pass.

## Task 2: Domain Model

**Files:**

- Create: `src/ImageSeriesStudio.Core/Projects/ProjectModel.cs`
- Test: `tests/ImageSeriesStudio.Tests/ProjectModelTests.cs`

- [ ] **Step 1: Add domain records**

Create project, series, item, prompt version, task, candidate, review, and delivery records. Include stable IDs, timestamps, state fields, provider profile IDs, and asset paths.

- [ ] **Step 2: Add state transition tests**

Test that an item can move from `Draft` to `Ready`, `NeedsReview`, `Approved`, and `Delivered`, and rejects invalid backwards transitions without an explicit reopen operation.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter ProjectModelTests
```

Expected: tests pass.

## Task 3: Provider Contracts And Fakes

**Files:**

- Create: `src/ImageSeriesStudio.Core/Providers/AiProviderContracts.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Fakes/FakeProviders.cs`
- Test: `tests/ImageSeriesStudio.Tests/FakeProviderTests.cs`

- [ ] **Step 1: Define provider contracts**

Define `ITextPlanningProvider`, `IImageGenerationProvider`, `IVisionReviewProvider`, and `IProviderCapabilities`.

- [ ] **Step 2: Implement deterministic fake providers**

Fake text provider returns a small series plan. Fake image provider writes a small generated placeholder image and sidecar metadata. Fake vision provider returns a structured review result with configurable pass/fail.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter FakeProviderTests
```

Expected: tests pass without network access.

## Task 4: Persistence

**Files:**

- Create: `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`
- Test: `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`

- [ ] **Step 1: Add EF Core SQLite context**

Map project, series, items, prompt versions, tasks, candidates, and reviews.

- [ ] **Step 2: Add in-memory temporary database tests**

Use a temporary SQLite file under the test temp directory. Save and load a complete fake project.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter PersistenceTests
```

Expected: tests pass and no database file is left under the repository.

## Task 5: Generation Queue

**Files:**

- Create: `src/ImageSeriesStudio.Core/Generation/GenerationQueue.cs`
- Test: `tests/ImageSeriesStudio.Tests/GenerationQueueTests.cs`

- [ ] **Step 1: Implement bounded queue**

Support queued, running, succeeded, failed, and cancelled task states. Include retry count, max retries, timeout, and cancellation token.

- [ ] **Step 2: Test retry and cancellation**

Use fake providers that fail once then succeed, and fake providers that respect cancellation.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter GenerationQueueTests
```

Expected: tests pass.

## Task 6: Review And Delivery

**Files:**

- Create: `src/ImageSeriesStudio.Core/Review/ReviewModel.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Delivery/DeliveryPackageWriter.cs`
- Test: `tests/ImageSeriesStudio.Tests/DeliveryPackageTests.cs`

- [ ] **Step 1: Add review model**

Represent rubric dimensions, hard-fail flags, reviewer comments, AI repair suggestion, human approval, and final decision.

- [ ] **Step 2: Add delivery package writer**

Export final images, prompt snapshots, sidecar metadata, review report, and manifest.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter DeliveryPackageTests
```

Expected: tests pass and exported package has one final image per approved item.

## Task 7: WPF Shell With Generic Host

**Files:**

- Modify: `src/ImageSeriesStudio.App/App.xaml`
- Modify: `src/ImageSeriesStudio.App/App.xaml.cs`
- Modify: `src/ImageSeriesStudio.App/MainWindow.xaml`
- Create: `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Wire Generic Host**

Use `Host.CreateApplicationBuilder`, register services, start the host on app startup, and stop it on app exit.

- [ ] **Step 2: Add workbench shell**

Create tabs for Brief, Plan, Prompts, Queue, Gallery, Review, and Delivery. Bind visible state to `MainWindowViewModel`.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet build
```

Expected: app builds and opens a basic workbench window.

## Task 8: OpenAI Providers

**Files:**

- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiImageGenerationProvider.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiVisionReviewProvider.cs`
- Test: `tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs`

- [ ] **Step 1: Implement provider adapters behind interfaces**

Do not expose OpenAI request objects outside infrastructure.

- [ ] **Step 2: Add contract tests using mocked HTTP**

Verify request shape, response parsing, metadata capture, and error mapping without real API calls.

- [ ] **Step 3: Add opt-in smoke command**

Real API smoke must be skipped unless an explicit local environment variable enables it.

- [ ] **Step 4: Gate**

Run:

```powershell
dotnet test --filter OpenAiProviderContractTests
```

Expected: mocked contract tests pass. Real API tests remain skipped by default.

## Task 9: Physics Project Import Sample

**Files:**

- Create: `src/ImageSeriesStudio.Infrastructure/Import/PhysicsPosterImportService.cs`
- Test: `tests/ImageSeriesStudio.Tests/PhysicsPosterImportTests.cs`

- [ ] **Step 1: Import prompt and delivery metadata**

Map physics prompts and finalized content into generic project, series, item, prompt, candidate, and review structures.

- [ ] **Step 2: Keep import read-only**

Do not mutate the source physics project. Copy only selected sample metadata into a new workspace.

- [ ] **Step 3: Gate**

Run:

```powershell
dotnet test --filter PhysicsPosterImportTests
```

Expected: import succeeds against a small fixture and never writes to the source project.

## Final Gate

Run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Expected: all gates pass before any release or real batch generation.
