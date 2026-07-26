# Checkpoint 0 Scientific Figure Corpus Human Review

Status: review worksheet only

Prepared: 2026-07-26
Corpus: `scientific-figures-v1`

## Review Boundary

This worksheet supports human review of the 12 scientific-figure corpus
candidates. Completing or editing this worksheet does not by itself admit a
candidate. Until an identified reviewer explicitly records a decision and the
repository validation passes:

- every corpus item remains `admissionStatus: candidate`
- every gold baseline remains `humanReview.status: draft`
- the corpus remains `admissionState: building`
- Checkpoint 0 and Task 6 remain blocked

For each item, verify the cited source context and baseline rather than relying
only on the summary below. Mark exactly one decision after completing all six
review areas.

Reviewer: ____________________________________

Review date: __________________________________

Scientific qualification or review authority: ______________________________

Decision meanings:

- `Accept`: source, claims, relations, limitations, variations, and mutations
  are suitable as corpus authority without revision.
- `Revise`: the candidate is potentially suitable but specified corrections
  must be completed and reviewed again.
- `Reject`: the candidate must not become corpus authority.

## A. Mechanism And Process Figures

### A1. Two-Car Momentum Transfer

- Item: `mechanics-two-car-momentum-transfer`
- Source: [OpenStax Physics 8.2, Conservation of Momentum](https://openstax.org/books/physics/pages/8-2-conservation-of-momentum)
- Baseline:
  `eval/scientific-figures/baselines/mechanism-process/mechanics-two-car-momentum-transfer.json`
- Objective: show before, contact, and after states for momentum transfer
  between two co-directional cars.

Review:

- [ ] Core claims: `m1` slows and loses momentum; `m2` speeds up and gains
  momentum; contact forces and impulses are equal and opposite; total
  two-car momentum is unchanged before/after under the stated boundary.
- [ ] Relations: momentum transfers from `m1` to `m2`; equal/opposite impulses
  produce the final momenta; before/after total momentum is compared as equal.
- [ ] Limitations: friction and other external forces are negligible; the
  source supports symbolic quantities but no mandatory numeric values.
- [ ] Allowed variation: horizontal or vertical time layout and car styling
  may vary only when time order, system boundary, arrow directions, and
  relative scientific meaning remain unambiguous.
- [ ] Blocking mutations: reversed transfer, same-direction impulses,
  unconditional conservation after deleting the external-force condition,
  swapped final-velocity labels, or unreadable arrowheads all block.
- [ ] Source anchors, elements, equations, relation endpoints, and mutation
  outcomes in the baseline are complete and scientifically correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### A2. Heat-Engine Energy Flow

- Item: `thermal-heat-engine-energy-flow`
- Source: [OpenStax Physics 12.4, Applications of Thermodynamics](https://openstax.org/books/physics/pages/12-4-applications-of-thermodynamics-heat-engines-heat-pumps-and-refrigerators)
- Baseline:
  `eval/scientific-figures/baselines/mechanism-process/thermal-heat-engine-energy-flow.json`
- Objective: show heat entering from a hot reservoir, work leaving the engine,
  and rejected heat reaching a cold reservoir.

Review:

- [ ] Core claims: part of `Qh` becomes work; `Qc` is rejected; a complete
  cycle returns internal energy to its initial value; `W = Qh - Qc` and
  `Eff = W / Qh`.
- [ ] Relations: `Qh` flows hot reservoir -> engine; the cycle produces
  outward `W`; `Qc` flows engine -> cold reservoir; the `Qh - Qc`
  difference equals work output.
- [ ] Limitations: the device is a cyclic heat engine with a hot and cold
  reservoir; perfect conversion `Qh = W` is not allowed; any later numeric
  `Qh`, `Qc`, and `W` values must use one consistent energy unit.
- [ ] Allowed variation: reservoir layout and hot/cold styling may vary only
  when `Th`, `Tc`, `Qh`, `Qc`, `W`, and all flow directions remain readable.
- [ ] Blocking mutations: deleting `Qc`, implying perfect conversion,
  reversing heat flows, pointing work into the engine, or attaching heat
  labels to the wrong reservoirs all block.
- [ ] Source anchors, cycle condition, equations, elements, and relation
  directions in the baseline are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### A3. Convex-Lens Real Image

- Item: `optics-convex-lens-real-image`
- Source: [OpenStax Physics 16.3, Lenses](https://openstax.org/books/physics/pages/16-3-lenses)
- Baseline:
  `eval/scientific-figures/baselines/mechanism-process/optics-convex-lens-real-image.json`
- Objective: draw a to-scale principal-ray construction for `f = 0.50 m`,
  `do = 0.75 m`, `di = 1.5 m`, and `m = -2.0`.

Review:

- [ ] Core claims: a parallel ray exits through the far focus; a center ray
  continues undeviated; a near-focus ray exits parallel; refracted rays cross
  at a real, inverted image twice the object height.
- [ ] Relations: each incident ray follows its evidence-backed refracted path;
  ray crossing and the thin-lens equation determine the image location and
  character.
- [ ] Limitations: the baseline uses the thin-lens/principal-ray model,
  positive converging-lens focal length, the stated object distance, and the
  stated sign convention; its scale must preserve the `0.50:0.75:1.5`
  geometry.
- [ ] Allowed variation: uniform scale, ray color, and line style may vary if
  all three paths, focal labels, distances, and arrow directions remain
  distinguishable.
- [ ] Blocking mutations: bending the center ray, missing the far focus,
  claiming an upright/virtual result, swapping focal labels, omitting
  arrowheads, or drawing `0.15 m` while labeling `1.5 m` all block.
- [ ] Equations, values, units, sign interpretation, source anchors, and
  element/relation endpoints are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### A4. Rotating-Coil Generator

- Item: `electromagnetism-rotating-coil-generator`
- Source: [OpenStax Physics 20.2, Motors, Generators, and Transformers](https://openstax.org/books/physics/pages/20-2-motors-generators-and-transformers)
- Baseline:
  `eval/scientific-figures/baselines/mechanism-process/electromagnetism-rotating-coil-generator.json`
- Objective: connect mechanical rotation, changing magnetic flux, induced emf,
  and sinusoidal electrical output.

Review:

- [ ] Core claims: vertical wire-segment forces drive current; mechanical
  input becomes electrical output; changing flux induces emf; for uniform
  rotation, `epsilon = N A B omega sin(omega t)` and
  `epsilon0 = N A B omega`.
- [ ] Relations: shaft input causes rotation; magnetic forces act along the
  relevant wire segments; changing flux produces sinusoidal emf; emf drives
  alternating current through the connected circuit.
- [ ] Limitations: the coil has `N` turns and area `A`, rotates at constant
  angular velocity in a uniform field, and uses a connected external circuit;
  any values must preserve the recorded SI units and `T = 2 pi / omega`.
- [ ] Allowed variation: mechanism and waveform layout plus vector colors may
  vary only with an unambiguous legend and preserved time/vector relations.
- [ ] Blocking mutations: constant nonzero emf during uniform rotation,
  waveform peaks at required zero crossings, wrong segment-force mechanism,
  inconsistent field/force/current reversal, or a disconnected claimed load
  all block.
- [ ] Source anchors, vector directions, equations, units, elements, and
  causal relations are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

## B. Concept And Comparison Figures

### B1. Static Versus Kinetic Friction

- Item: `mechanics-static-vs-kinetic-friction`
- Source: [OpenStax Physics 5.4, Inclined Planes](https://openstax.org/books/physics/pages/5-4-inclined-planes)
- Baseline:
  `eval/scientific-figures/baselines/concept-comparison/mechanics-static-vs-kinetic-friction.json`

Review:

- [ ] Core claims: static friction acts without sliding and responds up to
  `fs(max)`; kinetic friction acts during sliding; friction opposes motion or
  attempted motion; maximum static friction is usually, not universally,
  greater for the same contact pair.
- [ ] Relations: exceeding `fs(max)` causes transition to sliding; friction
  direction opposes motion; the regimes are compared; both coefficients are
  associated with the same surfaces and normal force.
- [ ] Limitations: panels use the same contact pair and fixed `N`; friction is
  surface-parallel; coefficients are dimensionless; “usually greater” must
  not become an exceptionless law.
- [ ] Allowed variation: panel order, crate styling, and neutral texture may
  vary without changing materials, threshold order, labels, or directions.
- [ ] Blocking mutations: `fs = mu_s N` in every static state, exceeding
  `fs(max)`, swapping regimes, reversing friction, or removing the transition
  threshold all block.
- [ ] Source anchors, inequalities/equations, relation classes, and visual
  distinctions are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### B2. Three Heat-Transfer Modes

- Item: `thermal-three-mode-heat-transfer-comparison`
- Source: [OpenStax Physics 11.2, Heat Transfer](https://openstax.org/books/physics/pages/11-2-heat-specific-heat-and-heat-transfer)
- Baseline:
  `eval/scientific-figures/baselines/concept-comparison/thermal-three-mode-heat-transfer-comparison.json`

Review:

- [ ] Core claims: conduction uses direct contact; convection uses moving
  liquid or gas; radiation uses emitted/absorbed electromagnetic radiation
  and needs no material medium; multiple modes may coexist.
- [ ] Relations: temperature difference produces net transfer; net direction
  is higher -> lower temperature; carrier/medium requirements are compared;
  named modes are non-causally associated as coexisting transfer modes.
- [ ] Limitations: a temperature difference is required; heat is not a stored
  material substance; no mode is asserted to dominate universally; any later
  quantities require evidence and consistent units.
- [ ] Allowed variation: columns, rows, icons, and mnemonic contexts may vary
  if carrier, medium, arrows, and scientific labels remain explicit.
- [ ] Blocking mutations: making radiation require matter, swapping
  conduction/convection, claiming mutual exclusivity, erasing carrier
  distinctions, or reversing net transfer without work all block.
- [ ] Source definitions, relation classes, medium conditions, elements, and
  arrows are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### B3. Specular Versus Diffuse Reflection

- Item: `optics-specular-vs-diffuse-reflection`
- Source: [OpenStax Physics 16.1, Reflection](https://openstax.org/books/physics/pages/16-1-reflection)
- Baseline:
  `eval/scientific-figures/baselines/concept-comparison/optics-specular-vs-diffuse-reflection.json`

Review:

- [ ] Core claims: `theta_r = theta_i` relative to the local normal; smooth
  aligned normals yield ordered reflection; rough varying normals yield many
  global directions; diffuse reflection does not suspend the local law.
- [ ] Relations: varying local orientation causes distributed directions;
  rays travel incident point -> reflected direction; outcomes are compared;
  both angles share the same local-normal reference.
- [ ] Limitations: geometric-optics rays and unchanged incident medium are
  assumed; angles are measured from the local normal, not the surface; any
  example angle must use a consistent unit and equal local values.
- [ ] Allowed variation: panel orientation, surface/ray colors, and everyday
  icons may vary if normals remain distinct and never become substitute
  evidence.
- [ ] Blocking mutations: unequal local angles, surface-referenced angles,
  claiming roughness violates reflection law, swapping labels, confusing
  normals with rays, or hiding the rough-surface basis all block.
- [ ] Source anchors, local geometry, relation classes, and panel comparison
  are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### B4. Series Versus Parallel Resistors

- Item: `electromagnetism-series-vs-parallel-resistors`
- Sources:
  [OpenStax Physics 19.2, Series Circuits](https://openstax.org/books/physics/pages/19-2-series-circuits) and
  [19.3, Parallel Circuits](https://openstax.org/books/physics/pages/19-3-parallel-circuits)
- Baseline:
  `eval/scientific-figures/baselines/concept-comparison/electromagnetism-series-vs-parallel-resistors.json`

Review:

- [ ] Core claims: series elements share current and add resistance; parallel
  branches share voltage, split/recombine current, and use reciprocal-sum
  resistance below the smallest positive branch resistance.
- [ ] Relations: added parallel paths lower equivalent resistance;
  conventional current splits/recombines; series/parallel conservation rules
  are compared; parallel resistors share the same two nodes.
- [ ] Limitations: steady-state DC, ideal wires, positive ohmic resistors, the
  same ideal battery voltage, and conventional-current direction are assumed.
- [ ] Allowed variation: orientation and IEC/ANSI glyph style may vary;
  connectivity, junctions, arrows, labels, and shared nodes remain
  evidentiary and unambiguous.
- [ ] Blocking mutations: unequal series current without a branch/transient,
  unequal voltage across shared parallel nodes, wrong equivalent-resistance
  rule, broken junctions, or currents that never recombine all block.
- [ ] Both source sections, equations, topology, relation classes, units, and
  endpoints are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

## C. Graphical Abstracts

### C1. Projectile Independent Components

- Item: `mechanics-projectile-independent-components-summary`
- Source: [OpenStax Physics 5.3, Projectile Motion](https://openstax.org/books/physics/pages/5-3-projectile-motion)
- Baseline:
  `eval/scientific-figures/baselines/graphical-abstract/mechanics-projectile-independent-components-summary.json`

Review:

- [ ] Core claims: with negligible air resistance and uniform downward
  gravity, horizontal and vertical motions are independent, share time, and
  recombine into one trajectory.
- [ ] Relations: component vectors compose total motion; the component lanes
  share matching times without causally influencing one another.
- [ ] Limitations: near-Earth uniform gravity and negligible drag are assumed;
  the qualitative-to-symbolic schematic establishes no measured launch angle,
  range, duration, size, speed, or uncertainty.
- [ ] Allowed variation: lane layout, colors, generic projectile, ground/sky,
  guides, and ghost positions may vary but carry no measurement evidence.
- [ ] Blocking mutations: omitting the drag condition, unequal horizontal
  spacing at equal times with constant `vx`, unsupported wind, or decorative
  scale/terrain/trails/error bands implying measurements all block.
- [ ] Central message, abstraction level, source anchors, elements, component
  relations, and asset boundary are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### C2. Doppler Relative Motion

- Item: `acoustics-doppler-relative-motion-summary`
- Source: [OpenStax Physics 14.3, Doppler Effect](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms)
- Baseline:
  `eval/scientific-figures/baselines/graphical-abstract/acoustics-doppler-relative-motion-summary.json`

Review:

- [ ] Core claims: approach raises and separation lowers observed sound
  frequency; moving-source wavefront spacing and moving-observer encounter
  rate are distinct causal cases.
- [ ] Relations: approach causes higher observed frequency; wavefronts
  propagate through the medium toward the observer.
- [ ] Limitations: equations are separate nonrelativistic sound cases in a
  material medium, with line-of-sight subsonic motion; spacing is wavelength,
  not amplitude/loudness; sonic booms and electromagnetic Doppler are
  excluded.
- [ ] Allowed variation: panel layout, colors, vehicle/source/listener/road
  icons may vary but cannot encode speed, loudness, distance, power, or
  measured frequency.
- [ ] Blocking mutations: universalizing the equations to light/relativity,
  reversing spacing/frequency meaning, merging cases into an unsupported
  formula, or using rings/icons/badges as measurement evidence all block.
- [ ] Central message, bounded equations, source anchors, causal distinction,
  and non-evidentiary asset boundary are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### C3. Photoelectric Threshold

- Item: `quantum-photoelectric-threshold-summary`
- Source: [OpenStax Physics 21.2, Photoelectric Effect](https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect)
- Baseline:
  `eval/scientific-figures/baselines/graphical-abstract/quantum-photoelectric-threshold-summary.json`

Review:

- [ ] Core claims: one photon transfers energy to one electron; frequency
  controls threshold and `KE_max`; above threshold, intensity changes emitted
  electron count/rate but not `KE_max`; `KE_max = h f - BE`.
- [ ] Relations: photon energy at/above the material threshold enables
  emission; intensity at fixed frequency has an explicit non-causal
  relationship to maximum kinetic energy.
- [ ] Limitations: monochromatic radiation and a clean, material-dependent
  surface are assumed; glyph counts and barrier height are qualitative; no
  measured current, yield, spectrum, stopping voltage, or uncertainty is
  claimed.
- [ ] Allowed variation: frequency/intensity layout, color, photon/electron
  glyphs, lamp, plate texture, and barrier motif may vary but carry no
  quantitative evidence.
- [ ] Blocking mutations: deleting the material threshold, claiming a
  universal work function, using intensity to increase `KE_max` or trigger
  below-threshold emission, or decorative counts/axes/error bars implying
  data all block.
- [ ] Central message, energy equation, source anchors, causal/non-causal
  distinction, and asset boundary are complete and correct.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

### C4. Controlled Fission Chain

- Item: `nuclear-fission-controlled-chain-summary`
- Source: [OpenStax Physics 22.4, Nuclear Fission and Fusion](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion)
- Baseline:
  `eval/scientific-figures/baselines/graphical-abstract/nuclear-fission-controlled-chain-summary.json`

Review:

- [ ] Core claims: neutron-induced fission releases energy and neutrons; only
  some neutrons induce later fissions; sufficient feedback sustains a chain;
  moderation/absorption regulate feedback; decay heat remains after shutdown.
- [ ] Relations: fission can feed subsequent fission; moderator/control roles
  regulate neutron feedback; chain termination does not cause immediate
  elimination of residual heat.
- [ ] Limitations: token counts and geometry are qualitative; critical mass is
  condition-dependent; the cited pressurized-water-reactor summary is not a
  design or safety case; no enrichment recipe, dimension, weapon
  configuration, operating setting, flux, power, or probability is approved.
- [ ] Allowed variation: tokens, branch layout, water band, rods, containment
  outline, glow, and warning icon may vary but cannot encode facility identity
  or quantitative evidence.
- [ ] Blocking mutations: claiming rods/shutdown eliminate all heat, using
  token count/geometry as reactor parameters, or smoke/photos/heat maps/gauges
  as accident, dose, operating, or safety evidence all block.
- [ ] Central message, residual-heat limitation, source anchors, causal
  relations, excluded capability boundary, and asset boundary are complete.

Decision:

- [ ] Accept
- [ ] Revise
- [ ] Reject

Corrections or reviewer notes:

______________________________________________________________________________

## Checkpoint 0 Final Decision

Complete only after all 12 item decisions are recorded and every requested
revision has been re-reviewed.

- [ ] All 12 sources and license/extraction evidence are acceptable.
- [ ] All 12 baseline schemas and semantic references pass.
- [ ] Exactly four mechanism/process, four concept/comparison, and four
  graphical-abstract candidates are approved.
- [ ] Every item above is marked `Accept`; no unresolved `Revise` or `Reject`
  decision remains.
- [ ] Full repository fixed-order verification passes on the proposed
  acceptance-state change.
- [ ] The reviewer authorizes repository projection to
  `accepted / accepted / human-approved`.

Final decision:

- [ ] Approve all 12 items and authorize Checkpoint 0 closeout
- [ ] Do not approve; corrections remain open

Reviewer signature or traceable identifier: _________________________________

Decision date: __________________

Required corrections, exceptions, or scope notes:

______________________________________________________________________________
______________________________________________________________________________

After explicit final approval, the implementation agent must separately:

1. update all 12 baseline `humanReview` records with `accepted`, reviewer, and
   review date;
2. update all 12 corpus records to `admissionStatus: accepted`;
3. set corpus `admissionState: human-approved`;
4. synchronize Task 3-5, Checkpoint 0, task status, and approval evidence;
5. run the fixed repository gates and commit the acceptance projection;
6. start Task 6 only after every preceding step succeeds.
