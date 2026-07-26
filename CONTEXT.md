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
