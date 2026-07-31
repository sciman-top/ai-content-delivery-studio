# Scientific Figure Workflow Supply-Chain Research

Last reviewed: 2026-08-01.

## Status And Decision Boundary

This note records the Task 1 supply-chain decision for the trustworthy
scientific-figure workflow. It is research and benchmark authorization, not
runtime-adoption evidence.

The repository remains fake-first. This task:

- makes no paid provider calls
- adds no runtime dependency
- adds no container, model, browser, font, or executable to the product;
  isolated temporary probes may restore exact packages and then remove them
- does not approve GROBID, Docling, WpfMath, MathJax, Svg.Skia, resvg, or
  Playwright for production use
- does not upgrade the current `PdfPig 0.1.14` or `SkiaSharp 3.119.4`

The source authority remains:

```text
source bytes and hash
  > located raw evidence
  > approved claims and ScientificFigureSpec
  > deterministic render plan
  > SVG authority
  > PNG/PDF exports
```

An extractor or renderer is an adapter below this authority. Tool success must
never be converted into scientific acceptance.

## Executive Decision

| Area | Decision | Version strategy | Adoption state |
| --- | --- | --- | --- |
| Local PDF extraction | Retain PdfPig and benchmark the next stable release | Keep repository `0.1.14`; evaluate stable `0.1.15` separately; reject `0.1.16-alpha-*` | Existing runtime retained |
| Scholarly structure | Evaluate GROBID through a sidecar adapter | Pin `0.9.0-crf` image by digest and configuration | Benchmark approved, runtime not approved |
| Windows extraction complement | Evaluate Docling through an isolated CLI/JSON adapter | Pin `2.115.0`, wheel/model hashes, device and pipeline settings | Complementary benchmark only |
| SVG production | Generate a repo-owned static SVG subset with .NET `XmlWriter` | Bind serializer version to the render-plan schema and .NET 10 runtime | Selected design; no new package |
| Formula rendering | Advance a pinned MathJax SVG adapter; defer WpfMath because it has no ready SVG exporter | MathJax `4.1.3`; pin package, extensions, and fonts | MathJax local probe passed; runtime not approved |
| SVG export | Benchmark Svg.Skia against a fixed export corpus | Svg.Skia `5.1.1` with current SkiaSharp `3.119.4` | Preferred benchmark, runtime not approved |
| PNG oracle | Keep resvg as a non-runtime comparison candidate | Pin `0.47.0` executable/library and fonts | Oracle only |
| Browser fallback | Keep Playwright out of the production export path | Pin `Microsoft.Playwright 1.61.0` plus browser revision | Test fallback only |

No upstream project reviewed here promises bit-for-bit stability for every
output relevant to this workflow. Determinism is therefore a repository-owned
contract proved by pinned inputs, versions, fonts, native assets, settings,
canonicalization, repeated runs, semantic checks, and pixel comparisons.

## Current Repository And Host Evidence

The current project targets `net10.0-windows` for the WPF app and tests and
`net10.0` for non-UI projects. The Infrastructure project currently references
`PdfPig 0.1.14` and `SkiaSharp 3.119.4`.

Isolated feasibility probes on 2026-07-26 established:

| Probe | Result | Meaning |
| --- | --- | --- |
| `dotnet test ... --filter DocumentExtractionProviderTests` | 11 passed, 0 failed | Current PdfPig-based extraction baseline runs locally |
| PdfPig two-column synthetic PDF | `page.Text` concatenated adjacent content without spaces; `ContentOrderTextExtractor` restored left-column then right-column lines; `GetWords()` interleaved rows visually | Current provider cannot be a scholarly reading-order authority |
| `dotnet test ... --filter SkiaDeterministicTextComposerTests` | 2 passed, 0 failed | Current Skia composition baseline runs locally |
| .NET SDK/runtime | SDK `10.0.302`; Windows Desktop Runtime `10.0.10` present | Host can run the current target |
| `dotnet --info` workload enumeration | Installer helper threw `TypeInitializationException` after printing SDK/runtime data | Host workload reporting is partially unhealthy; this is not exporter evidence |
| Docker / GROBID | CLI `29.5.3` present; registry manifest reachable; Docker Desktop Linux daemon unavailable | GROBID container execution was not run |
| Java | Not found | Native GROBID build/service probe was not run |
| Python / Docling | Python `3.13.7` present; Docling not installed | Docling conversion probe was not run |
| Node / MathJax | Node `24.12.0`; isolated `mathjax@4.1.3 --ignore-scripts`; configured conversion passed in two fresh processes | One representative formula SVG was byte-stable locally; npm audit reported 0 vulnerabilities |
| WpfMath | Package `2.1.0` not cached | Formula benchmark was not run |
| Svg.Skia | Isolated .NET 10 probe used `Svg.Skia 5.1.1` and current SkiaSharp Win32 `3.119.4`; two exports matched for both PNG and PDF | Narrow same-input repeatability proved; package is still not referenced by the solution |
| resvg / Inkscape | Executables not found | Native exporter comparison was not run |

