# Scientific Figure Graphical-Abstract Corpus Candidates

Last reviewed: 2026-07-26.

## Status And Admission Boundary

This note proposes four `graphical-abstract` evaluation candidates for Task 5
of the trustworthy scientific-figure workflow. It is source research, not
corpus admission or human approval.

The candidates cover mechanics, acoustics, quantum physics, and nuclear
physics. They do not repeat the Task 3 mechanism/process sections (8.2, 12.4,
16.3, and 20.2) or the Task 4 concept/comparison sections (5.4, 11.2, 16.1,
19.2, and 19.3). Each candidate is intentionally broader than a single
mechanism panel: it has one central message, an evidence-linked scientific
structure, an explicit abstraction level, and a bounded class of visual
assets that carry no scientific evidence.

Before any candidate is added to `eval/scientific-figures/corpus.json`, Task 5
must create a schema-valid draft baseline and keep `humanReview.status` as
`draft`. A human reviewer must separately accept its claims, anchors,
elements, relations, limitations, allowed variation, and blocking mutations.
The corpus must remain `building` until all 12 required items are accepted.

## Shared Source, License, And Extraction Evidence

### Source identity

- Title: *Physics*
- Authors: Paul Peter Urone and Roger Hinrichs
- Publisher: OpenStax
- First-party publication metadata date: 2020-03-26
- Source type: `open-textbook`
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

The first-party preface permits distribution, remixing, and adaptation with
attribution to OpenStax and its content contributors. These candidate
baselines call for new schematic compositions from cited statements. They do
not authorize copying an OpenStax figure or third-party artwork embedded in
the book. Any later reuse of source artwork requires a separate inspection of
that asset's credit and license.

Recommended attribution for derived corpus material:

> Adapted from *Physics* by Paul Peter Urone and Roger Hinrichs, OpenStax,
> licensed under CC BY 4.0. Changes were made.

### Hash-matched local evidence

The Git-ignored cache already contains the reviewed official PDF and its
Poppler-extracted text. Their bytes were re-hashed on 2026-07-26:

| Artifact | Byte length | SHA-256 |
| --- | ---: | --- |
| `Physics-WEB-a3f75487411e.pdf` | `57,463,331` | `a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e` |
| `Physics-WEB-a3f75487411e.txt` | `2,804,564` | `331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4` |

All four source records should use:

```text
contentHash: sha256:a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e
localCacheKey: openstax/physics/2026-07-26/Physics-WEB-a3f75487411e.pdf
textHash: sha256:331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4
extractionMethod: pdftotext 24.04.0 -layout
```

The official HTML pages are the precise public anchors; the hash-matched PDF
is the offline source authority. Admission must fail closed if either hash
changes until the new bytes and extraction are reviewed.

## Candidate Summary

| Proposed item ID | Domain | Central message | Approved abstraction |
| --- | --- | --- | --- |
| `mechanics-projectile-independent-components-summary` | Mechanics | Ideal projectile motion is one trajectory produced by independent horizontal and vertical motions sharing time | Qualitative-to-symbolic explanatory summary |
| `acoustics-doppler-relative-motion-summary` | Acoustics | Relative approach raises observed frequency and separation lowers it because wavefront encounter/spacing changes | Qualitative causal summary with bounded equations |
| `quantum-photoelectric-threshold-summary` | Quantum physics | One photon transfers energy to one electron; frequency controls threshold and maximum kinetic energy while intensity controls count | Conceptual evidence summary with one energy balance |
| `nuclear-fission-controlled-chain-summary` | Nuclear physics | A fission chain is self-sustaining only when enough released neutrons induce further fissions, and reactor controls regulate that feedback | System-level causal summary, not a reactor design |

## 1. Mechanics: Independent Components Of Projectile Motion

### Source record and exact locations

- Proposed `itemId`:
  `mechanics-projectile-independent-components-summary`
- Proposed `sourceId`: `openstax-physics-section-5-3`
- Public URL:
  <https://openstax.org/books/physics/pages/5-3-projectile-motion>
