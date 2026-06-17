# Rename Compatibility Notes

This repository now uses `ContentDeliveryStudio.*` as the active internal solution, project, namespace, and script prefix.

The old `ImageSeriesStudio` name is still expected in a few bounded places.

## Local App-Data Compatibility

- New default local app-data root: `ContentDeliveryStudio`
- Legacy fallback root: `ImageSeriesStudio`

The app may keep using the legacy root when it already exists and the new root has not yet been created. This is intentional so older local-generated folders and review-prep artifacts remain reachable during the rename transition.

## Historical Text That Stays Old

The repository may still contain `ImageSeriesStudio` in:

- historical ADRs
- historical spec and plan documents
- compatibility tests
- this compatibility note
- explicit legacy-alias code paths required for old local data discovery

## Diagnostics And Delivery Evidence

Already exported diagnostics bundles, historical manifests, and older delivery evidence are not retroactively rewritten. They remain valid historical evidence even when they still contain the old internal name.

## Search Expectation

After the rename slice, repository search for `ImageSeriesStudio` should not find ordinary active solution or namespace usage. Remaining hits should be explainable as historical evidence or bounded compatibility behavior.
