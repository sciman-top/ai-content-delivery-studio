# Scientific Figure Mechanism/Process Corpus Candidates

Last reviewed: 2026-07-26.

## Status And Admission Boundary

This note proposes four `mechanism-process` evaluation candidates for Tasks
3-5 of the trustworthy scientific-figure workflow. It is source research, not
corpus admission or human approval.

The four candidates cover mechanics, thermal physics, optics, and
electromagnetism. They use one first-party OpenStax open textbook so that the
license, publisher, downloadable artifact, and attribution chain are
consistent. No source PDF, source image, or other binary is added to this
repository.

Before a record with `admissionStatus: candidate` can enter the `building`
manifest in `eval/scientific-figures/corpus.json`:

- the corpus schema must admit an honest `open-textbook` source type; the
  research-start schema exposed only
  `paper | scholarly-article | repo-fixture`, so Task 3 must retain the
  additive enum change and its contract test rather than mislabel these
  sources as papers
- the source PDF must be downloaded into the Git-ignored local cache and its
  hash must match the candidate `contentHash`
- the source record and a schema-valid draft gold baseline must be added
  together so the repository contract remains closed

A candidate may remain explicitly unapproved while the manifest is
`building`. Before changing its `admissionStatus` to `accepted`, a human
reviewer must approve the source, claims, anchors, scientific/visual
relations, allowed variation, and blocking mutations. The corpus itself must
not change to `human-approved` until all 12 required items are accepted.

## Shared Source, License, And Download Evidence

### Source identity

- Title: *Physics*
- Authors: Paul Peter Urone and Roger Hinrichs
- Publisher: OpenStax
- First-party publication metadata date: 2020-03-26
- Source type: `open-textbook` (requires the Task 3 additive corpus-schema
  contract)
- Official book page:
  <https://openstax.org/details/books/physics>
- Official web contents:
  <https://openstax.org/books/physics/pages/preface>
- Official downloadable PDF:
  <https://assets.openstax.org/oscms-prodcms/media/documents/Physics_-_WEB.pdf>

### License and redistribution verdict

- SPDX identifier: `CC-BY-4.0`
- First-party license evidence:
  <https://openstax.org/books/physics/pages/preface#fs-id1163975727583>
- License text: <https://creativecommons.org/licenses/by/4.0/legalcode>
- Reviewed at: `2026-07-26`
- Corpus redistribution value: `allowed`

The first-party preface states: “Physics is licensed under a Creative Commons
Attribution 4.0 International (CC BY) license, which means that you can
distribute, remix, and build upon the content, as long as you provide
attribution to OpenStax and its content contributors.”

The verdict is subject to CC BY 4.0 attribution and notice requirements. The
proposed baselines create new schematic diagrams from cited scientific
statements; they do not copy the textbook figures or their embedded artwork.
If a future implementation reuses an image rather than facts and text, it must
separately inspect that image's credit and license because the preface warns
that some art has source-specific attribution.

Recommended attribution for derived corpus material:

> Adapted from *Physics* by Paul Peter Urone and Roger Hinrichs, OpenStax,
> licensed under CC BY 4.0. Changes were made.

### Local probe

The official PDF was downloaded to an isolated temporary directory on
2026-07-26.

| Probe | Result |
| --- | --- |
| HTTP download | succeeded |
| Byte length | `57,463,331` |
| SHA-256 | `a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e` |
| Extractor | `pdftotext` (Poppler) `24.04.0` |
| Extraction command | `pdftotext -layout Physics-WEB.pdf Physics-WEB.txt` |
| Extraction result | succeeded |
| Extracted UTF-8 text size | `2,804,564` bytes |
| Extracted text SHA-256 | `331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4` |
| Located printed pages | 265-267, 386-391, 517-519 and 527-528, 684-688 |

The probe directory still exists at
`C:\Users\sciman\AppData\Local\Temp\cds-task3-mechanism-3eea487a73c642b2ac49d6f255a86163`.
It contains the downloaded PDF, extracted text, and disposable HTML snapshots.
It is outside the repository and nothing from it is tracked by Git. This note
does not claim that the temporary files were removed.

