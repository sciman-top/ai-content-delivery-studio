# Scientific Figure Concept/Comparison Corpus Candidates

Last reviewed: 2026-07-26.

## Status And Admission Boundary

This note proposes four `concept-comparison` evaluation candidates for Task 4
of the trustworthy scientific-figure workflow. It is first-party source
research, not corpus admission and not human approval.

The candidates cover mechanics, thermal physics, optics, and
electromagnetism. Their bounded objectives are concept relationships or
side-by-side comparisons; they do not repeat the Task 3 mechanism targets for
two-car momentum transfer, heat-engine flow, convex-lens image formation, or
a rotating-coil generator.

No source PDF, source image, or other binary is added to this repository. A
candidate may be represented in a `building` manifest only as
`admissionStatus: candidate` with a draft baseline. Before changing it to
`accepted`, a human reviewer must approve its source, claims, anchors,
elements, relations, allowed variations, and both scientific and visual
blocking mutations. This note must not be interpreted as that approval.

## Shared First-Party Source And Cache Evidence

All four candidates use the same reviewed OpenStax textbook artifact:

- Title: *Physics*
- Authors: Paul Peter Urone and Roger Hinrichs
- Publisher: OpenStax
- First-party publication metadata date: 2020-03-26
- Source type: `open-textbook`
- Official book page: <https://openstax.org/details/books/physics>
- Official web contents: <https://openstax.org/books/physics/pages/preface>
- Official PDF:
  <https://assets.openstax.org/oscms-prodcms/media/documents/Physics_-_WEB.pdf>
- License: `CC-BY-4.0`
- First-party license evidence:
  <https://openstax.org/books/physics/pages/preface#fs-id1163975727583>
- Redistribution: `allowed`, subject to attribution and notice requirements
- Reviewed at: `2026-07-26`

The OpenStax preface states that *Physics* is licensed under CC BY 4.0 and may
be distributed, remixed, and built upon with attribution to OpenStax and its
content contributors. The proposed baselines would create new schematic
diagrams from cited scientific statements; they would not copy the textbook
figures or embedded artwork. Reuse of any source image would require a
separate credit and license review.

Recommended attribution for derived corpus material:

> Adapted from *Physics* by Paul Peter Urone and Roger Hinrichs, OpenStax,
> licensed under CC BY 4.0. Changes were made.

The Task 4 research rechecked the existing Task 3 local probe rather than
downloading a second copy:

| Probe | Rechecked result |
| --- | --- |
| PDF byte length | `57,463,331` |
| PDF SHA-256 | `a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e` |
| Extractor | `pdftotext` (Poppler) `24.04.0` with `-layout` |
| Extracted UTF-8 text size | `2,804,564` bytes |
| Extracted text SHA-256 | `331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4` |
| Task 4 located printed pages | 179-180, 346-349, 496-497, 638-642 |

Every candidate should therefore use:

```text
contentHash: sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e
textHash: sha256:331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4
localCacheKey: openstax/physics/2026-07-26/Physics-WEB-a3f75487411e.pdf
```

The reviewed PDF and extracted text remain outside Git at
`C:\Users\sciman\AppData\Local\Temp\cds-task3-mechanism-3eea487a73c642b2ac49d6f255a86163`.
That directory is a disposable probe, not the repository cache authority, and
this note does not claim it was removed. Admission must place or verify the
hash-matched PDF under the Git-ignored
`eval/scientific-figures/.cache/` boundary. A changed download hash must fail
closed pending edition review.

## Evidence-Kind Convention

- `verbatim-text` means the quoted prose is copied from the cited first-party
  HTML/PDF location. Inline symbols may be omitted from a quote only when the
  omission does not change its meaning.
- `normalized-text` means the source statement is restated in a compact,
  diagram-ready form and is not presented as a quotation.
- `normalized-equation` means MathML or print-layout mathematics is transcribed
  into stable ASCII notation. It is not a byte-for-byte quotation.

## Candidate Summary

