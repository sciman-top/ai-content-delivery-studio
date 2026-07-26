# Scientific Figure Supply-Chain Decision Evidence

Date: 2026-07-26

## Scope

This evidence closes Task 1 of the trustworthy-scientific-figure plan at the
research and benchmark-decision boundary. It does not adopt a new runtime
dependency, start scientific workflow implementation, create the 12-item
corpus, or approve a live provider.

Risk: medium. The change establishes future dependency and adapter constraints,
updates enforced reference governance, and refreshes the project-local external
reference shelf. Product code, project package references, schemas, databases,
workspace data, and generated user assets are unchanged.

## Decisions

- Retain `PdfPig 0.1.14`; benchmark stable `0.1.15` only after the corpus
  contract exists.
- Evaluate GROBID `0.9.0-crf` as a pinned Linux container sidecar and Docling
  `2.115.0` as a complementary isolated Windows CLI. Neither is runtime
  approved.
- Generate a repository-owned static SVG subset with .NET `XmlWriter`.
- Advance MathJax `4.1.3` as the formula-to-SVG benchmark candidate through an
  isolated adapter. WpfMath remains a deferred alternative.
- Benchmark Svg.Skia `5.1.1` with the existing SkiaSharp `3.119.4` for PNG/PDF
  exports derived from one SVG authority. Keep resvg and Playwright as test
  oracles only.

The detailed primary-source basis, licenses, versions, maintenance evidence,
security posture, deterministic-output limits, Windows/.NET fit, and rollback
are recorded in
`docs/research/SCIENTIFIC_FIGURE_WORKFLOW_RESEARCH.md`.

## Local Probe Evidence

### Existing extraction and composition

Commands:

```powershell
dotnet test ContentDeliveryStudio.sln --filter "FullyQualifiedName~DocumentExtractionProviderTests"
dotnet test ContentDeliveryStudio.sln --filter "FullyQualifiedName~SkiaDeterministicTextComposerTests"
```

Results:

- extraction tests: exit `0`, `11 / 11` passed
- deterministic composition tests: exit `0`, `2 / 2` passed

A temporary .NET 10 PdfPig `0.1.14` probe generated a two-column PDF. The
current `page.Text` path concatenated adjacent content without spaces.
`ContentOrderTextExtractor` improved this fixture, while `GetWords()` produced
a different row-interleaved order. This proves the current provider cannot be
treated as scholarly reading-order authority.

### SVG export

A temporary .NET 10 probe restored `Svg.Skia 5.1.1` and direct
`SkiaSharp.NativeAssets.Win32 3.119.4`, loaded one 640x360 static SVG into an
`SKPicture`, and exported PNG and PDF twice:

- PNG: byte equal; SHA-256
  `9ab5d6ce628e1a84bba532603d16e30b254020b9dbc7c01ed85f729ab8f0bb24`
- PDF: byte equal; SHA-256
  `9356de9f4daad91d0f0b6b7db916eea94337a0f9e404cac5a869b9929faf6daa`
- `dotnet list package --vulnerable --include-transitive`: exit `0`, no
  vulnerable-package entries

This proves only narrow same-input repeatability on the current host. It does
not prove corpus fidelity, hostile-SVG containment, cross-machine stability,
or an upstream byte-determinism guarantee.

### Formula SVG

A temporary Node `24.12.0` probe restored `mathjax@4.1.3` with
`--ignore-scripts`. The default Windows ESM path reproduced the path-resolution
failure tracked by upstream issue `mathjax/MathJax#3481`. Setting
`loader.paths.mathjax = "mathjax"` made the documented Node conversion work.
Two fresh processes produced the same formula SVG:

- SHA-256
  `2fb54aa0c56a8ca4e41c04e08ae330b0383a30715c64126b9da979360322c099`
- serialized SVG size: `8634` bytes
- `npm audit --omit=dev`: exit `0`, zero known vulnerabilities

This is benchmark evidence, not production-packaging approval.

### GROBID boundary

Read-only probes established:

- Docker CLI `29.5.3` is installed.
- The `grobid/grobid:0.9.0-crf` registry manifest is reachable.
- The Docker Desktop Linux daemon was not running.
- Java was not installed.

No container was pulled or started, and no GROBID extraction was claimed.
Container health, version, sample TEI, resource, and offline-consolidation
checks remain pre-adoption corpus benchmark gates.

## Reference Governance

The external shelf source manifest and README were refreshed under
`D:\CODE\external\ai-content-delivery-studio-references`. Shallow, clean local
references now exist for:

- `05-document-rendering/GROBID` at `f0daa78`
- `05-document-rendering/MathJax` at `fb98717`
- `05-document-rendering/Svg.Skia` at `03f64b6`

The repository machine source adds the required
`scientific-figure-workflow` area. Generated `docs/REFERENCE_BASIS.md` and
`scripts/external-reference-shelf.snapshot.json` are derived through
`scripts/sync-reference-governance.ps1`.

## Compatibility And N/A

- Backward compatibility: preserved; no product contract or runtime behavior
  changed.
- Persistence/schema migration: `gate_na`; no persistence or schema file
  changed. Recovery condition: any future scientific aggregate persistence
  task must supply migration, compatibility, and rollback evidence.
- Paid provider/live call: `gate_na`; Task 1 is fake-first research and no paid
  call is needed. Recovery condition: the opt-in live acceptance tasks require
  explicit approval.
- GROBID live execution: not N/A and not accepted; it remains an explicit
  pre-adoption benchmark gate because the local daemon was unavailable.

## Rollback

Repository rollback reverts only this Task 1 commit, restoring the previous
reference basis, generated reference documentation, shelf snapshot, research
note, and evidence.

External-shelf rollback removes only the three exact new reference directories
and their three manifest/README entries:

- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\GROBID`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\MathJax`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\Svg.Skia`

No Git rollback can or should be used to alter user workspaces or generated
assets; none were changed by this task.

## Repository Gate Evidence

The final closeout used the repository fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
   - exit `0`
   - 0 warnings, 0 errors
2. `dotnet test ContentDeliveryStudio.sln --no-build`
   - exit `0`
   - `461 / 461` passed, 0 failed, 0 skipped
3. Contract and invariant:
   - `scripts/verify-reference-evidence.ps1`: exit `0`
   - `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`:
     exit `0`
4. `scripts/preflight-release.ps1 -NoRestore`
   - exit `0`
   - reference parity, placeholder/conflict scans, canonical repository
     verification, publish WhatIf, and diff hygiene passed

Task-specific `scripts/sync-reference-governance.ps1 -Check`,
`scripts/verify-reference-evidence.ps1`, and `git diff --check` also returned
exit `0`.
