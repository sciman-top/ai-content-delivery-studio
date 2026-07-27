# AI Content Delivery Studio

AI Content Delivery Studio turns source-grounded content into reviewable visual
deliverables while preserving the authority and approval boundaries of each
workflow.

## Scientific Figures

**Scientific Source Block**:
A precisely located, immutable fragment recovered from one source asset.
_Avoid_: Chunk, snippet

**Scientific Claim**:
A normalized, reviewable statement relevant to one bounded figure objective.
_Avoid_: Fact, model assertion

**Claim Evidence Link**:
A role-preserving connection from a scientific claim to an exact scientific
source block and quotation.
_Avoid_: Citation, generic evidence

**Supporting Evidence**:
A validated support or definition link that can establish a claim; qualification
and contradiction links remain distinct and never count as support.
_Avoid_: Evidence bundle

**Understanding Coverage**:
The explicit completeness state of required scientific content for one bounded
figure objective.
_Avoid_: Paper completeness, general understanding

**Understanding Approval Readiness**:
A computed, fail-closed state indicating that relevant claims are traceable,
conflict-free, and completely covered; it is not human approval.
_Avoid_: Approval, Gate 1

**Scientific Figure Corpus Acceptance**:
An offline, deterministic replay of every human-approved baseline and declared
blocking mutation through the real scientific workflow contracts. It proves
fixed-corpus behavior only and is not live-provider or human delivery approval.
_Avoid_: General scientific acceptance, live acceptance

**Scientific Figure Spec**:
The versioned scientific-content contract that is the sole authority for a
future render plan.
_Avoid_: Prompt, layout draft

**Scientific Figure Provenance**:
The authority for one scientific element or relation, expressed as validated
claim evidence or an explicit scientific convention.
_Avoid_: Design rationale, model suggestion

**Scientific Convention**:
An explicitly named, non-paper convention used to represent accepted scientific
meaning without being normalized into a paper claim.
_Avoid_: Common knowledge, implicit convention

**Gate 1 Approval**:
A human approval snapshot that freezes one understanding version and one
Scientific Figure Spec version for downstream work.
_Avoid_: Approval readiness, delivery approval

**Scientific Figure Workflow Aggregate**:
The project-owned persistence boundary containing one source extraction,
versioned understanding, Figure Spec, Gate 1 snapshot, downstream approvals,
and invalidation state.
_Avoid_: Prompt history, generated asset

**Scientific Workflow Payload Schema**:
The explicit version contract for a persisted scientific workflow JSON
snapshot; unsupported versions fail closed before domain restoration.
_Avoid_: Database schema version, provider response schema

**Unrecoverable Source Region**:
A located source area where no text can be safely recovered; it remains
explicitly missing and can never serve as claim evidence.
_Avoid_: Empty paragraph, inferred text

**Required Scientific Content**:
A formula or table declared necessary for the bounded figure objective; absence
is represented as a located `Missing` block and blocks extraction readiness.
_Avoid_: Nice-to-have detail, heuristic match

**Scientific Understanding Chunk**:
A bounded, ordered set of immutable source blocks sent to one provider-neutral
understanding call; chunking never changes source block identity.
_Avoid_: Independent paper, prompt window

**Understanding Merge Conflict**:
Two evidence-bound claim drafts with the same merge key but incompatible
normalized statements; it remains explicit and blocks Gate 1 until resolved.
_Avoid_: Duplicate text, provider disagreement score

**Gate 1 Decision**:
The explicit human action that either approves the current ready understanding
and Figure Spec versions or leaves the workflow unapproved.
_Avoid_: Readiness state, automatic approval

**SVG Render Plan**:
The deterministic, pre-render compilation target for one current Gate-1-
approved Figure Spec version, including stable item authority, layers, exact
content, layout constraints, style tokens, accessibility, and export settings.
_Avoid_: SVG output, provider prompt

**Render Item Authority**:
The stable Figure Spec element or relation identifier from which one critical
render element or connection was compiled.
_Avoid_: Visual node ID, evidence quote

**Scientific SVG Authority**:
The deterministic editable SVG string rendered solely from a validated SVG
Render Plan; its hash and embedded plan/spec metadata anchor later exports.
_Avoid_: PNG preview, generated image

**Non-Authoritative Decoration**:
A render-plan-approved decorative geometry group that cannot contain visible
text, relations, images, or scientific provenance.
_Avoid_: Scientific element, generated background with labels

**Approved SVG Hash**:
The SHA-256 identity explicitly authorized for PNG/PDF derivation; the exporter
recomputes the SVG bytes and fails closed unless artifact and approval match.
_Avoid_: Preview hash, semantic hash

**Scientific Export Semantics**:
The canonical accessibility, element, exact-text, relation, direction, and
specification-provenance fixtures shared by every export from one SVG authority.
_Avoid_: OCR result, visual-review score

**Internal Scientific Workflow Pack**:
The registered `scientific-figure` workflow, blueprint, domain, renderer, and
three-layer rubric family; it remains unavailable to WPF users until acceptance.
_Avoid_: Article illustration pack, enabled feature

**Scientific Contract Review**:
A deterministic comparison of the approved Figure Spec, render plan, SVG
authority, and hash-bound exports; it does not call or imitate an AI reviewer.
_Avoid_: Scientific semantic review, visual-quality score

**Scientific Contract Hard Failure**:
A non-overridable invariant violation naming the responsible item, concrete
evidence, and repair layer; any such finding makes contract review fail.
_Avoid_: Low score, optional suggestion

