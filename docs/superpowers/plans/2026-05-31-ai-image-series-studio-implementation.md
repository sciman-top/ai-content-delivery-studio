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

- [x] **Step 1: Create solution and projects**

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

- [x] **Step 2: Add packages**

Run:

```powershell
dotnet add src/ImageSeriesStudio.App package CommunityToolkit.Mvvm
dotnet add src/ImageSeriesStudio.App package Microsoft.Extensions.Hosting
dotnet add src/ImageSeriesStudio.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/ImageSeriesStudio.Infrastructure package Microsoft.Extensions.Http
dotnet add tests/ImageSeriesStudio.Tests package Microsoft.NET.Test.Sdk
```

Expected: package restore succeeds.

- [x] **Step 3: Gate**

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

- [x] **Step 1: Add domain records**

Create project, series, item, prompt version, task, candidate, review, and delivery records. Include stable IDs, timestamps, state fields, provider profile IDs, and asset paths.

- [x] **Step 2: Add state transition tests**

Test that an item can move from `Draft` to `Ready`, `NeedsReview`, `Approved`, and `Delivered`, and rejects invalid backwards transitions without an explicit reopen operation.

- [x] **Step 3: Gate**

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

- [x] **Step 1: Define provider contracts**

Define `ITextPlanningProvider`, `IImageGenerationProvider`, `IVisionReviewProvider`, and `IProviderCapabilities`.

- [x] **Step 2: Implement deterministic fake providers**

Fake text provider returns a small series plan. Fake image provider writes a small generated placeholder image and sidecar metadata. Fake vision provider returns a structured review result with configurable pass/fail.

- [x] **Step 3: Gate**

Run:

```powershell
dotnet test --filter FakeProviderTests
```

Expected: tests pass without network access.

## Task 4: Persistence

**Files:**

- Create: `src/ImageSeriesStudio.Infrastructure/Persistence/AppDbContext.cs`
- Test: `tests/ImageSeriesStudio.Tests/PersistenceTests.cs`

- [x] **Step 1: Add EF Core SQLite context**

Map project, series, items, prompt versions, tasks, candidates, and reviews.

- [x] **Step 2: Add in-memory temporary database tests**

Use a temporary SQLite file under the test temp directory. Save and load a complete fake project.

- [x] **Step 3: Gate**

Run:

```powershell
dotnet test --filter PersistenceTests
```

Expected: tests pass and no database file is left under the repository.

## Task 5: Generation Queue

**Files:**

- Create: `src/ImageSeriesStudio.Core/Generation/GenerationQueue.cs`
- Test: `tests/ImageSeriesStudio.Tests/GenerationQueueTests.cs`

- [x] **Step 1: Implement bounded queue**

Support queued, running, succeeded, failed, and cancelled task states. Include retry count, max retries, timeout, and cancellation token.

- [x] **Step 2: Test retry and cancellation**

Use fake providers that fail once then succeed, and fake providers that respect cancellation.

- [x] **Step 3: Gate**

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

- [x] **Step 1: Add review model**

Represent rubric dimensions, hard-fail flags, reviewer comments, AI repair suggestion, human approval, and final decision.

- [x] **Step 2: Add delivery package writer**

Export final images, prompt snapshots, sidecar metadata, review report, and manifest.

- [x] **Step 3: Gate**

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

- [x] **Step 1: Wire Generic Host**

Use `Host.CreateApplicationBuilder`, register services, start the host on app startup, and stop it on app exit.

- [x] **Step 2: Add workbench shell**

Create tabs for Brief, Plan, Prompts, Queue, Gallery, Review, and Delivery. Bind visible state to `MainWindowViewModel`.

- [x] **Step 3: Gate**

Run:

```powershell
dotnet build
```

Expected: app builds and opens a basic workbench window.

## Task 7A: Application Layer And Localization Foundation

**Files:**

- Create: `src/ImageSeriesStudio.Application/ImageSeriesStudio.Application.csproj`
- Create: `src/ImageSeriesStudio.Application/Localization/LocalizationService.cs`
- Modify: `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ImageSeriesStudio.App/MainWindow.xaml`
- Test: `tests/ImageSeriesStudio.Tests/LocalizationTests.cs`

- [x] **Step 1: Add application layer project**

Create `ImageSeriesStudio.Application`, reference `Core`, add it to the solution, and let the WPF app reference it.

- [x] **Step 2: Add Chinese and English localization service**

Support `System`, `Chinese`, and `English` preferences. Resolve `zh-CN` and `en-US` shell strings through stable keys.

- [x] **Step 3: Add language selection to shell**

Bind a language selector in the WPF top bar. Refresh visible shell labels when language changes.

- [x] **Step 4: Gate**

Run:

```powershell
dotnet build
dotnet test --filter LocalizationTests
```

Expected: app builds and localization tests pass.

## Task 7B: Project Application Service Foundation

**Files:**

