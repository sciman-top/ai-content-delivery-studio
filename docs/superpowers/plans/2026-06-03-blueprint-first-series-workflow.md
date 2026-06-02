# Blueprint-First Series Workflow Plan

## Goal

Turn the existing brief-first image workflow into a generalized blueprint-first series workbench without breaking the existing project, queue, review, and delivery model.

## Milestone 1: Domain And Persistence

- Add `DesignBlueprint` domain record.
- Add blueprint category and optional `SeriesItemKind`.
- Persist blueprint selections under project or series scope.
- Keep backward compatibility with existing projects.

## Milestone 2: Fake-First Planning

- Extend the fake planning provider to generate blueprint candidates.
- Add recommendation logic that maps common goals to blueprint families.
- Produce promoted blueprint output that can become a generic series plan.

## Milestone 3: Application Workflow

- Add application service entrypoints for:
  - create brief
  - generate blueprint candidates
  - promote blueprint
  - generate item or panel plan from blueprint
- Record repair-layer outcomes from review.

## Milestone 4: UI

- Expand the Brief tab to show blueprint cards and promotion actions.
- Show which consistency and variation rules come from the promoted blueprint.
- Keep the Plan tab generic: standard items and panels should render through the same list and inspector model.

## Milestone 5: Review And Delivery

- Route review failures back to brief, blueprint, prompt, or settings layers.
- Include promoted blueprint and route metadata in delivery packages.
- Keep human final approval unchanged.

## Verification

- `dotnet build`
- `dotnet test`
- `dotnet format --verify-no-changes`

For early documentation-only slices, gate this implementation work behind fake providers first and keep real provider calls opt-in.