| Proposed item ID | Domain | Proposed source ID | Bounded comparison |
| --- | --- | --- | --- |
| `mechanics-static-vs-kinetic-friction` | Mechanics | `openstax-physics-section-5-4-friction` | Static response before slipping versus kinetic friction during sliding |
| `thermal-three-mode-heat-transfer-comparison` | Thermal physics | `openstax-physics-section-11-2-transfer-modes` | Conduction, convection, and radiation by carrier and medium requirement |
| `optics-specular-vs-diffuse-reflection` | Optics | `openstax-physics-section-16-1-reflection-types` | Smooth-surface specular reflection versus rough-surface diffuse reflection |
| `electromagnetism-series-vs-parallel-resistors` | Electromagnetism | `openstax-physics-sections-19-2-19-3-resistors` | Series and parallel topology, conserved quantities, and equivalent resistance |

## Relation-Class Coverage

The draft baselines should retain the following evidence-grounded
`relationClass` assignments. The classes describe the source-backed meaning,
not merely the visual connector shape.

| Candidate | `causal` | `directional` | `comparative` | `associative-non-causal` |
| --- | --- | --- | --- | --- |
| Static/kinetic friction | `Fapp > fs(max)` causes transition to sliding (`fs-id1167066836088`) | friction opposes motion or attempted motion (Figure 5.33) | maximum static friction is usually greater than kinetic friction for the contact pair (`fs-id1167066811244`) | `mu_s` and `mu_k` are associated with the same reviewed surface pair and common `N` (`fs-id1167067036387`, `fs-id1167066836088`) |
| Three heat-transfer modes | a temperature difference produces heat transfer (`fs-id1167064903342`); buoyancy drives the cited convection relation (`fs-id1167066032304`) | net transfer is from higher- to lower-temperature region (Figure 11.4) | conduction/contact, convection/moving fluid, and radiation/no required medium are contrasted (`fs-id1167065035104`, `fs-id1167065972910`, `fs-id1167062890228`) | each named mode is associated with its carrier or medium condition; several modes may coexist (`fs-id1167064899898`) |
| Specular/diffuse reflection | varying local surface orientations cause parallel incident rays to leave in many global directions (`fs-id1167067150525`, Figure 16.4) | incident ray -> point of incidence -> reflected ray (Figure 16.3) | smooth/ordered specular and rough/distributed diffuse outcomes are contrasted (`fs-id1167067150525`) | `theta_i`, `theta_r`, and the local normal belong to the same point of incidence (`fs-id1167067294202`, Figure 16.3) |
| Series/parallel resistors | adding parallel paths decreases equivalent resistance relative to the smallest branch resistance (`fs-id1167065722817`) | conventional current follows the connected path and splits/recombines at parallel nodes (`fs-id1167065843479`) | series has one shared current and summed resistance; parallel has shared voltage and reciprocal-sum resistance (sections 19.2-19.3, pp. 638-642) | branch resistors in parallel are associated by sharing the same two nodes (`fs-id1167065746966`, `fs-id1167065978239`) |

Across the four candidates, every required class has multiple independent
examples. A later baseline may use more than one relation of a class, but it
must not relabel a comparison as causal without a source-backed mechanism.

## 1. Mechanics: Static Versus Kinetic Friction

### Source and exact locations

- Proposed `itemId`: `mechanics-static-vs-kinetic-friction`
- Proposed `sourceId`: `openstax-physics-section-5-4-friction`
- Official HTML:
  <https://openstax.org/books/physics/pages/5-4-inclined-planes>