All four candidate records should use:

```text
contentHash: sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e
localCacheKey: openstax/physics/2026-07-26/Physics-WEB-a3f75487411e.pdf
```

The dated cache key records the reviewed byte snapshot. The public HTML pages
are precise, readable anchors, while the downloaded PDF bytes and hash are the
offline evidence authority. Admission must fail closed if a later download has
a different hash until the changed edition is reviewed.

## Candidate Summary

| Proposed item ID | Domain | Bounded figure target | Main evidence |
| --- | --- | --- | --- |
| `mechanics-two-car-momentum-transfer` | Mechanics | Before/interaction/after momentum transfer between two co-directional cars | Section 8.2, Figure 8.4, pp. 265-267 |
| `thermal-heat-engine-energy-flow` | Thermal physics | Hot reservoir to engine, work output, and rejected heat to cold reservoir | Section 12.4, Figure 12.14, pp. 386-391 |
| `optics-convex-lens-real-image` | Optics | Three principal rays and a numerically constrained real inverted image | Section 16.3, Figures 16.27 and 16.36, pp. 517-519 and 527-528 |
| `electromagnetism-rotating-coil-generator` | Electromagnetism | Rotating coil, changing flux, induced current, and sinusoidal emf | Section 20.2, Figures 20.26-20.28, pp. 684-688 |

## 1. Mechanics: Two-Car Momentum Transfer

### Source record

- Proposed `sourceId`: `openstax-physics-section-8-2`
- Source type: `open-textbook`
- Public URL:
  <https://openstax.org/books/physics/pages/8-2-conservation-of-momentum>
