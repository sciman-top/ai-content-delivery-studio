# Final Visual Delivery Layout

This contract separates user-deliverable images from local work-in-progress files.
It applies to the generic image-series workflow, image edits, scientific figures,
document illustrations, courseware visuals, poster/report visuals, and article
figure sets.

## Default roots

The root is resolved in this order:

1. an explicit `customRoot` passed to the delivery path resolver;
2. `CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT`;
3. `<studio-data-root>/deliveries`, where the studio data root is selected by
   `CONTENT_DELIVERY_STUDIO_DATA_ROOT` or the platform's local-app-data fallback.

The stable category directories are:

| Scenario | Category directory | Candidate/review location |
| --- | --- | --- |
| Generic image series | `image-series` | `workspace/<project-id>/generated` |
| Approved image edits | `image-edits` | `workspace/<project-id>/edited` |
| Article figure sets | `article-figure-sets` | `workspace/article-figure-runs/<run-id>` |
| Scientific figures | `scientific-figures` | workflow state and review evidence under workspace or in-memory package state |
| Document illustrations | `document-illustrations` | document planning/review workspace |
| Courseware visuals | `courseware-visuals` | image-series/workflow workspace |
| Poster/report visuals | `poster-report-visuals` | image-series/composition workspace |

An approved package uses this shape:

```text
<delivery-root>/<category>/<project-id>/<timestamp>/
  images/       final approved image bytes only
  prompts/      prompt snapshots
  metadata/     generation and provenance sidecars
  composition/  deterministic composition evidence
  manifest.json
  manifest.csv
  review-report.md
```

`LocalStudioDataPaths.ResolveFinalDeliveryPackageDirectory` and
`ResolveFinalImageDirectory` are the single path-construction API. The category
is a finite `FinalImageDeliveryCategory` enum, so titles and prompts cannot become
directory names. `DeliveryExportRequest.CreateForFinalDelivery` is the application
request factory for real final-image exports; `DeliveryWorkflowCoordinator` uses it
for the generic route and accepts a category/custom root for specialized routes.
The native WPF review panel exposes the same finite category selector, initializes
the editable root from the configured delivery root, provides a folder picker, and
shows the resolved category directory before export. An invalid root under the
temporary `workspace` leaves the preview empty and disables export. The scientific Gate 2 Save dialog defaults to the
`scientific-figures` category root and still lets the user choose another path.

The deterministic text compositor and article figure scripts deliberately remain
workspace writers. A composed poster, document illustration, or article figure
becomes a final image only when its approved `DeliveryExportItem` is passed through
the categorized delivery request and atomic delivery writer. This keeps a readable
composition or a visual-provider `Pass` from bypassing human approval.

The older `ResolveFinalDeliveryDirectory(projectId, timestamp)` overload remains
as a compatibility fallback for callers that have not yet selected a category; new
final-image code should use the categorized overloads.

## Temporary versus final

Generation, editing, review-prep, crops, contact sheets, blind probes, and
interactive/system visual evidence remain under `workspace/` or an explicitly
named evidence output directory. They are never promoted merely because a visual
provider returned `Pass`. A delivery writer receives only human-approved
`ReviewDecision.Pass` items and writes the final `images/` directory atomically.

Article figure scripts therefore continue to write their candidate runs under
`workspace/article-figure-runs/` by default; the current article candidates remain
`PendingHumanApproval` until the separate scientific Gate 1 and Gate 2 decisions
are complete.

For repository-local runs, [LOCAL_OUTPUTS.md](LOCAL_OUTPUTS.md) defines the visible
`outputs/` classification and the separate ignored `deliveries/` root. In
particular, `outputs/review-ready/` means machine-complete and ready for human
review; it does not mean approved or delivered.

## Local classroom deployment

To keep final assets off `C:\Users\<user>\AppData\Local`, set both roots to the
intended D: locations before starting the app or scripted run:

```powershell
$env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = 'D:\CODE\ai-content-delivery-studio'
$env:CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT = 'D:\CODE\classroom-answer-toolkit\正式交付\科学配图'
```

The application does not hard-code `classroom-answer-toolkit`; another deployment
can select another external root. Existing candidates and delivery assets are not
migrated or deleted automatically.