- Exact HTML anchors:
  - [`fs-id1167066811244`](https://openstax.org/books/physics/pages/5-4-inclined-planes#fs-id1167066811244)
    distinguishes the two regimes and compares their magnitudes
  - [`fs-id1167067036387`](https://openstax.org/books/physics/pages/5-4-inclined-planes#fs-id1167067036387)
    introduces the static-friction inequality
  - [`fs-id1167066879604`](https://openstax.org/books/physics/pages/5-4-inclined-planes#fs-id1167066879604)
    explains the maximum static-friction limit
  - [`fs-id1167066836088`](https://openstax.org/books/physics/pages/5-4-inclined-planes#fs-id1167066836088)
    describes the transition to motion and kinetic friction
  - [Figure 5.33](https://openstax.org/books/physics/pages/5-4-inclined-planes#Figure_05_04_surface)
- Official PDF: section 5.4, printed pp. 179-180; Figure 5.33 and the
  static/kinetic friction equations on p. 180

### Bounded figure objective

Create a two-regime concept comparison for the same crate, contact pair, and
normal force. The static panel shows no relative motion and a friction arrow
that opposes and matches the applied force only up to `fs(max)`. The kinetic
panel shows sliding and a friction arrow opposite the relative motion with
`fk = mu_k N`. A threshold connector states that motion begins when the
applied force exceeds `fs(max)`.

### Claims and evidence anchors

1. `claim-friction-regimes`:
   - `verbatim-text`, anchor `fs-id1167066811244`, PDF p. 179:
     “Kinetic friction acts on an object in relative motion, while static
     friction acts on an object or system at rest relative to each other.”
2. `claim-static-usually-larger`:
   - `verbatim-text`, the same anchor and page:
     “The maximum static friction is usually greater than the kinetic friction
     between the objects.”
3. `claim-static-responsive`:
   - `verbatim-text`, anchor `fs-id1167066836088`, PDF p. 180:
     “Static friction is a responsive force that increases to be equal and
     opposite to whatever force is exerted, up to its maximum limit.”
4. `claim-transition`:
   - `normalized-text`, the same anchor and page: once the applied force
     exceeds `fs(max)`, the object moves.
5. `claim-friction-direction`:
   - `verbatim-text`, Figure 5.33 caption, PDF p. 180:
     “Frictional forces, such as f, always oppose motion or attempted motion
     between objects in contact.”

### Elements, relations, conditions, and comparison

- Elements: static-regime crate, kinetic-regime crate, common contact surface,
  normal force `N`, applied force `Fapp`, static friction `fs`, maximum static
  friction `fs(max)`, kinetic friction `fk`, and sliding-direction arrow.
- Static relations:
  - `normalized-equation`, anchors `fs-id1167067036387` and
    `fs-id1167066879604`, PDF p. 180: `fs <= mu_s N`
  - `normalized-equation`, anchor `fs-id1167066879604`, PDF p. 180:
    `fs(max) = mu_s N`
  - `normalized-text`, anchor `fs-id1167066836088`: before the threshold,
    `fs` responds opposite `Fapp` and matches it in magnitude.
- Kinetic relation:
  - `normalized-equation`, anchor `fs-id1167066836088`, PDF p. 180:
    `fk = mu_k N`
- Comparison: static friction applies without relative sliding and varies up
  to a maximum; kinetic friction applies during relative sliding. For the
  compared contact pair, the source says the maximum static value is usually
  greater than the kinetic value.
- Conditions: the same two surfaces and fixed `N` are used across panels;
  friction is parallel to the contact surface; `mu_s` and `mu_k` are
  dimensionless coefficients. Do not generalize “usually greater” into an
  exceptionless universal law.
- Values and units: no numeric value is required. `fs`, `fk`, `Fapp`, and `N`
  are forces in newtons (`N`); `mu_s` and `mu_k` are dimensionless.

### Allowed variations

- Layout: the regimes may be side-by-side or stacked if the threshold order
  and shared surface pair remain explicit.
- Style: crate appearance, color, and line style may vary; force labels and
  arrow directions may not.
- Non-evidentiary asset: a neutral texture may distinguish the contact
  surface if it does not imply a different material between panels.

### Blocking mutations

- Scientific: state `fs = mu_s N` for every static state instead of the
  inequality, or show static friction exceeding `fs(max)`. Expected:
  `block`.
- Scientific: assign kinetic friction to the no-slip panel or static friction
  to the sliding panel. Expected: `block`.
- Scientific: replace “usually greater” with a universal
  `fs(max) > fk` claim without preserving the same contact-pair conditions.
  Expected: `block`.
- Visual: point either friction arrow in the direction of motion or attempted
  motion. Expected: `block`.
- Visual: remove the threshold connector so the two panels imply simultaneous
  states for the same crate. Expected: `block`.

## 2. Thermal Physics: Three Heat-Transfer Modes

### Source and exact locations

- Proposed `itemId`: `thermal-three-mode-heat-transfer-comparison`
- Proposed `sourceId`: `openstax-physics-section-11-2-transfer-modes`
- Official HTML:
  <https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer>
- Exact HTML anchors:
  - [`fs-id1167064899898`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167064899898)
    enumerates all three methods
  - [`fs-id1167064903342`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167064903342)
    states the temperature-difference condition
  - [`fs-id1167065035104`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167065035104)
    defines conduction
  - [`fs-id1167065972910`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167065972910)
    defines convection
  - [`fs-id1167066032304`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167066032304)
    relates density, buoyancy, and convection
  - [`fs-id1167066091544`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167066091544)
    defines radiation
  - [`fs-id1167062890228`](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#fs-id1167062890228)
    states radiation's no-medium distinction
  - [Figures 11.3-11.7](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer#Figure_11_02_Fireplace)
- Official PDF: section 11.2, printed pp. 346-349; Figure 11.3 on
  pp. 346-347, conduction and Figure 11.4 on p. 347, convection and Figures
  11.5-11.6 on pp. 347-348, radiation and Figure 11.7 on pp. 348-349

### Bounded figure objective

Create a three-column comparison with a common “net energy transfer from
higher-temperature region to lower-temperature region” header. Show
conduction as transfer through direct physical contact, convection as transfer
by moving liquid or gas, and radiation as emitted/absorbed electromagnetic
radiation that can cross empty space. Include a note that multiple methods may
occur simultaneously.

### Claims and evidence anchors

1. `claim-three-methods`:
   - `verbatim-text`, anchor `fs-id1167064899898`, PDF p. 346:
     “There are three different heat transfer methods: conduction,
     convection, and radiation.”
2. `claim-simultaneous`:
   - `verbatim-text`, the same anchor and page:
     “At times, all three may happen simultaneously.”
3. `claim-conduction-definition`:
   - `verbatim-text`, anchor `fs-id1167065035104`, PDF p. 347:
     “Conduction is heat transfer through direct physical contact.”
4. `claim-convection-definition`:
   - `verbatim-text`, anchor `fs-id1167065972910`, PDF p. 347:
     “Convection is heat transfer by the movement of a fluid.”
5. `claim-radiation-definition`:
   - `verbatim-text`, anchor `fs-id1167066091544`, PDF p. 348:
     “Radiation is a form of heat transfer that occurs when electromagnetic
     radiation is emitted or absorbed.”
6. `claim-radiation-no-medium`:
   - `verbatim-text`, anchor `fs-id1167062890228`, PDF p. 348:
     “Radiation is the only method of heat transfer where no medium is
     required.”

### Elements, relations, conditions, and comparison

- Shared elements: higher-temperature region, lower-temperature region, and a
  net heat-transfer direction from higher to lower temperature.
- Conduction elements and relation: contacting material regions and
  particle-collision/contact interface; energy crosses through direct
  physical contact.
- Convection elements and relation: a liquid or gas, warmer less-dense rising
  flow, cooler denser sinking return flow; moving fluid transports energy.
- Radiation elements and relation: emitter, electromagnetic-wave arrows, and
  absorber; emission/absorption transfers energy without requiring matter
  between them.
- Comparison:
  - conduction requires direct physical contact
  - convection requires moving fluid, where “fluid” means liquid or gas
  - radiation requires neither contact nor a material medium
  - a real situation may contain more than one mode
- Conditions: represent heat transfer only when a temperature difference is
  present; do not equate “heat” with a stored material substance; do not claim
  that radiation alone always dominates.
- Formulas, values, and units: no formula or numeric value is scientifically
  required for this qualitative comparison. If a temperature difference is
  labeled, use kelvin (`K`) or degrees Celsius (`deg C`) consistently. If heat
  is quantified in a later baseline, use joules (`J`) and cite a new bounded
  source anchor.

### Allowed variations

- Layout: columns, rows, or a triangular comparison are acceptable if each
  transfer mode retains its carrier/medium distinction.
- Style: icons for solids, fluids, and electromagnetic waves may vary; arrows
  and labels must remain unambiguous.
- Non-evidentiary asset: stove, room-air, or Sun/Earth context may be used as
  a mnemonic but may not replace the scientific labels.

### Blocking mutations

- Scientific: state that radiation requires air or another material medium.
  Expected: `block`.
- Scientific: label bulk fluid circulation as conduction or direct-contact
  transfer in a stationary solid as convection. Expected: `block`.
- Scientific: claim the three modes are mutually exclusive. Expected:
  `block`.
- Visual: use identical unlabeled arrows or icons so the carrier distinction
  cannot be read. Expected: `block`.
- Visual: reverse the shared net-transfer arrow from lower to higher
  temperature without adding work or another scientifically grounded
  condition. Expected: `block`.

## 3. Optics: Specular Versus Diffuse Reflection

### Source and exact locations

- Proposed `itemId`: `optics-specular-vs-diffuse-reflection`
- Proposed `sourceId`: `openstax-physics-section-16-1-reflection-types`
- Official HTML:
  <https://openstax.org/books/physics/pages/16-1-reflection>
- Exact HTML anchors:
  - [`fs-id1167067294202`](https://openstax.org/books/physics/pages/16-1-reflection#fs-id1167067294202)
    states the law of reflection and defines the normal
  - [`fs-id1167067150525`](https://openstax.org/books/physics/pages/16-1-reflection#fs-id1167067150525)
    contrasts smooth and rough surfaces and explains diffused directions
  - [Figure 16.3](https://openstax.org/books/physics/pages/16-1-reflection#Figure_16_01_Reflect)
  - [Figure 16.4](https://openstax.org/books/physics/pages/16-1-reflection#Figure_16_01_Reflect2)
- Official PDF: section 16.1, printed pp. 496-497; law-of-reflection prose
  begins on p. 496, with Figures 16.3 and 16.4 and the smooth/rough comparison
  on p. 497

### Bounded figure objective

Create two panels receiving the same set of parallel incident rays. The smooth
surface panel shows aligned local normals and reflected rays remaining
ordered (specular reflection). The rough surface panel shows different local
surface orientations/normals and reflected rays leaving at different global
angles (diffuse reflection). Include one enlarged local ray pair in each panel
to show that the law `theta_r = theta_i` still applies relative to the local
normal.

### Claims and evidence anchors

1. `claim-reflection-law`:
   - `normalized-text`, anchor `fs-id1167067294202`, PDF pp. 496-497: the
     angle of reflection `theta_r` equals the angle of incidence `theta_i`.
2. `claim-angle-reference`:
   - `normalized-text`, the same anchor and Figure 16.3, PDF p. 497: incidence
     and reflection angles are measured relative to the normal at the point
     where the ray strikes the surface.
3. `claim-smooth-specular`:
   - `normalized-text`, anchor `fs-id1167067294202`, PDF p. 497: reflection
     from the smooth surface with the law-of-reflection geometry is called
     specular reflection.
4. `claim-rough-diffuse`:
   - `verbatim-text`, anchor `fs-id1167067150525`, PDF p. 497:
     “Because the light is reflected from different parts of the surface at
     different angles, the rays go in many different directions, so the
     reflected light is diffused.”
5. `claim-figure-diffuse`:
   - `verbatim-text`, Figure 16.4 caption, PDF p. 497:
     “Here, many parallel rays are incident, but they are reflected at many
     different angles because the surface is rough.”

### Elements, relations, conditions, and comparison

- Elements: parallel incident rays, smooth surface, rough surface, points of
  incidence, local normals, incidence angle `theta_i`, reflection angle
  `theta_r`, ordered reflected rays, and distributed reflected rays.
- Shared relation:
  - `normalized-equation`, anchor `fs-id1167067294202` and Figure 16.3,
    PDF pp. 496-497: `theta_r = theta_i`
- Specular comparison: common surface orientation gives aligned normals and a
  correspondingly ordered reflected direction.
- Diffuse comparison: varying local surface orientation gives varying local
  normals and many global reflected directions. Diffuse reflection does not
  mean the local law of reflection is suspended.
- Conditions: use geometric-optics rays; measure both angles from the local
  normal, not from the surface; keep incident medium unchanged across the
  comparison.
- Values and units: no numeric angle is required. If an example angle is
  added, `theta_i` and `theta_r` must have the same value in degrees (`deg`) or
  radians (`rad`), with one unit used consistently.

### Allowed variations

- Layout: panels may be horizontal or vertical if the same incident-ray set is
  visibly reused.
- Style: surface color, ray color, and roughness texture may vary; normals
  must remain distinguishable from physical rays.
- Non-evidentiary asset: a mirror/page icon may label the everyday case but
  may not replace the local-normal construction.

### Blocking mutations

- Scientific: measure one angle from the surface and the other from the
  normal, or show `theta_r != theta_i` for a local reflection. Expected:
  `block`.
- Scientific: claim that rough surfaces violate the law of reflection.
  Expected: `block`.
- Scientific: label the rough/distributed panel “specular” and the
  smooth/ordered panel “diffuse.” Expected: `block`.
- Visual: make normals look like additional rays or omit them while displaying
  angle arcs. Expected: `block`.
- Visual: draw the diffuse rays at different angles but from a single flat
  local orientation, obscuring the rough-surface basis of the comparison.
  Expected: `block`.

## 4. Electromagnetism: Series Versus Parallel Resistors

### Source and exact locations

- Proposed `itemId`: `electromagnetism-series-vs-parallel-resistors`
- Proposed `sourceId`: `openstax-physics-sections-19-2-19-3-resistors`
- Official HTML:
  - <https://openstax.org/books/physics/pages/19-2-series-circuits>
  - <https://openstax.org/books/physics/pages/19-3-parallel-circuits>
- Exact series anchors:
  - [`fs-id1167064901800`](https://openstax.org/books/physics/pages/19-2-series-circuits#fs-id1167064901800)
    defines series topology
  - [Figure 19.14](https://openstax.org/books/physics/pages/19-2-series-circuits#Figure_19_02_Circuit03)
    shows three series resistors and their equivalent resistor
  - [`fs-id1167065821410`](https://openstax.org/books/physics/pages/19-2-series-circuits#fs-id1167065821410)
    states the same-current equivalence
  - [`fs-id1167066036066`](https://openstax.org/books/physics/pages/19-2-series-circuits#fs-id1167066036066)
    gives the loop voltage-drop relation
  - [`fs-id1167066197380`](https://openstax.org/books/physics/pages/19-2-series-circuits#fs-id1167066197380)
    gives the series equivalent-resistance rule
- Exact parallel anchors:
  - [`fs-id1167065746966`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167065746966)
    defines parallel topology
  - [`fs-id1167065978239`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167065978239)
    states equal branch voltage
  - [`fs-id1167062315741`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167062315741)
    states branch currents need not be equal
  - [`fs-id1167065722817`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167065722817)
    compares parallel equivalent resistance to the smallest branch resistance
  - [Figure 19.16](https://openstax.org/books/physics/pages/19-3-parallel-circuits#Figure_19_03_Circuit04)
  - [`fs-id1167065843479`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167065843479)
    gives current conservation at the branches
  - [`fs-id1167065761913`](https://openstax.org/books/physics/pages/19-3-parallel-circuits#fs-id1167065761913)
    gives the general parallel equivalent-resistance relation
- Official PDF: section 19.2, Figure 19.14 and equations 19.9-19.13,
  printed pp. 638-639; section 19.3, Figure 19.16 and equations 19.21-19.28,
  printed pp. 641-642

### Bounded figure objective

Create matched side-by-side circuit concept diagrams using the same ideal
battery and three positive resistors `R1`, `R2`, and `R3`. The series panel
shows one branch, common current, divided voltage drops, and summed equivalent
resistance. The parallel panel shows three branches, common branch voltage,
split currents that recombine, and reciprocal-sum equivalent resistance.

### Claims and evidence anchors

1. `claim-series-topology`:
   - `verbatim-text`, anchor `fs-id1167064901800`, PDF pp. 637-638:
     “Components connected in series are connected one after the other in the
     same branch of a circuit.”
2. `claim-series-equivalence`:
   - `verbatim-text`, anchor `fs-id1167065821410`, PDF p. 638:
     “The same current will flow through the left and right circuits in
     Figure 19.14 if we use the equivalent resistor in the right circuit.”
3. `claim-series-resistance`:
   - `verbatim-text`, anchor `fs-id1167066197380`, PDF p. 639:
     “The equivalent resistance for a series of resistors is simply the sum
     of the resistances of each resistor.”
4. `claim-parallel-topology`:
   - `verbatim-text`, anchor `fs-id1167065746966`, PDF p. 641:
     “Resistors are in parallel when both ends of each resistor are connected
     directly together.”
5. `claim-parallel-voltage`:
   - `verbatim-text`, anchor `fs-id1167065978239`, PDF p. 641:
     “This means that the voltage drop across each resistor is the same.”
6. `claim-parallel-current`:
   - `normalized-text`, anchors `fs-id1167062315741` and
     `fs-id1167065843479`, PDF pp. 641-642: branch currents need not be equal,
     but their sum equals the current through the battery.
7. `claim-parallel-resistance`:
   - `verbatim-text`, anchor `fs-id1167065722817`, PDF p. 641:
     “The equivalent resistance must be less than the smallest resistance of
     the parallel resistors.”

### Elements, relations, conditions, and comparison

- Shared elements: ideal battery with voltage `V`, ideal wires, positive
  resistors `R1`, `R2`, `R3`, conventional-current arrows, and an equivalent
  resistor `Req`.
- Series topology and relations:
  - one branch with the same current `I` through all three resistors
  - `normalized-equation`, anchor `fs-id1167066036066`, equation 19.9,
    PDF p. 638: `V = V1 + V2 + V3`
  - `normalized-equation`, anchor `fs-id1167066197380`, equation 19.13,
    PDF p. 639: `Req(series) = R1 + R2 + R3`
- Parallel topology and relations:
  - both ends of every resistor share the same two nodes, so
    `V1 = V2 = V3 = V`
  - `normalized-equation`, anchor `fs-id1167065843479`, equation 19.23,
    PDF p. 642: `I = I1 + I2 + I3`
  - `normalized-equation`, anchor `fs-id1167065761913`, equation 19.28,
    PDF p. 642:
    `1/Req(parallel) = 1/R1 + 1/R2 + 1/R3`
- Comparison: series adds resistance and preserves one path/current; parallel
  adds current paths, preserves branch voltage, and yields
  `Req(parallel) < min(R1, R2, R3)` for finite positive resistances.
- Conditions: steady-state direct-current comparison, ideal wires, ohmic
  positive resistors, and the same battery voltage across the matched
  examples. Conventional current direction is used.
- Values and units: no numeric values are required. `V`, `V1`, `V2`, and `V3`
  are in volts (`V`); `I`, `I1`, `I2`, and `I3` are in amperes (`A`);
  `R1`, `R2`, `R3`, and `Req` are in ohms (`ohm`).

### Allowed variations

- Layout: circuits may be horizontal or vertical; electrical connectivity and
  distinct parallel nodes must remain unambiguous.
- Style: IEC or ANSI resistor glyphs may be used consistently; changing glyph
  style must not change connectivity.
- Non-evidentiary asset: a battery enclosure or neutral background may vary;
  circuit lines, junctions, arrows, and labels remain evidentiary.

### Blocking mutations

- Scientific: show different currents through series elements without a
  branch, accumulation, or transient condition. Expected: `block`.
- Scientific: show different voltage drops across ideal parallel branches
  connected to the same two nodes. Expected: `block`.
- Scientific: use `Req(parallel) = R1 + R2 + R3`, or claim the parallel
  equivalent exceeds the largest branch resistance. Expected: `block`.
- Visual: omit or misplace a junction dot so the intended parallel branches
  are electrically series-connected or disconnected. Expected: `block`.
- Visual: place current arrows that split but do not recombine while retaining
  the steady-state conservation claim. Expected: `block`.

## Recommendation And Remaining Human Gate

The four candidates use stable first-party access, the same rechecked
hash-matched PDF/text artifact as Task 3, CC BY 4.0 source terms, precise HTML
anchors, and bounded comparison objectives. They are suitable for
schema-compatible candidate records and draft gold baselines.

Candidate progression remains:

```text
research candidate
  -> hash-matched PDF verified under the ignored local-cache boundary
  -> candidate corpus record plus draft baseline
  -> contract and anchor validation
  -> human scientific and visual acceptance review
  -> accepted corpus item
```

The recommendation is machine-preparation only. Human approval remains open,
and the corpus must remain `building` until all 12 required items are
individually accepted.