- License: the shared `CC-BY-4.0` record above
- Evidence locations:
  - HTML paragraph
    [`fs-id1167067043785`](https://openstax.org/books/physics/pages/8-2-conservation-of-momentum#fs-id1167067043785)
  - HTML paragraphs
    [`fs-id1167066967155`](https://openstax.org/books/physics/pages/8-2-conservation-of-momentum#fs-id1167066967155)
    and
    [`fs-id1167066854057`](https://openstax.org/books/physics/pages/8-2-conservation-of-momentum#fs-id1167066854057)
  - HTML
    [Figure 8.4](https://openstax.org/books/physics/pages/8-2-conservation-of-momentum#Figure_08_02_collision)
  - Official PDF, printed pp. 265-267

### Figure objective

Show a left-to-right, three-state mechanism diagram for a rear car `m1`
bumping a lead car `m2`: before contact, equal-and-opposite collision impulses
during contact, and after contact with `m1` slower and `m2` faster. Enclose
both cars in the system boundary and state that total momentum is unchanged
only under negligible external force/friction.

### Required scientific content

- Elements: car `m1`, car `m2`, two-car system boundary, before/contact/after
  states, velocity arrows `v1`, `v2`, `v1'`, `v2'`, impulses
  `Delta p1` and `Delta p2`.
- Directions: both initial car velocities point in the same direction; the
  contact impulses are equal and opposite; after contact `m1` slows while
  `m2` speeds up.
- Conditions: collision duration `Delta t` is shared; friction is negligible;
  the net external force on the two-car system is zero.
- Relations:
  `Delta p1 = F1 Delta t`,
  `F2 = -F1`,
  `Delta p2 = -Delta p1`,
  `Delta p1 + Delta p2 = 0`, and
  `p1 + p2 = p1' + p2'`.
- Values and units: no numeric value is scientifically required. The baseline
  must use symbolic momentum and velocity rather than inventing masses,
  speeds, seconds, or SI values absent from this bounded example.

### Exact anchors

1. Section 8.2, paragraph `fs-id1167067043785`, PDF pp. 265-266:
   “Car m1 slows down as a result of the collision, losing some momentum,
   while car m2 speeds up and gains some momentum.”
2. Paragraph `fs-id1167066967155`, PDF p. 266, contains the exact prose
   “We know from Newton’s third law of motion” and the MathML relations
   normalized here as `F2 = -F1` and `Delta p2 = -Delta p1`.
3. Figure 8.4 caption, PDF p. 266:
   “The momentum of each car is changed, but the total momentum ptot of the
   two cars is the same before and after the collision if you assume friction
   is negligible.”
4. Equation anchors `eip-167`, `fs-id1167066874701`, and
   `fs-id1167067066930` supply the impulse and before/after equations.

### Blocking mutations

- Scientific: reverse the conclusion so `m1` speeds up and `m2` slows down,
  or draw both collision impulses in the same direction. Expected outcome:
  `block`.
- Scientific: remove the negligible-external-force condition while retaining
  the conservation claim. Expected outcome: `block`.
- Visual: swap the `v1'` and `v2'` labels or make arrowheads ambiguous so the
  before/after direction cannot be read. Expected outcome: `block`.

## 2. Thermal Physics: Heat-Engine Energy Flow

### Source record

- Proposed `sourceId`: `openstax-physics-section-12-4`
- Source type: `open-textbook`
- Public URL:
  <https://openstax.org/books/physics/pages/12-4-applications-of-thermodynamics-heat-engines-heat-pumps-and-refrigerators>
- License: the shared `CC-BY-4.0` record above
- Evidence locations:
  - HTML paragraph
    [`fs-id1167063517821`](https://openstax.org/books/physics/pages/12-4-applications-of-thermodynamics-heat-engines-heat-pumps-and-refrigerators#fs-id1167063517821)
  - HTML paragraph
    [`fs-id1167063710956`](https://openstax.org/books/physics/pages/12-4-applications-of-thermodynamics-heat-engines-heat-pumps-and-refrigerators#fs-id1167063710956)
  - HTML
    [Figure 12.14](https://openstax.org/books/physics/pages/12-4-applications-of-thermodynamics-heat-engines-heat-pumps-and-refrigerators#Figure_12_04_Reservoirs)
  - Equations 12.21 and 12.26
  - Official PDF, printed pp. 386-391

### Figure objective

Render an energy-flow schematic for a cyclic heat engine: heat `Qh` leaves a
hot reservoir at `Th` and enters the engine; the engine sends useful work `W`
outward and rejects unused heat `Qc` to a cold reservoir at `Tc`. Pair the
arrows with the cycle balance and efficiency, without implying perfect
heat-to-work conversion.

### Required scientific content

- Elements: hot reservoir `Th`, heat engine/working system, cold reservoir
  `Tc`, heat transfers `Qh` and `Qc`, useful work output `W`.
- Directions: `Qh` points hot reservoir -> engine; `W` points engine ->
  surroundings/load; `Qc` points engine -> cold reservoir.
- Conditions: one complete cycle; `Delta U = 0`; `Th > Tc`; the second law
  rules out `Qc = 0` for a real heat engine.
- Relations: `Q = Qh - Qc`, `W = Qh - Qc`, and
  `Eff = W / Qh`.
- Values and units: no numeric energy is required. All three energy quantities
  must share one declared unit if a later baseline introduces numbers.

### Exact anchors

1. Section 12.4, paragraph `fs-id1167063517821`, PDF p. 387:
   “Heat engines do work by using part of the energy transferred by heat from
   some source.”
2. The same paragraph, PDF p. 387 (inline MathML symbols normalized):
   “heat transfers energy, Qh, from the high-temperature object (or hot
   reservoir), whereas heat transfers unused energy, Qc, into the
   low-temperature object (or cold reservoir), and the work done by the
   engine is W.”
3. Figure 12.14 caption, PDF p. 388 (inline MathML symbols normalized):
   “Qh is the heat out of the hot reservoir, W is the work output, and Qc is
   the unused heat into the cold reservoir.”
4. Paragraph `fs-id1167063710956` and equation
   `fs-id1164563536045` (12.21), PDF p. 388, establish the cyclic
   `Delta U = 0` condition and `W = Qh - Qc`.
5. Equation `fs-id1164562049082` (12.26), PDF p. 391, defines
   `Eff = W / Qh`.

### Blocking mutations

- Scientific: delete `Qc` and show `Qh = W`, implying perfect conversion of
  heat to work. Expected outcome: `block`.
- Scientific: reverse either heat arrow while still labeling the device as a
  heat engine rather than a refrigerator/heat pump. Expected outcome:
  `block`.
- Visual: point the `W` arrow into the engine or attach `Qh` and `Qc` to the
  wrong reservoirs. Expected outcome: `block`.

## 3. Optics: Convex-Lens Real Image

### Source record

- Proposed `sourceId`: `openstax-physics-section-16-3`
- Source type: `open-textbook`
- Public URL:
  <https://openstax.org/books/physics/pages/16-3-lenses>
- License: the shared `CC-BY-4.0` record above
- Evidence locations:
  - HTML paragraph
    [`fs-id1167065892046`](https://openstax.org/books/physics/pages/16-3-lenses#fs-id1167065892046)
  - HTML
    [Figure 16.27](https://openstax.org/books/physics/pages/16-3-lenses#Figure_16_03_ConvexL2)
  - HTML paragraph
    [`fs-id1167066138560`](https://openstax.org/books/physics/pages/16-3-lenses#fs-id1167066138560)
  - HTML
    [Figure 16.36](https://openstax.org/books/physics/pages/16-3-lenses#Figure_16_03_Example)
  - Equations 16.18 and 16.19
  - Official PDF, printed pp. 517-519 and 527-528

### Figure objective

Draw a to-scale principal-ray diagram for a convex thin lens with focal length
`f = 0.50 m` and an upright object at `do = 0.75 m` on the left. Show the
three standard rays from the object's top and their crossing at a real,
inverted image `di = 1.5 m` on the right with magnification `m = -2.0`.

### Required scientific content

- Elements: optical axis, convex thin lens, focal points `F` on both sides,
  object arrow, inverted image arrow, three incident/refracted ray pairs.
- Ray relations:
  - a ray parallel to the axis exits through the far focal point
  - a ray through the lens center is undeviated
  - a ray through the near focal point exits parallel to the axis
- Directions: every physical ray arrow points object -> lens -> image; the
  three refracted rays meet at the top of the inverted real image.
- Conditions: thin converging lens; `do > f`; distances are measured from the
  lens; the diagram uses the stated sign convention.
- Formulas:
  `1/f = 1/di + 1/do`,
  `di = f do / (do - f)`, and
  `m = hi/ho = -di/do`.
- Values and units:
  `f = +0.50 m`,
  `do = +0.75 m`,
  `di = +1.5 m`,
  `m = -2.0` (dimensionless).

### Exact anchors

1. Section 16.3 ray rule, PDF p. 518:
   “A ray entering a converging lens parallel to its axis passes through the
   focal point, F, of the lens on the other side.”
2. Section 16.3 ray rule, PDF p. 518:
   “A ray passing through the center of either a converging or a diverging
   lens does not change direction.”
3. Figure 16.27 caption, PDF p. 519:
   “The image is located at the point where the rays cross. In this case, a
   real image—one that can be projected on a screen—is formed.”
4. Paragraph `fs-id1167066138560` and Figure 16.36, PDF p. 527:
   “A clear glass light bulb is placed 0.75 m from a convex lens with a 0.50 m
   focal length.”
5. Equations `fs-id1164566540855` (16.18) and
   `fs-id1164565253490` (16.19), PDF p. 528, give
   `di = 1.5 m` and `m = -2.0`.

### Blocking mutations

- Scientific: route the parallel incident ray so it does not pass through the
  far focal point, or bend the center ray. Expected outcome: `block`.
- Scientific: label the result upright or virtual despite the stated
  `do = 0.75 m` and `f = 0.50 m`. Expected outcome: `block`.
- Visual: omit ray arrowheads, swap the near/far focal labels, or place the
  image at `0.15 m` while retaining the `1.5 m` label. Expected outcome:
  `block`.

## 4. Electromagnetism: Rotating-Coil Generator

### Source record

- Proposed `sourceId`: `openstax-physics-section-20-2`
- Source type: `open-textbook`
- Public URL:
  <https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers>
- License: the shared `CC-BY-4.0` record above
- Evidence locations:
  - HTML paragraph
    [`fs-id1167063183921`](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers#fs-id1167063183921)
  - HTML
    [Figure 20.26](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers#Figure_20_03_generator)
  - HTML paragraph
    [`fs-id1167064928983`](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers#fs-id1167064928983)
  - HTML
    [Figures 20.27](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers#Figure_20_03_3dgen)
    and
    [20.28](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers#Figure_20_03_Light)
  - Equations 20.16-20.18
  - Official PDF, printed pp. 684-688

### Figure objective

Show how externally rotating an `N`-turn rectangular coil in a uniform
magnetic field converts mechanical input into alternating electrical output:
rotation changes the coil orientation and flux, magnetic forces drive charges
along the two vertical wire segments, current flows through an external load,
and the induced emf follows a sine wave.

### Required scientific content

- Elements: north/south field source or uniform `B` field arrows, rotating
  rectangular coil, shaft and rotation arrow `omega`, surface normal/angle
  `theta`, side-wire charge-force/current arrows, external circuit/load, emf
  graph.
- Directions: mechanical rotation enters through the shaft; `B` is uniform;
  forces on charges in the vertical sides are along the wire and combine in
  one loop-current direction at an instant; the top/bottom forces are
  perpendicular to their wires and do not drive current.
- Conditions: `N` turns, loop area `A`, constant angular velocity `omega`,
  uniform field `B`, and `theta = omega t`.
- Relations:
  `epsilon = N A B omega sin(omega t)`,
  `epsilon = epsilon0 sin(omega t)`,
  `epsilon0 = N A B omega`, and
  `T = 2 pi / omega`.
- Values and units: no numeric value is required. If displayed, use `B` in
  tesla, `A` in square metres, `omega` in radians per second, emf in volts,
  and `T` in seconds.

### Exact anchors

1. Paragraph `fs-id1167063183921`, PDF p. 686:
   “charges in the vertical wires experience forces parallel to the wire,
   causing a current to flow through the wire and through an external circuit
   if one is connected.”
2. The same paragraph, PDF p. 686:
   “A device such as this that converts mechanical energy into electrical
   energy is called a generator.”
3. Figure 20.26 caption, PDF p. 687:
   “When this coil is rotated through one-fourth of a revolution, the magnetic
   flux Φ changes from its maximum to zero, inducing an emf, which drives a
   current through an external circuit.”
4. Paragraph `fs-id1167064534859`, PDF p. 687, states the constant-angular-
   velocity condition and `theta = omega t`.
5. Equations `fs-id1164567983060`, `fs-id1164567015914`, and
   `fs-id1164567420614` (20.16-20.18), PDF p. 687, give the instantaneous and
   peak emf relations.
6. Figure 20.28 caption, PDF p. 688, supplies the sinusoidal output graph and
   `T = 2 pi / omega`.

### Blocking mutations

- Scientific: show constant nonzero emf for constant-speed rotation, or make
  emf peak when the sine relation requires a zero crossing. Expected outcome:
  `block`.
- Scientific: draw charge forces in the top and bottom segments as the source
  of loop current while omitting the vertical-side contribution. Expected
  outcome: `block`.
- Visual: reverse the magnetic-field direction without updating force/current
  arrows, or disconnect the coil from the depicted load while claiming
  current through it. Expected outcome: `block`.

## Admission Recommendation

All four candidates are scientifically bounded, independently checkable, and
redistribution-compatible under CC BY 4.0. They should advance to draft
baseline construction only while the additive `open-textbook` schema value
and its contract test remain present and passing.

Candidate progression remains:

```text
research candidate
  -> schema supports open-textbook
  -> locally cached hash-matched PDF
  -> schema-compatible candidate record plus draft gold baseline
  -> refine draft claims/anchors/elements/relations/mutations
  -> human scientific and visual acceptance review
  -> accepted corpus item
```

Do not infer human approval from this recommendation. A reviewer may reject or
replace any candidate, and the corpus must remain in `building` state until all
12 required items and their baselines are accepted.
