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