The successful probes prove only the stated inputs, versions, host, and
settings. They do not prove the 12-item corpus, cross-machine stability,
malicious-input resistance, or production packaging. The remaining GROBID,
Docling, WpfMath, resvg, and browser probes are explicit pre-adoption
benchmark gates, not evidence already supplied by Task 1. They must not be
reported as `platform_na`: the candidates are available in principle, but
this docs-only slice intentionally did not adopt them.

## 1. PDF And Scholarly Extraction

### 1.1 PdfPig: Retain The Coordinate-Level Baseline

Primary sources:

- [PdfPig v0.1.15 release](https://github.com/UglyToad/PdfPig/releases/tag/v0.1.15)
- [PdfPig v0.1.15 package metadata](https://www.nuget.org/packages/PdfPig/0.1.15)
- [PdfPig README and layout-analysis examples](https://github.com/UglyToad/PdfPig/blob/v0.1.15/README.md)
- [PdfPig target frameworks](https://github.com/UglyToad/PdfPig/blob/v0.1.15/src/UglyToad.PdfPig/UglyToad.PdfPig.csproj)
- [Apache-2.0 license](https://github.com/UglyToad/PdfPig/blob/v0.1.15/LICENSE)

**Capabilities and limits.** PdfPig exposes pages, letters, words, images,
glyph rectangles, baselines, and document-layout algorithms. Its own README
warns that `page.Text` preserves internal PDF content order, which is rarely
the desired reading order. The library provides nearest-neighbour word
extraction, Docstrum page segmentation, and reading-order detection, but these
remain layout heuristics. A PDF is a presentation format; PdfPig does not by
itself establish scholarly section semantics, formula meaning, caption
ownership, or scientific correctness.

The local synthetic two-column probe makes this limitation concrete. For a
page containing two lines in each column, `page.Text` returned:

```text
LEFT-1 Force balance F = maLEFT-2 Evidence condition: frictionlessRIGHT-1 Thermal relation Q = mc deltaTRIGHT-2 Evidence condition: closed system
```

`ContentOrderTextExtractor.GetText` recovered left line 1, left line 2, right
line 1, right line 2 as separate lines, while `page.GetWords()` followed visual
rows and interleaved left line 1, right line 1, left line 2, right line 2. The
current
[`LocalBinaryDocumentExtractionProvider`](../../src/ContentDeliveryStudio.Infrastructure/Sources/LocalBinaryDocumentExtractionProvider.cs)
uses `page.Text` and only collapses whitespace; it therefore preserves the
no-space concatenation. A content-order extractor improves this fixture but
still does not prove scholarly semantics.

**License and version.** The package is Apache-2.0. Stable `0.1.15` was
published on 2026-06-25 from upstream commit
`f131f642976936e06ee91cb19d3ed728f9dd18b6`. Its NuGet SHA-512 is
`M5PHyQHujFuKMOuLQhyp9LNQz36E6r/qrCl86B/YCwM7gCxjW4IFQTxzOnbdoi1BVuvA2LFCJV93+TjjfEtKpg==`.
The repository currently uses `0.1.14`; pre-1.0 API and extraction drift must
be assumed.

**Maintenance.** The project published a stable release in June 2026 and had
upstream activity in July 2026. That is a positive maintenance signal, not an
API-stability guarantee.

**Security and supply chain.** `0.1.15` publishes `net8.0`,
`netstandard2.0`, and other compatible assets and has no third-party NuGet
dependency in its `net8.0` group. Untrusted PDFs still require limits for file
size, page count, decompression, elapsed time, memory, and malformed objects.
NuGet metadata showed no deprecation or known-vulnerability flag at review
time, but upstream has no public security-response contract that substitutes
for repository scanning.

**Determinism.** Upstream makes no bit-output promise. Pin the package; sort and
round coordinates in a repo-owned canonicalizer; preserve raw strings and
source hashes; and compare block order, coordinates, and diagnostics over the
fixed corpus.

**Windows/.NET 10 fit.** This is already proved at the current `0.1.14`
baseline by the local 11-test probe. The `net8.0` asset in `0.1.15` is
compatible with a `net10.0` consumer, but the upgrade itself is not yet proved.

**Rollback.** Keep `IScientificDocumentExtractor` provider-neutral. A failed
upgrade returns to `0.1.14` without changing source, claim, or evidence
schemas. A parsing failure returns a structured blocked result; it never falls
through to guessed content.

**Decision.** Retain `0.1.14`. Admit `0.1.15` only to an extraction-drift
benchmark after the 12-item corpus contract exists. Do not combine a PdfPig
upgrade with scientific workflow implementation.

### 1.2 GROBID: Scholarly Sidecar Candidate

Primary sources:

- [GROBID 0.9.0 release](https://github.com/grobidOrg/grobid/releases/tag/0.9.0)
- [GROBID README, requirements, capabilities, and license](https://github.com/grobidOrg/grobid/blob/0.9.0/Readme.md)
- [Installation requirements](https://github.com/grobidOrg/grobid/blob/0.9.0/doc/Install-Grobid.md)
- [REST service and TEI options](https://grobid.readthedocs.io/en/latest/Grobid-service/)
- [Apache-2.0 license](https://github.com/grobidOrg/grobid/blob/0.9.0/LICENSE)

**Capabilities and limits.** GROBID is designed for technical and scientific
publications. `/api/processFulltextDocument` returns TEI XML for header, body,
references, figures, tables, and formulas. `teiCoordinates` can request PDF
coordinates for supported structures. TEI is the richest response and must
remain the adapter input; Markdown/JSON client projections are lossy.

GROBID reports typed failures for unreadable, scanned, oversized, timed-out,
or pdfalto-failed documents. Those diagnostics map well to the repository's
fail-closed contract. GROBID does not turn extracted statements into approved
scientific claims.

**License and version.** Code is Apache-2.0; documentation and annotated data
have separate licenses recorded upstream. Stable `0.9.0` was released on
2026-04-07. The benchmark image is `grobid/grobid:0.9.0-crf`, pinned by
manifest digest
`sha256:24ba90eb1c959f65d812bcdb2cf79c677fa5fd7b95235de616b8bc9fa1317849`.

**Maintenance.** The project describes steady long-term development and had
upstream activity in July 2026. It is production-used, but upstream also
states that it is maintained as a side project.

**Security and supply chain.** Source builds require OpenJDK 21 and native
pdfalto. Deep-learning modes add Python/JEP, models, and optionally CUDA.
Default CRF mode is the smallest evaluation surface. Bind the service to
loopback, apply request and resource limits, record image digest and model
configuration, and scan the image before use. Set `consolidateHeader=0`,
`consolidateCitations=0`, and `consolidateFunders=0`; otherwise consolidation
can call Crossref or biblio-glutton and violate the source-only/offline
boundary.

**Determinism.** GROBID makes no universal bit-for-bit TEI guarantee. Pin the
image, CRF models, pdfalto, JDK, configuration, locale, and request options;
canonicalize TEI before comparison; store raw TEI hash; and measure repeated
output over the corpus.

**Windows/.NET 10 fit.** Upstream supports Linux and macOS native builds and
explicitly does not guarantee Windows support. The only acceptable first
integration is an optional Linux-container sidecar behind `HttpClient` and a
repo-owned TEI adapter. The local Docker daemon was unavailable, so service
health, version, timeout, and sample-TEI probes remain open.

**Rollback.** Remove or disable the sidecar configuration and continue with
PdfPig/fake extraction. No TEI type crosses the adapter boundary. If required
scholarly structure is absent, the workflow blocks rather than silently
degrading.

**Decision.** `benchmark-approved/runtime-not-approved`. Compare
`0.9.0-crf` against PdfPig on section order, caption/formula locations,
evidence anchors, failures, latency, and repeatability before any dependency
or deployment change.

### 1.3 Docling: Windows Complement, Not GROBID Replacement

Primary sources:

- [Docling README](https://github.com/docling-project/docling/blob/main/README.md)
- [Docling 2.115.0 package metadata](https://pypi.org/project/docling/2.115.0/)
- [Docling CLI reference](https://github.com/docling-project/docling/blob/main/docs/reference/cli.md)
- [Advanced options and model artifacts](https://github.com/docling-project/docling/blob/main/docs/usage/advanced_options.md)
- [MIT code license](https://github.com/docling-project/docling/blob/main/LICENSE)

**Capabilities and limits.** Docling advertises PDF layout, reading order,
tables, formulas, image classification, lossless JSON, local execution, and
Windows support. Its own roadmap still distinguishes broader scholarly
metadata capabilities, so it must not be represented as a complete GROBID
replacement. OCR and visual-model modes are outside the first slice.

**License and version.** Code is MIT. Current `2.115.0` was published on
2026-07-23 and requires Python `>=3.10,<4.0`. The wheel SHA-256 is
`1a3d9bdf2f82610e97085a1a1b53cf259d1bd7aff97651ff2decc3b2b105123c`.
Models and their assets retain their own licenses and must be inventoried
separately.

**Maintenance.** The current release and active upstream repository are strong
maintenance signals. Its fast cadence also increases drift risk and makes
exact version/model pinning mandatory.

**Security and supply chain.** Use an isolated Python environment with a
hash-locked dependency graph and a fixed local `--artifacts-path`. Record every
model name, version, license, and digest. Keep remote services and external
plugins disabled, prohibit URL input in the adapter, and run processing
without network access. Python package metadata showed 11 direct dependency
entries; transitive and native-model scope must be reviewed before adoption.

**Determinism.** No general deterministic-output promise was found. Pin
Python, package, models, device, pipeline, accelerator, OCR mode, locale, and
options; canonicalize lossless JSON; and compare repeated extraction.

**Windows/.NET 10 fit.** Upstream supports Windows x86_64/arm64 and a CLI/JSON
boundary. The host has Python 3.13.7 but not Docling, so no sample conversion
was run. Python objects must never enter Core or Application contracts.

**Rollback.** Remove the isolated environment and adapter registration.
PdfPig remains the local baseline and GROBID remains independently optional.

**Decision.** `benchmark-approved/complementary-only`. Evaluate layout,
reading-order, table, figure, and formula recovery on Windows. Do not use it
to bypass the OCR non-goal or GROBID's scholarly-structure benchmark.

### 1.4 Extractor Adapter Contract

All candidates must project into one repo-owned result that records:

- input SHA-256 and byte length
- adapter identity, package/container/model versions, configuration, and
  executable hashes
- page, block, raw text, offsets, and coordinates before normalization
- reading-order, encoding, formula, table, figure, and caption diagnostics
- timeout, truncation, unsupported, scanned, and malformed states
- raw tool-output hash plus canonical projection hash

The adapter must not merge GROBID or Docling output into PdfPig output by
guessing. Conflicts are explicit and block corpus admission or Gate 1.

## 2. Deterministic SVG Production

Primary sources:

- [W3C SVG 1.1 specification](https://www.w3.org/TR/SVG11/)
- [W3C SVG 2 specification](https://www.w3.org/TR/SVG2/)
- [.NET 10 `XmlWriter` API](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlwriter?view=net-10.0)
- [.NET 10 `XmlWriterSettings` API](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlwritersettings?view=net-10.0)
- [.NET runtime MIT license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)

### Decision

Generate SVG with a repo-owned forward-only `XmlWriter` serializer. Use SVG
1.1 as the compatibility baseline and admit only individually tested static
SVG 2 features. This introduces no third-party package and keeps stable domain
IDs, evidence links, and element ordering under repository control.

### License, Version, And Maintenance

`System.Xml` is part of the .NET runtime under MIT. The serializer is bound to
the repository's render-plan schema version and the .NET 10 runtime. W3C SVG
defines interoperability; the repository owns the narrower executable subset.

### Security And Supply Chain

`XmlWriter` encodes text and attributes, but it does not make arbitrary SVG
safe. The renderer must emit from typed fields only and reject raw markup.
Initially allow only the minimum required static elements and attributes.
Prohibit:

- `script`, event attributes, animation, and JavaScript URLs
- `foreignObject`
- external HTTP/HTTPS/file references and CSS `@import`
- arbitrary embedded HTML, XML entities, or unapproved data URLs
- fonts or images that are not package-owned, licensed, and hash-pinned

Every post-render SVG is parsed with DTD processing prohibited, checked
against the static-subset allowlist, and required to resolve every relation,
marker, clip, and internal reference locally.

### Deterministic-Output Contract

The serializer fixes:

- UTF-8 encoding without BOM, XML declaration policy, LF newlines, and
  namespace declarations
- element and attribute order
- layer and item order derived from stable render-plan IDs
- `InvariantCulture` number formatting, precision, and rounding
- lowercase color notation and canonical opacity
- stable IDs derived from specification IDs, never timestamps or random GUIDs
- view box, dimensions, DPI interpretation, and accessibility-node order
- no environment-derived metadata

Exact SVG bytes and SHA-256 are expected to be stable for an identical
serializer version, render plan, and approved asset/font set. Tests also parse
the XML and compare semantics so attribute order is never mistaken for SVG
meaning.

### Windows/.NET 10 Fit And Rollback

The approach is BCL-native on both `net10.0` and `net10.0-windows`.
`IScientificFigureRenderer` isolates the serializer. Rollback swaps or removes
only the renderer implementation; `ScientificFigureSpec`, `SvgRenderPlan`,
stable IDs, and approval state remain compatible.

## 3. Formula Rendering

Formula text is scientific authority. A renderer may typeset only an approved
normalized formula string; unsupported commands, missing glyphs, ambiguity,
or rendering drift produce a blocking diagnostic.

### 3.1 WpfMath/XAML-Math 2.1.0: Rejected Primary, Deferred Alternative

Primary sources:

- [WpfMath 2.1.0 package](https://www.nuget.org/packages/WpfMath/2.1.0)
- [XAML-Math rendering API and PNG example](https://github.com/ForNeVeR/xaml-math/blob/v2.1.0/README.md)
- [`IElementRenderer` extension boundary](https://github.com/ForNeVeR/xaml-math/blob/master/src/XamlMath.Shared/Rendering/IElementRenderer.cs)
- [WPF rendering implementation](https://github.com/ForNeVeR/xaml-math/blob/master/src/WpfMath/Rendering/WpfTeXFormulaExtensions.cs)
- [Font licenses](https://github.com/ForNeVeR/xaml-math/blob/master/fonts/LICENSES.md)
- [Project changelog](https://github.com/ForNeVeR/xaml-math/blob/master/CHANGELOG.md)

**Capabilities and limits.** WpfMath parses a LaTeX-style formula model and
renders through WPF. It exposes `IElementRenderer`, which makes a repo-owned
SVG-path renderer technically possible. The documented built-in convenience
path targets PNG/bitmap, not editable SVG. "LaTeX-style" must not be widened
to "complete LaTeX"; a representative formula corpus must define supported
commands.

**License and version.** Stable `2.1.0` was released in July 2023 and targets
`net6.0-windows7.0` and .NET Framework. Its NuGet license expression is
`MIT AND OFL-1.1`, while bundled Computer Modern and other font files carry
additional upstream terms recorded in `fonts/LICENSES.md`. Package SHA-512 is
`6CCtNZILxEKAqu64R4EYYN8adkq92X2EPpF6dQj3XAekXDi5s16qwMt3v/QLbS/StuHvVq/LxUUNeISlpoiBOw==`.

**Maintenance.** Source activity continued in 2026, but `3.0.0` remains
unreleased in the upstream changelog. The stable release cadence is therefore
slow and must be treated as a maintenance risk.

**Security and supply chain.** A release adds managed code plus bundled fonts.
Review all font notices, package signature/hash, command parser limits, and
resource exhaustion for deeply nested formulas. No public security response
commitment or deterministic-output promise was found.

**Determinism.** Pin package and fonts, reject fallback to host fonts, fix DPI
and size, and compare parsed formula, geometry/path commands, baselines, bounds,
and pixels across repeated runs. A bitmap fallback is not acceptable in the
editable SVG authority.

**Windows/.NET 10 fit.** `net10.0-windows` can consume the Windows target, but
the dependency must stay in a Windows-specific formula adapter. It must not
leak WPF into the current platform-neutral `net10.0` Core, Application, or
Infrastructure contracts.

**Rollback.** Remove adapter registration and package. Preserve formula source
and return `formula_renderer_unavailable`; never replace the formula with a
plain-text approximation in an approved figure.

**Decision.** Reject it as the primary first-slice renderer and defer it. It
has no ready SVG exporter, would push WPF/Windows coupling into the renderer
implementation, and received no local SVG feasibility proof in this task.
Reconsider only if MathJax fails the fixed corpus. Any reconsideration would
require a proved SVG-path adapter, supported-command policy, font-license
record, missing-glyph diagnostics, and repeated-run equivalence.

### 3.2 MathJax 4.1.3: Preferred SVG Benchmark

Primary sources:

- [MathJax 4.1.3 release](https://github.com/mathjax/MathJax/releases/tag/4.1.3)
- [MathJax package metadata](https://www.npmjs.com/package/mathjax/v/4.1.3)
- [MathJax in Node](https://docs.mathjax.org/en/latest/server/start.html)
- [SVG output options](https://docs.mathjax.org/en/latest/options/output/svg.html)
- [SVG output model](https://docs.mathjax.org/en/latest/output/index.html)
- [Windows path-resolution issue #3481](https://github.com/mathjax/MathJax/issues/3481)
- [Apache-2.0 license](https://github.com/mathjax/MathJax/blob/4.1.3/LICENSE)

**Capabilities and limits.** MathJax supports TeX/MathML input and an SVG
output processor. SVG characters are paths. `fontCache` can be `local`,
`global`, or `none`; `local` keeps each expression self-contained and
`localID` controls the path-ID prefix. This makes it a useful independent
formula-to-SVG oracle.

**License and version.** `mathjax 4.1.3` is Apache-2.0 and was published on
2026-07-03. Its npm integrity is
`sha512-BN/8Pkgn7G1pIDYJqd9md+JHsE/jydSYbyOZnSdSA0WziuVO8mRxdYiWFumkVVly/8U+hm9DpIIoWuvySverzw==`.
The package depends on `@mathjax/mathjax-newcm-font` with a compatible range,
so a lockfile and exact transitive resolution are mandatory.

**Maintenance.** The July 2026 release and active upstream project are strong
signals. They do not make generated IDs, geometry, or pixels stable across
upgrades.

**Security and supply chain.** MathJax adds a Node runtime, npm dependency
graph, fonts, and process boundary. Use an offline lockfile install from an
approved cache, verify integrity/provenance, disable network during execution,
bound input and time, and accept only the configured TeX packages.

**Determinism.** Pin Node, MathJax, font package, loaded extensions, scale,
display mode, `fontCache`, and `localID`. Canonicalize the SVG fragment and
compare paths/bounds/pixels. No upstream bit-for-bit guarantee was found.

**Windows/.NET 10 fit.** Node 24.12 is present locally, but MathJax is not
an in-process .NET dependency. Integration is a pinned out-of-process adapter.
An isolated `npm install mathjax@4.1.3 --ignore-scripts` probe found a Windows
packaging caveat: the official-style initialization with
`load: ['input/tex', 'output/svg']` treated a `C:` path as a URL scheme, the
same failure class recorded in upstream issue #3481. Adding
`loader.paths = { mathjax: 'mathjax' }` resolved package loading.

With that setting, the representative formula
`F_net=ma, Q=mc DeltaT` produced an 8,634-byte SVG. Two fresh Node processes
produced the same SHA-256:
`2fb54aa0c56a8ca4e41c04e08ae330b0383a30715c64126b9da979360322c099`.
`npm audit` reported 0 vulnerabilities for the isolated probe. This proves one
local formula and configuration, not full formula coverage or long-term
determinism.

**Rollback.** Remove the tool adapter and locked npm payload. The formula
contract and deferred WpfMath alternative remain unchanged.

**Decision.** Selected formula-to-SVG benchmark candidate because it emits
editable paths and now has a local Windows proof. Runtime adoption still
requires the fixed formula corpus, exact dependency/font lock, offline
packaging, stable-ID configuration, process limits, accessibility output, and
repair/rollback tests. WpfMath remains deferred behind the same formula
adapter contract and is not a gate for the MathJax decision.

## 4. SVG-To-PNG/PDF Export

Exports must consume the exact approved SVG bytes/hash. Re-rendering directly
from `SvgRenderPlan` is not an export from the SVG authority.

### 4.1 Svg.Skia 5.1.1: Preferred .NET Benchmark

Primary sources:

- [Svg.Skia 5.1.1 package](https://www.nuget.org/packages/Svg.Skia/5.1.1)
- [Svg.Skia README and PNG/PDF examples](https://github.com/wieslawsoltes/Svg.Skia/blob/v5.1.1/README.md)
- [`net10.0` target and package metadata](https://github.com/wieslawsoltes/Svg.Skia/blob/v5.1.1/src/Svg.Skia/Svg.Skia.csproj)
- [MIT license](https://github.com/wieslawsoltes/Svg.Skia/blob/v5.1.1/LICENSE.TXT)
- [Security policy](https://github.com/wieslawsoltes/Svg.Skia/blob/v5.1.1/SECURITY.md)
- [SkiaSharp PDF document API](https://github.com/mono/SkiaSharp/blob/main/binding/SkiaSharp/SKDocument.cs)

**Capabilities and limits.** Svg.Skia loads SVG into a Skia picture and
documents direct PNG and PDF saves. It uses SVG 1.1 as a baseline with a tested
static SVG 2 subset. JavaScript is disabled by default, but the loader can
resolve local, file, HTTP, and data resources unless the host applies a strict
resource policy.

**License and version.** `5.1.1` is MIT, was published on 2026-06-15, and
includes a `net10.0` asset from commit
`3261e036769d0deba1f621265c7ab7000e3ce470`. Package SHA-512 is
`iniYR59sYuWAN0UXDDTD5onpKSBaNAnE8essRlX9fKvrpRalLhpJiv4S3zLRmNlymEyce0OnwqImSM0eJDcfaw==`.

**Maintenance.** The current release, explicit .NET 10 target, SVG test suites,
and public security policy are positive signals. The security policy does not
promise a response SLA.

**Security and supply chain.** The `net10.0` package depends on several
`Svg.* 5.1.1` packages, HarfBuzzSharp `8.3.1.3` and native assets for multiple
platforms, and SkiaSharp `3.119.2` or later. The repository's
`SkiaSharp 3.119.4` satisfies that floor, so this benchmark must not trigger a
SkiaSharp major upgrade. Before adoption, enable NuGet lock-file mode, pin
resolved versions/hashes, restrict the loader to in-memory approved SVG and
package-owned assets, and disable JavaScript and external resolution.

**Determinism.** Upstream does not promise identical PNG/PDF bytes. Fix native
Skia/HarfBuzz libraries, fonts, DPI, dimensions, background, color space, and
metadata. Require repeated PNG pixel equivalence. For PDF require page,
dimension, vector/text/path, formula/label/arrow, and rasterized visual
equivalence; require byte identity only if the benchmark proves it.

**Windows/.NET 10 fit.** The package directly targets `net10.0` and has Win32
native dependencies. It is the closest fit to the current SkiaSharp stack.
The package is cached locally, but no solution reference was added in this
task.

The isolated feasibility probe used a .NET 10 console, `Svg.Skia 5.1.1`,
direct `SkiaSharp.NativeAssets.Win32 3.119.4`, and a 640x360 SVG containing a
circle and two paths. The command shape was
`SKSvg.Load -> SKPicture -> SKBitmap/PNG` and
`SKSvg.Load -> SKPicture -> SKDocument.CreatePdf`. Two export rounds produced:

- PNG equal: `true`; SHA-256
  `9ab5d6ce628e1a84bba532603d16e30b254020b9dbc7c01ed85f729ab8f0bb24`
- PDF equal: `true`; SHA-256
  `9356de9f4daad91d0f0b6b7db916eea94337a0f9e404cac5a869b9929faf6daa`

The isolated NuGet vulnerable-package scan reported no entries. This proves
only this simple same-input host probe. It does not prove formula/font
fidelity, cross-process or cross-machine behavior, complex SVG coverage,
external-resource blocking, or the fixed corpus.

**Rollback.** Remove the package and exporter registration. Keep approved SVG
and its hash deliverable. Do not substitute a second render-plan renderer.

**Decision.** Preferred benchmark for both PNG and PDF. Adoption requires
static-resource enforcement, locked dependency resolution, representative
formula/font fixtures, repeated-run evidence, and cross-format invariant
tests.

### 4.2 resvg 0.47.0: PNG Oracle

Primary sources:

- [resvg 0.47.0 package metadata](https://docs.rs/crate/resvg/0.47.0)
- [resvg architecture, static subset, reproducibility, and license](https://github.com/linebender/resvg/blob/v0.47.0/README.md)

**License and version.** `0.47.0` is dual `Apache-2.0 OR MIT`, published in
February 2026, and requires Rust 1.87 when built from source.

**Maintenance.** The project is active, has an extensive SVG-to-PNG regression
suite, and explicitly targets portable static SVG rendering.

**Security and supply chain.** A Rust/C/CLI integration adds a native binary
and the `usvg`, font parser, and raster stack. Pin executable and source
checksums, run without network, restrict fonts/resources, and sandbox
untrusted SVG. Its static-only model excludes scripting and animation.

**Determinism.** Upstream states that avoiding system rendering libraries
allows reproducible results across supported platforms. Repository tests must
still prove the exact approved subset, font set, CPU architecture, and output
settings. It produces PNG, not the required PDF.

**Windows/.NET 10 fit.** A CLI/C boundary can work on Windows but is not a
managed .NET 10 integration. `resvg` is not installed locally.

**Rollback and decision.** No runtime adoption. Use only as a PNG comparison
oracle if a pinned binary is approved in a later benchmark. Removal has no
effect on the Svg.Skia path or SVG authority.

### 4.3 Microsoft.Playwright 1.61.0: Browser Test Fallback

Primary sources:

- [Microsoft.Playwright 1.61.0 package](https://www.nuget.org/packages/Microsoft.Playwright/1.61.0)
- [Browser installation and version coupling](https://playwright.dev/dotnet/docs/browsers)
- [`Page.ScreenshotAsync`](https://playwright.dev/dotnet/docs/api/class-page#page-screenshot)
- [`Page.PdfAsync`](https://playwright.dev/dotnet/docs/api/class-page#page-pdf)
- [MIT license](https://github.com/microsoft/playwright-dotnet/blob/v1.61.0/LICENSE)

**License and version.** The .NET package is MIT. `1.61.0` was published on
2026-06-23 from upstream commit
`3c0f3289febd698f331ca616b0e269bdf491da79`; NuGet SHA-512 is
`aGRaEVz55vAAkEmIsENCm2IhZuE2+BqJtqmBrec/fxQad5pyWdWceQ+svJsseaZr09xCLb9XVxNkgL4wreOlKg==`.

**Maintenance.** Microsoft releases Playwright frequently and couples each
package to browser revisions. That improves currency but creates regular
browser-binary churn.

**Security and supply chain.** The package requires separately installed
browser binaries. Use a pinned Chromium revision, isolated context, network
blocking, local in-memory content, no navigation, and bounded process
resources. Browser attack surface and binary size are materially larger than
Svg.Skia.

**Determinism.** Screenshots and printed PDF depend on browser revision, fonts,
OS rendering, CSS print behavior, device scale, color mode, and metadata.
Playwright provides no general byte-reproducibility contract for this use.

**Windows/.NET 10 fit.** The package is .NET-compatible and Chromium runs on
Windows, but neither package nor required browser is present in the solution.
The local global Node Playwright version is unrelated to a pinned .NET
integration.

**Rollback and decision.** Keep it out of production export. It may render
cross-format fixtures as an independent browser oracle during tests. Removing
the oracle cannot change approved artifacts.

## 5. Required Benchmark Before Runtime Adoption

No new dependency may enter the solution until a separate benchmark slice
records all of the following.

### Extractors

- the same redistribution-safe PDFs through PdfPig `0.1.14`, PdfPig `0.1.15`,
  GROBID `0.9.0-crf`, and Docling `2.115.0`
- reading order, section/caption/formula/table locations, exact source
  anchoring, failure classification, latency, memory, and repeated output
- offline behavior with every external consolidation/plugin/service disabled
- raw output hashes, canonical projection hashes, versions, models, and config
- explicit corpus item admission or rejection; no average score can hide a
  critical anchor failure

### Formula

- a versioned set covering fractions, superscripts/subscripts, roots, Greek
  symbols, vectors, matrices, relations, units, multiline formulas, and
  intentionally unsupported commands
- MathJax supported-command coverage, two-process repeatability, pinned font
  cache/IDs, and accessibility output
- if MathJax fails and WpfMath is reconsidered, WpfMath parser coverage and a
  non-raster SVG-path proof; this is not a gate for MathJax adoption
- font inventory, licenses, hashes, missing-glyph behavior, bounds, baseline,
  accessibility text, repeated geometry, and pixels

### Export

- exact approved SVG bytes as the only input
- the repository static SVG subset, including markers, clipping, labels,
  formula paths, accessibility metadata, and approved embedded assets
- Svg.Skia PNG/PDF output compared with resvg PNG and browser-rendered
  fixtures where useful
- two or more identical-process runs and a fresh-process run
- fixed fonts, native assets, DPI, dimensions, background, color space, and
  metadata
- SVG semantic equality, PNG pixel thresholds, PDF structure/page/dimension
  checks, and rasterized PDF visual equality
- malicious/unsupported SVG fixtures proving no script, network, file escape,
  external font, or unbounded resource access

### Supply Chain

- exact direct and transitive versions
- package/container/executable/model/font hashes and licenses
- NuGet lock-file mode for any adopted managed package
- vulnerability and provenance scan evidence
- supported-platform matrix and packaged native assets
- owner, update cadence, rollback command, and recovery condition

## 6. Final Recommendation And Rollback Map

1. Keep PdfPig `0.1.14` as the extraction baseline and make all scholarly
   structure adapters optional.
2. Benchmark GROBID `0.9.0-crf` for scholarly TEI and Docling `2.115.0` for a
   Windows-local complementary projection; do not install either in default
   repository gates.
3. Implement deterministic SVG later with a repo-owned `XmlWriter` static
   subset and stable IDs. This is the only selected production approach in
   this note that does not still require dependency feasibility evidence.
4. Advance MathJax `4.1.3` as the selected formula-to-SVG benchmark candidate
   now that the configured Windows probe passes. Keep WpfMath `2.1.0` rejected
   as primary and deferred unless MathJax fails the corpus. Do not accept
   rasterized formula authority.
5. Benchmark Svg.Skia `5.1.1` with the existing SkiaSharp `3.119.4`. Use resvg
   or Playwright only as independent test oracles, not production fallbacks.
6. Preserve SVG as the editable authority even when all exporters are
   unavailable. A missing exporter blocks PNG/PDF delivery; it does not
   invalidate an approved SVG or trigger a scientifically different render.

Rollback always removes only the optional adapter/package/tool and its
registration. It never rewrites source evidence, approved claims,
`ScientificFigureSpec`, stable render-plan IDs, or the approved SVG hash.

## 7. Article-Candidate Integration Follow-Up (2026-07-31)

The article-level candidate slice reuses the already retained `PdfPig 0.1.14`
extractor and the repository-owned static SVG approach. It adds neither a
package nor an external executable to the product. The sample article route
produces located, high-risk proposals and an explicitly non-final optical SVG
preview; it does not invoke a provider, perform OCR, or claim scholarly
structure recovery beyond the PDF text layer.

The implementation deliberately keeps the existing authority boundary intact:

```text
article PDF and hash
  > page/block evidence for candidate proposals
  > explicit human Gate 1 approval of claims and ScientificFigureSpec
  > deterministic approved SVG and existing review/delivery workflow
```

The preview is labelled as a non-proportional Gate 1 candidate, so it cannot
be exported as an approved deliverable or used to infer that article assertions
about eye focal length, accommodation, perceived orientation, or clarity have
been independently confirmed. This is an adoption decision for the existing
repository contracts, not a change to the GROBID, Docling, MathJax, Svg.Skia,
or resvg decisions above.

The article run also exposed a Windows/Skia text-rendering requirement: a
requested UI font can omit CJK glyphs, and Unicode subscripts may be absent
from the CJK fallback font. The repository exporter now resolves a system
fallback only when one typeface covers the complete approved text run; otherwise
it fails closed before PNG/PDF or visual review. Approved scientific labels use
the interoperable ASCII forms `L1` and `L2` in this slice. This is a rendering
integrity guard, not a scientific conclusion or a substitute for a full font
inventory and cross-machine packaging proof.

Visual review also needs a deterministic readability floor before a provider
sees the image. The article sample exposed an overlap between two critical
relation-label backgrounds: the SVG still carried both labels, but the later
white background hid part of the earlier label in the PNG. The renderer now
allocates each relation label against already occupied label bounds, tries the
opposite side of the relation, and keeps candidate bounds within the canvas.
`ScientificContractReviewer` independently turns any remaining overlap of
critical relation-label bounds into the hard failure
`critical-relation-label-overlap`. This guard checks visual legibility of the
approved relationship; it does not infer or validate any additional scientific
claim.

## 8. Optical Scientific-Rigor Review Package (2026-08-01)

The article shortcut now applies the repo-owned `article-optics-v1`
deterministic review package before provider visual review. This package is
authoritative for the bounded eye/lens corpus only; the visual model remains a
visible-defect detector and cannot redefine scientific truth. It checks the
approved article branches `y=x/(x+1), x>0` and `y=x/(1-x), 0<x<1`, the
definitions `x=u/f` and `y=v/f`, positive-distance and dimensionless-variable
conventions, convex/concave lens roles, `L2/S` plane ordering, left-to-right
ray propagation, convergence/divergence topology, changed intervention focus,
and complete coverage of the six located source-photo references.

Each full-resolution review request is accompanied by responsibility-scoped
formula, lens, ray/relationship, or source-evidence crops. Every crop carries
a typed `ExpectedVisualCheck` containing its scientific meaning, exact
content, relationship direction, conditions, forbidden content, evidence
block IDs, and authority. The formal workflow derives these checks from an
approved `ScientificFigureSpec` and marks them `ApprovedSpecification`. The
article shortcut has no approved spec and therefore marks them
`LocatedSourceEvidencePendingGateOne`; it must keep every candidate
`PendingHumanApproval` rather than manufacturing approval.

Scientific findings fail closed and never enter automatic repair. The bounded
repair loop remains available only for presentation defects such as layout,
text, labels, or non-evidentiary assets. Any change to a formula, optical
element role, plane order, ray direction, or focus topology invalidates Gate 1
and requires human review.

The OpenAI Responses routes now default to the explicitly requested
`gpt-5.6-sol` model with `reasoning.effort=medium`. This is a configuration
change only: fake providers remain the default, and no live call is authorized
by the model selection. Official GPT-5.6 guidance identifies Sol as the
frontier-capability tier, supports `none`, `low`, `medium`, `high`, `xhigh`, and
`max`, and describes `medium` as a balanced starting point. The image-detail
policy uses `original` for GPT-5.4 and newer models when exact small text and
spatial relationships matter, and falls back to `high` for older routes. Full
bytes, typed crops, deterministic checks, and human Gate 1 remain separate
controls even when `original` is available.

Primary OpenAI sources:

- <https://developers.openai.com/api/docs/guides/model-guidance?model=gpt-5.6#migration-quickstart>
- <https://developers.openai.com/api/docs/guides/images-vision#choose-an-image-detail-level>