- Create: `src/ImageSeriesStudio.Application/Projects/ProjectApplicationService.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/Persistence/EfProjectRepository.cs`
- Test: `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`

- [x] **Step 1: Define project repository port and service**

Add an application-layer service that can create and load projects without WPF.

- [x] **Step 2: Implement SQLite repository adapter**

Use `AppDbContext` to save and load `ImageProject` aggregates.

- [x] **Step 3: Gate**

Run:

```powershell
dotnet test --filter ProjectApplicationServiceTests
```

Expected: a project can be created, saved, and loaded from a temporary SQLite database outside the repository.

## Task 7C: Phase 2 UI Implementation Plan

**Goal:** Make the WPF shell complete the fake-provider workflow end-to-end before any real API integration.

**Execution order:**

- [x] **Task 7C.1: Project create/load/save UI**
  - Add localized project name input, create action, project list, and current project summary.
  - Persist project records through `ProjectApplicationService`.
  - Gate: `dotnet build`, `dotnet test`.

- [x] **Task 7C.1A: Series and item editor foundation**
  - Add application service methods for adding series and items.
  - Add localized right-panel controls for creating series and items.
  - Gate: `dotnet build`, `dotnet test --filter ProjectApplicationServiceTests`.

- [x] **Task 7C.2: Series and item editing**
  - Add application service methods for adding series and items.
  - Add localized series and item table in the Plan tab.
  - Gate: service tests plus `dotnet build`.

- [x] **Task 7C.3: Prompt version editor**
  - Add prompt version creation and history display.
  - Keep provider/model fields provider-neutral.
  - Gate: prompt service tests plus `dotnet build`.

- [x] **Task 7C.4: Fake planning action**
  - Run `FakeTextPlanningProvider` from the Brief/Plan path.
  - Create draft series items from the returned plan.
  - Gate: fake workflow test plus UI build.

- [x] **Task 7C.5: Queue panel**
  - Bind `GenerationQueue` state to the Queue tab.
  - Support cancellation and retry visibility.
  - Gate: queue tests plus `dotnet build`.

- [x] **Task 7C.6: Candidate gallery**
  - Show generated placeholder images from fake provider output.
  - Prepare virtualization-friendly item model.
  - Gate: fake provider tests plus `dotnet build`.

- [x] **Task 7C.7: Review panel**
  - Bind rubric and `FakeVisionReviewProvider` result.
  - Keep human approval as the final decision.
  - Gate: review tests plus `dotnet build`.

- [x] **Task 7C.8: Delivery export panel**
  - Call `DeliveryPackageWriter` from the Delivery tab.
  - Export manifest, prompts, metadata, review report, and final image copies.
  - Gate: delivery tests plus `dotnet build`.

**Checkpoint after 7C:** The app can create a project, plan one image series with fakes, generate placeholder candidates, review, approve, and export a delivery package without network access or paid API calls.

## Task 8: OpenAI Providers

**Files:**

- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiProviderOptions.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiTextPlanningProvider.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiImageGenerationProvider.cs`
- Create: `src/ImageSeriesStudio.Infrastructure/OpenAI/OpenAiVisionReviewProvider.cs`
- Test: `tests/ImageSeriesStudio.Tests/OpenAiProviderConfigurationTests.cs`
- Test: `tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs`

- [x] **Step 0: Add provider configuration and real API guard**

Keep real API calls disabled by default. Read secrets from an external secret source, not project files.

- [ ] **Step 1: Implement provider adapters behind interfaces**

Do not expose OpenAI request objects outside infrastructure.

  - [x] Text planning adapter using Responses API request shape.
  - [x] Image generation adapter.
  - [x] Vision review adapter.
  - [x] Provider capability validation.

- [x] **Step 2: Add contract tests using mocked HTTP**

Verify request shape, response parsing, metadata capture, and error mapping without real API calls.

- [x] **Step 3: Add opt-in smoke command**

Real API smoke must be skipped unless an explicit local environment variable enables it.

- [ ] **Step 4: Gate**

Run:

```powershell
dotnet test --filter OpenAiProviderContractTests
```

Expected: mocked contract tests pass. Real API tests remain skipped by default.

- [x] **Step 5: Add local cost estimate and quota guard**

Use configurable rate cards and local quota limits. Do not hard-code live provider prices into the domain model.

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

## Task 10: Review Quality Loop

- [x] **Step 1: Add review rubric templates**

Provide reusable domain templates for general image quality, text-heavy posters, and series consistency. The fake review workflow should use the default general template.

- [x] **Step 2: Add structured AI review output**

Normalize provider review results into a domain output with rubric scores, hard failures, comments, suggested fix, and persistable `ReviewResult` conversion.

- [ ] **Step 3: Add structured repair suggestions**
- [ ] **Step 4: Add prompt diff and candidate comparison**
- [ ] **Step 5: Add batch requeue by failure reason**
- [ ] **Step 6: Add human final approval workflow**

## Final Gate

Run:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Expected: all gates pass before any release or real batch generation.