- Exact HTML anchors:
  - [ideal-projectile limitation paragraph](https://openstax.org/books/physics/pages/5-3-projectile-motion#fs-id1167067029756)
  - [independent-motion central claim](https://openstax.org/books/physics/pages/5-3-projectile-motion#fs-id1167066778142)
  - [separate-axis analysis](https://openstax.org/books/physics/pages/5-3-projectile-motion#fs-id1167067053283)
  - [Figure 5.27](https://openstax.org/books/physics/pages/5-3-projectile-motion#Figure_05_03_cannonball)
  - [Figure 5.29](https://openstax.org/books/physics/pages/5-3-projectile-motion#Figure_05_03_projectile)
- Official PDF printed pages 170-173.

### Bounded graphical-abstract objective

Compose a left-to-right summary with one launch state, one parabolic
trajectory, and two aligned component lanes. The horizontal lane shows
constant horizontal velocity; the vertical lane shows downward gravitational
acceleration and changing vertical velocity. Recombine the component arrows
at selected times and state the ideal-model limitation prominently.

### Central message and approved abstraction level

Central message: when air resistance is negligible, horizontal and vertical
projectile motions are independent but share the same elapsed time; their
vector recombination produces the observed trajectory.

Approved abstraction: qualitative-to-symbolic. The figure may omit a
particular projectile's mass, launch speed, launch angle, range, and maximum
height. It must not present a decorative arc as a measured or to-scale
trajectory. No numerical prediction is approved for this candidate.

### Scientific claims, elements, relations, and limitations

- Claims:
  - after launch, an ideal projectile experiences gravity while air
    resistance is neglected
  - horizontal and vertical motion can be analyzed independently
  - `a_x = 0`, `a_y = -g`, and horizontal velocity is constant in the chosen
    Earth-near, uniform-gravity model
  - the two component motions share time and recombine into total position
    and velocity
- Elements: launch point, projectile, trajectory, `x` and `y` axes, gravity
  arrow, horizontal component lane, vertical component lane, time markers,
  component velocity arrows, and recombined velocity arrows.
- Relations:
  - launch state -> independent horizontal and vertical evolution (causal)
  - `a_x = 0` -> constant `v_x` (causal under the model)
  - `a_y = -g` -> changing `v_y` (causal under the model)
  - `v_x` and `v_y` -> vector sum `v` (directional/compositional)
  - matching time markers connect both component lanes (associative,
    explicitly non-causal)
- Limitations:
  - air resistance must be negligible
  - gravity is treated as uniform and downward near Earth's surface
  - the schematic does not establish a real launch angle, distance, duration,
    object size, or measurement uncertainty

### Explicitly allowed non-evidentiary assets

- A generic ball, shell, or point glyph may represent the projectile.
- A flat ground band, simple sky fill, and restrained color coding may aid
  orientation.
- Dotted guide lines and repeated ghost positions may aid temporal reading.
- These assets must be marked by role as non-evidentiary and may vary in
  style, shape, and color without changing the scientific baseline.

Forbidden implication boundary: the background, projectile icon, arc
thickness, ghost-position spacing, and color changes must not imply measured
terrain, aerodynamic drag, time intervals, speed, uncertainty, or a
source-observed trajectory. A flame trail, wind streak, or scale grid is
forbidden unless separately evidence-linked.

### Evidence kinds

1. `verbatim-text`, Section 5.3 limitation paragraph, HTML
   `fs-id1167067029756`, PDF p. 170:
   “Projectile motion is the motion of an object thrown (projected) into the
   air when, after the initial force that launches the object, air resistance
   is negligible and the only other force that object experiences is the
   force of gravity.”
2. `verbatim-text`, Section 5.3 central claim, HTML
   `fs-id1167066778142`, PDF pp. 170-171:
   “The most important concept in projectile motion is that when air
   resistance is ignored, horizontal and vertical motions are independent,
   meaning that they don’t influence one another.”
3. `normalized-text`, Figure 5.27 caption, PDF p. 171: a horizontally
   launched projectile and a dropped object have the same vertical position
   over time in the ideal model.
4. `normalized-equation`, Section 5.3 component discussion and Table 5.1,
   PDF pp. 171-172: `a_x = 0`, `a_y = -g`, and the component motions share
   `t`.

### Blocking mutations

- Omitted limitation, scientific: remove the negligible-air-resistance
  condition while retaining the independence and parabolic-trajectory claims.
  Expected outcome: `block`.
- Invented visual claim, scientific: use uneven horizontal spacing of equal
  time markers while labeling `v_x` constant, or add a wind force unsupported
  by the source. Expected outcome: `block`.
- Decorative asset implying evidence, visual: render a scaled terrain grid,
  flame trail, or uncertainty band so it appears to report observed range,
  drag, speed, or error. Expected outcome: `block`.

## 2. Acoustics: Doppler Effect From Relative Motion

### Source record and exact locations

- Proposed `itemId`: `acoustics-doppler-relative-motion-summary`
- Proposed `sourceId`: `openstax-physics-section-14-3`
- Public URL:
  <https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms>
- Exact HTML anchors:
  - [Doppler definition](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms#fs-id1164566981984)
  - [three-scenario explanation](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms#fs-id1167063520278)
  - [moving-observer explanation](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms#fs-id1167063836052)
  - [relative-motion summary](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms#fs-id1167063772054)
  - [Figures 14.14-14.16](https://openstax.org/books/physics/pages/14-3-doppler-effect-and-sonic-booms#Figure_14_03_Stationary)
- Official PDF printed pages 446-448; equations 14.11 and 14.12.

### Bounded graphical-abstract objective

Compose a three-column summary: stationary source/observer, approach, and
separation. Use wavefront spacing and observer encounter rate to connect
relative motion to observed frequency. Include compact source-moving and
observer-moving equations as bounded references, not as a universal
relativistic formula.

### Central message and approved abstraction level

Central message: in a material medium with fixed sound speed, approach
increases observed frequency and separation decreases it; source motion
changes emitted wavefront spacing while observer motion changes the rate at
which wavefronts are encountered.

Approved abstraction: qualitative causal summary with bounded equations. A
generic siren/source and listener may replace the textbook examples. The
figure need not encode an actual source frequency, speed, sound level, or
distance. It must preserve the distinction between source motion and observer
motion.

### Scientific claims, elements, relations, and limitations

- Claims:
  - the Doppler effect is a change in observed pitch/frequency caused by
    relative source-observer motion
  - for a moving source, wavefronts are closer ahead and farther behind
  - an observer moving toward a source encounters wavefronts more frequently;
    moving away decreases the encounter frequency
  - for the stated sound model, `v_w` is the sound speed in the medium
- Elements: sound source, observer, medium label, source/observer motion
  arrows, wavefronts, approach panel, separation panel, `f_s`, `f_obs`,
  `v_s`, `v_obs`, and `v_w`.
- Relations:
  - source approach -> shorter forward wavelength -> higher `f_obs`
  - source recession -> longer wavelength -> lower `f_obs`
  - observer approach -> greater crest encounter rate -> higher `f_obs`
  - observer recession -> lower crest encounter rate -> lower `f_obs`
- Limitations:
  - equations 14.11 and 14.12 are separate nonrelativistic sound cases in a
    medium; they are not one arbitrary vector formula
  - speeds are along the source-observer line and remain below the sound speed
    for the displayed formulas
  - wavefront spacing shows wavelength, not amplitude or measured sound level
  - the candidate excludes sonic-boom shock geometry and electromagnetic
    Doppler shift

### Explicitly allowed non-evidentiary assets

- A generic vehicle, loudspeaker, listener, road strip, or direction icon may
  establish roles.
- Color may distinguish approach from separation and source motion from
  observer motion.
- Uniform decorative rings may stand in for wavefronts only when their
  spacing remains scientifically consistent with the panel.

Forbidden implication boundary: vehicle model, background road, ring color,
ring thickness, and icon size do not encode source power, loudness, frequency,
speed, distance, or measured data. Decorative motion streaks must not create
extra wavefronts or suggest a quantified velocity.

### Evidence kinds

1. `verbatim-text`, Section 14.3 definition, HTML
   `fs-id1164566981984`, PDF p. 446:
   “The Doppler effect is a change in the observed pitch of a sound, due to
   relative motion between the source and the observer.”
2. `normalized-text`, Figures 14.15 and 14.16 and surrounding prose, HTML
   `fs-id1167063520278` and `fs-id1167063836052`, PDF pp. 446-447:
   approach compresses source wavefront spacing or raises observer encounter
   rate; separation has the opposite effect.
3. `verbatim-text`, relative-motion summary, HTML
   `fs-id1167063772054`, PDF p. 447:
   “Relative motion of source and observer toward one another increases the
   perceived frequency. Relative motion apart decreases the perceived
   frequency.”
4. `normalized-equation`, equations 14.11 and 14.12, PDF pp. 447-448:
   for a stationary observer and moving source,
   `f_obs = f_s v_w / (v_w +/- v_s)`; for a stationary source and moving
   observer, `f_obs = f_s (v_w +/- v_obs) / v_w`, with signs selected by
   approach or separation as defined in the source.

### Blocking mutations

- Omitted limitation, scientific: omit the material-medium/fixed-sound-speed
  boundary and present the sound equations as universally valid, including
  light or relativistic speeds. Expected outcome: `block`.
- Invented visual claim, scientific: make wavefront spacing tighter while
  labeling the corresponding observed frequency lower, or merge source and
  observer motion into one unsupported equation. Expected outcome: `block`.
- Decorative asset implying evidence, visual: use ring opacity, thickness,
  vehicle size, or a speedometer-like badge as if they encode measured
  loudness, frequency, or speed. Expected outcome: `block`.

## 3. Quantum Physics: Photoelectric Threshold And Energy Allocation

### Source record and exact locations

- Proposed `itemId`: `quantum-photoelectric-threshold-summary`
- Proposed `sourceId`: `openstax-physics-section-21-2`
- Public URL:
  <https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect>
- Exact HTML anchors:
  - [five observed properties and their photon-model explanation](https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect#fs-id1167065947633)
  - [Figure 21.6](https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect#Figure_21_02_Flashlight)
  - [Figure 21.7 caption and photoelectric-effect evidence statement](https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect#fs-id1167066086108)
  - [Figure 21.8](https://openstax.org/books/physics/pages/21-2-einstein-and-the-photoelectric-effect#Figure_21_02_Graph)
- Official PDF printed pages 718-720; equation 21.6.

### Bounded graphical-abstract objective

Compose an input-to-outcome summary centered on a clean metal surface. Split
the input into photon frequency and photon count/intensity. Show that a photon
below the material's threshold cannot eject an electron regardless of
intensity; above threshold, one photon's energy pays the binding energy and
the remainder becomes the electron's maximum kinetic energy. Show intensity
changing the number of emitted electrons, not their maximum kinetic energy.

### Central message and approved abstraction level

Central message: photoemission is an individual photon-electron energy
transfer. Frequency sets photon energy and therefore threshold and maximum
electron kinetic energy; above threshold, intensity changes the number of
available photons and thus the electron count.

Approved abstraction: conceptual evidence summary with one normalized energy
balance. Electron, photon, and metal glyphs are schematic. The figure may
compare below/above threshold and low/high intensity without reporting an
experimental dataset, material-specific work function, current, wavelength,
or stopping voltage.

### Scientific claims, elements, relations, and limitations

- Claims:
  - photon energy is `E = h f`
  - an individual photon transfers energy to an individual electron
  - below threshold frequency `f_0`, no electron is ejected regardless of
    intensity
  - above threshold, increased intensity increases electron count/rate but
    not maximum kinetic energy
  - maximum kinetic energy obeys `KE_max = h f - BE`, with `BE = h f_0`
- Elements: incident photons, frequency lane, intensity/photon-count lane,
  clean metal surface, bound electron, binding-energy barrier `BE`, ejected
  electron, below-threshold state, above-threshold state, and `KE_max`.
- Relations:
  - higher `f` -> higher energy per photon
  - `h f < BE` -> no photoemission
  - `h f >= BE` -> electron ejection with remaining energy as `KE_max`
  - higher intensity at fixed above-threshold `f` -> more emitted electrons
  - intensity at fixed `f` -/-> greater `KE_max` (explicit non-causal
    relation)
- Limitations:
  - the source discussion assumes monochromatic incident radiation for the
    five-property comparison and a clean metal surface
  - `BE`/work function depends on the material
  - glyph counts are qualitative unless separately labeled and evidence-linked
  - the candidate does not depict measured current, quantum efficiency,
    electron-energy distributions, surface contamination, or uncertainty

### Explicitly allowed non-evidentiary assets

- Stylized photon packets, electron dots, a lamp housing, and a metal-plate
  texture may identify roles.
- Color may distinguish below- and above-threshold cases, but hue must not
  encode a wavelength unless the legend explicitly says so.
- A simple barrier or step motif may represent `BE`; its drawn height is not
  quantitative.

Forbidden implication boundary: glow brightness, photon/electron glyph count,
color, barrier height, arrow length, and plate texture must not appear to be
measured intensity, frequency, kinetic energy, work function, yield, or
material identity. A graph-like axis or error bar is forbidden unless its
values and provenance are added to the evidence contract.

### Evidence kinds

1. `normalized-text`, Figure 21.6 and surrounding text, PDF p. 718: an
   electromagnetic wave of frequency `f` is composed of photons with
   individual energy `h f`; higher intensity means more photons per unit area
   per second.
2. `verbatim-text`, property 1 in HTML `fs-id1167065947633`, PDF p. 719:
   “For a given material, there is a threshold frequency f0 for the EM
   radiation below which no electrons are ejected, regardless of intensity.”
3. `verbatim-text`, property 4 in the same HTML anchor, PDF p. 719:
   “The maximum kinetic energy of ejected electrons is independent of the
   intensity of the EM radiation.”
4. `normalized-equation`, equation 21.6 and its following definition, HTML
   `fs-id1164567516604`, PDF p. 719:
   `KE_max = h f - BE` and `BE = h f_0`.

### Blocking mutations

- Omitted limitation, scientific: remove the material-dependent threshold or
  monochromatic/clean-surface context while claiming one universal numeric
  work function. Expected outcome: `block`.
- Invented visual claim, scientific: show brighter light increasing
  `KE_max` at fixed frequency, or show below-threshold photons ejecting
  electrons merely by increasing their number. Expected outcome: `block`.
- Decorative asset implying evidence, visual: use exact-looking photon counts,
  a scaled barrier, spectral colors, plot axes, or error bars so decorative
  choices appear to report measured intensity, `BE`, wavelength, yield, or
  uncertainty. Expected outcome: `block`.

## 4. Nuclear Physics: Conditional And Controlled Fission Chain

### Source record and exact locations

- Proposed `itemId`: `nuclear-fission-controlled-chain-summary`
- Proposed `sourceId`: `openstax-physics-section-22-4`
- Public URL:
  <https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion>
- Exact HTML anchors:
  - [neutron-induced fission explanation](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#fs-id1167067092552)
  - [chain-reaction conditions and limitations](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#fs-id1167066961774)
  - [reactor control and residual-heat limitation](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#fs-id1167067029303)
  - [Figure 22.26](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#Figure_22_04_Fission)
  - [Figure 22.27](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#Figure_22_04_Reaction)
  - [Figure 22.28](https://openstax.org/books/physics/pages/22-4-nuclear-fission-and-fusion#Figure_22_04_WaterReactor)
- Official PDF printed pages 771-774; equation 22.65.

### Bounded graphical-abstract objective

Compose a system-level summary with three layers: neutron-induced fission
releases energy and additional neutrons; only some neutrons induce subsequent
fissions; moderator and control-rod roles regulate the neutron population in
the textbook's pressurized-water-reactor example. End with an explicit
residual-decay-heat limitation. Do not provide engineering dimensions,
operating settings, fuel geometry, or weapon design.

### Central message and approved abstraction level

Central message: fission can become a self-sustaining chain only when enough
released neutrons induce further fissions. Reactor operation regulates that
feedback through neutron moderation and absorption, but terminating the
chain does not eliminate heat from radioactive fission products.

Approved abstraction: system-level causal summary. Nuclei and neutrons are
tokens, not a to-scale atomic model or a count-based simulation. A simplified
reactor silhouette may identify functional roles, but the result is neither a
complete reactor design nor evidence of the safety or performance of any real
facility.

### Scientific claims, elements, relations, and limitations

- Claims:
  - absorbing a neutron can deform and split a heavy nucleus, releasing
    energy and additional neutrons
  - not every released neutron induces another fission
  - self-sustained fission depends on enough neutrons causing subsequent
    fissions and on material-dependent conditions including critical mass
  - in the cited reactor example, water slows neutrons and control rods absorb
    neutrons to regulate neutron flux
  - stopping the chain reaction does not remove residual heat from radioactive
    fission products
- Elements: incident neutron, fissile nucleus, two fission fragments, released
  energy, emitted neutrons, induced-next-fission branch, escape/non-fission
  branch, moderator, control rods, heat pathway, and residual-decay-heat
  warning.
- Relations:
  - neutron absorption -> nucleus deformation/splitting
  - fission -> fragments + energy + emitted neutrons
  - sufficient subsequent neutron-induced fissions -> self-sustaining chain
  - moderator -> slower neutrons in the cited `235U` reactor context
  - control-rod insertion -> greater neutron absorption -> reduced neutron flux
  - chain termination -/-> immediate elimination of decay heat (explicit
    non-causal relation)
- Limitations:
  - token counts and branch ratios are qualitative; they do not encode a
    measured multiplication factor, neutron spectrum, reaction rate, or power
  - critical mass depends on nuclide and physical conditions; it is not a
    universal drawn volume
  - the pressurized-water-reactor control summary is bounded to the source and
    is not a complete safety case
  - no weapon configuration, enrichment recipe, critical dimension, or
    actionable operating parameter is part of this candidate

### Explicitly allowed non-evidentiary assets

- Generic nucleus/neutron tokens, restrained energy-glow marks, a water-flow
  band, control-rod silhouettes, and a generic containment outline may
  establish roles.
- Branches may be spatially arranged for readability without representing an
  actual neutron path or geometry.
- A warning icon may flag residual heat, provided its meaning is stated in
  text.

Forbidden implication boundary: token counts, glow area, branch density,
reactor silhouette, water color, hazard icon, and containment thickness must
not imply measured neutron flux, power, radiation dose, probability,
efficiency, critical mass, facility design, accident evidence, or a certified
safety margin. Photographic smoke, blast, or facility imagery is excluded
because it can imply an observed event or named installation.

### Evidence kinds

1. `normalized-text`, Figure 22.26 and surrounding prose, HTML
   `fs-id1167067092552`, PDF pp. 771-772: neutron absorption can deform and
   split a heavy nucleus, releasing energy and additional neutrons.
2. `verbatim-text`, HTML `fs-id1167066961774`, PDF p. 772:
   “However, not every neutron produced by fission induces further fission.”
3. `verbatim-text`, Figure 22.27 caption, PDF p. 772:
   “A chain reaction can produce self-sustained fission if each fission
   produces enough neutrons to induce at least one more fission.”
4. `normalized-text`, Figure 22.28 and HTML `fs-id1167067029303`, PDF p. 773:
   water slows neutrons in the cited reactor, control rods regulate neutron
   flux, and radioactive fission products can continue generating heat after
   the chain terminates.
5. `normalized-equation`, equation 22.65, HTML
   `fs-id1167067103429`, PDF p. 773: `E = m c^2`; this supports the
   mass-energy statement only and does not quantify the candidate's chain or
   reactor tokens.

### Blocking mutations

- Omitted limitation, scientific: state that inserting control rods or losing
  moderator immediately removes all reactor heat, omitting residual
  radioactive-fission-product heat. Expected outcome: `block`.
- Invented visual claim, scientific: make an exact number of drawn neutrons,
  branch generations, or token sizes assert a multiplication factor, reaction
  rate, critical mass, power output, or probability absent from the source.
  Expected outcome: `block`.
- Decorative asset implying evidence, visual: use smoke, blast imagery,
  facility photographs, radiation heat maps, scaled containment, or gauge-like
  graphics so decoration appears to document an accident, dose, plant
  identity, operating value, or safety margin. Expected outcome: `block`.

## Admission Recommendation

The four candidates are public, text-extractable, hash-bound, and suitable for
testing the graphical-abstract distinction between authoritative scientific
structure and permitted non-evidentiary styling. Each has:

- one bounded central message
- an explicit abstraction level
- evidence-linked claims, elements, and relations
- at least one essential limitation
- explicit non-evidentiary assets and a forbidden implication boundary
- blocking mutations for omitted limitations, invented visual claims, and
  decorative assets that imply evidence

Recommended progression:

```text
research candidate
  -> schema-compatible candidate source plus draft baseline
  -> validate every claim/element/relation anchor
  -> verify allowed assets carry no evidentiary semantics
  -> run scientific and visual mutations
  -> human scientific and visual acceptance review
  -> accepted corpus item
```

This recommendation does not constitute human approval. The corpus must
remain `building`, and each baseline's `humanReview.status` must remain
`draft`, until an identified human reviewer records acceptance.