**Scientific Contract Advisory Score**:
A bounded informational value retained for reporting that has no authority to
override a scientific contract hard failure.
_Avoid_: Pass threshold, acceptance decision

**Scientific Semantic Review Request**:
A provider-neutral request containing only accepted claims, the exact evidence
used by the approved Figure Spec, that specification, and a bounded render
summary.
_Avoid_: Full paper payload, planner transcript, generic review prompt

**Scientific Visual Review Request**:
A provider-neutral request containing the original full-resolution output and
typed, item-addressable region crops; it never uses the compact generic-review
artifact as scientific acceptance evidence.
_Avoid_: Thumbnail review, 384px review image

**Scientific Machine Review Decision**:
The fail-closed combination of independent semantic and visual provider
results. Any non-pass verdict, invalid output, finding, or provider failure
creates a blocker; it is not Gate 2 approval.
_Avoid_: Human approval, averaged review score

**Scientific Review Manifest**:
A local, exportable index of specification/render identity, minimum approved
evidence identifiers, critical SVG structure rows, full-resolution output
metadata, and typed crop IDs with private filesystem paths removed.
_Avoid_: Provider prompt log, local workspace dump

**Scientific SVG Structure Row**:
An item-addressable record linking one critical element or relation to its
scientific summary, typed visual role, and bounded pixel region.
_Avoid_: OCR guess, untyped bounding box

**Scientific Review Dispatch Budget**:
The full-resolution byte/pixel, crop-count, and per-crop byte limits checked
before any scientific review provider receives artifacts.
_Avoid_: 384px compact-review policy, provider quota

**Scientific Repair Layer**:
The single owning boundary for a finding: extraction, scientific understanding,
Figure Spec, SVG renderer, layout/style, non-evidentiary asset, or exporter.
_Avoid_: Generic repair, prompt retry

**Automatic Scientific-Figure Repair**:
A bounded repair allowed only for layout/style or non-evidentiary assets, with
at most three completed attempts before human action is mandatory.
_Avoid_: Formula rewrite, claim correction, unlimited retry

**Scientific Revision**:
A change to claims, meaning, exact content, relations, conditions, values, or
units that creates a new Figure Spec version and invalidates Gate 1 plus every
downstream artifact and approval.
_Avoid_: Layout adjustment, visual polish

**Gate 2 Approval**:
The explicit final human decision for one current specification version after
deterministic contract, independent semantic/visual review, and repair closure
all pass.
_Avoid_: Machine readiness, provider pass, delivery export

**Scientific Delivery Package**:
The ZIP delivery containing SVG, PNG, PDF, Figure Spec, claim-evidence-item map,
contract and machine reviews, repair history, provider metadata, artifact
hashes, and both human approvals.
_Avoid_: Image-only export, preview bundle

**Gate 2 Readiness**:
A fail-closed application-layer eligibility result proving current Gate 1,
contract and machine review, repair closure, provider metadata, exact format
cardinality, and artifact hash bindings. It enables a human decision but is not
itself approval.
_Avoid_: Gate 2 approval, visual pass score

**Scientific Delivery Workspace**:
The terminal WPF projection for comparing SVG, PNG, and PDF identities and
previews, inspecting review and evidence provenance, recording the explicit
Gate 2 decision, and exporting the resulting package on user command.
_Avoid_: Package writer, scientific authority, automatic approval

**Scientific Workspace Projection**:
The read-only WPF coordinator view of Source, Understanding, Figure Spec,
Render & Review, and Delivery status derived from the authoritative workflow
state; it never edits prompts or creates scientific truth.
_Avoid_: Workflow aggregate, prompt editor, approval authority

**Scientific Evidence Selection**:
The WPF selection that links one claim row to its exact authoritative source
block, page, section, and verbatim quotation without changing either record.
_Avoid_: Search result, inferred citation, free-text note

**Scientific Claim Correction Draft**:
An audited proposal containing the accepted claim snapshot, proposed wording,
reviewer, reason, and timestamp; it does not mutate or approve the claim.
_Avoid_: In-place claim edit, Gate 1 approval

**Scientific Figure Spec Proposal Diff**:
A typed, target-bound comparison between one current Figure Spec field and a
proposed value. The current value must match the authoritative spec exactly;
acceptance requires a new spec revision and never mutates the frozen version.
_Avoid_: Raw JSON edit, prompt suggestion, Gate 1 approval

**Gate 1 Frozen Authority Versions**:
The exact understanding and Figure Spec versions recorded by an affirmative
human Gate 1 decision. Pending or accepted proposal diffs block approval of the
current version until they are rejected or incorporated into a new revision.
_Avoid_: Current draft version, display-only status, provider approval

**Scientific SVG Authority Selection**:
The WPF selection that maps one deterministic SVG render id through its source
Figure Spec item to the approved claim and exact source evidence or convention.
The preview validates matching understanding, spec, plan, SVG identity, hash,
and deterministic markup before display.
_Avoid_: Pixel hit guess, generated caption, unvalidated browser markup

**Scientific Repair UI Authorization**:
The presentation-layer projection of a domain `ScientificRepairAction`. An
automatic command is enabled only for `LayoutStyle` or `NonEvidentiaryAsset`,
requires an actual execution callback, and restores the three-attempt limit
from continuous audit history.
_Avoid_: UI-inferred repair layer, scientific auto-fix, reset attempt counter
