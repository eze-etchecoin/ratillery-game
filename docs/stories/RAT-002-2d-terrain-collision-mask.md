# RAT-002: 2D terrain + collision mask

status: ready

## Story

As a player,
I want to see the rat standing on real 2D terrain — a ground that shows the
layered cross-section of DEC-003 (surface grass/earth → rock → dark interior)
instead of the flat staging floor of RAT-001,
so that the game starts to look like a destructible world and the foundation
for cratering (slices 6–8) is in place.

This is slice 2 of the milestone "A rat can destroy the world". RAT-001
established the animated idle rat standing on a flat, non-destructible floor
reference used purely for staging. This slice replaces that staging with two
halves that land together:

1. **Rendered terrain**: a terrain body spanning the playfield, with a clear
   surface profile and a depth-layered look (surface earth/grass → rock →
   dark interior) that makes destruction legibility obvious — the "tierra →
   roca → interior oscuro" direction of DEC-003.
2. **Collision mask**: an explicit solid/empty representation of exactly that
   terrain (a pixel-grid mask, per the original product brief and
   `docs/product/game-rules.md`: destructible terrain = terrain texture +
   collision mask). The mask lives in the MonoGame-free layer
   (`src/Ratillery.Core`) so it is unit-testable without a graphics window.
   It can answer "is this position solid?" and "what is the terrain surface
   height at this column?" but nothing in this slice modifies it: no
   explosions, no craters, no impacts, no mask mutation of any kind. The mask
   is the static substrate that future slices (impact, explosion, crater)
   will query and carve.

The rat keeps its RAT-001 looping idle animation, its bottom-center pivot and
its right-facing orientation. Instead of resting on the staging line, its feet
now rest on the real terrain surface at its column, using the mask's
per-column surface height as the minimal placement mechanism (no physics or
movement in this slice).

Real terrain art is not yet delivered; per DEC-002 the slice renders a clearly
identified placeholder terrain until the human (graphic designer) delivers and
the asset-processor validates it (DEC-006, ADR-002).

## Acceptance Criteria

- AC-1: Launching the game shows the playable-canvas scene from RAT-001 with
  the flat floor band/line removed: the only ground in the scene is a terrain
  body that spans the full width of the fixed 1280 x 720 world-unit playfield
  (R-4) and is drawn exactly where the collision mask reports solid terrain,
  running from its surface down to the playfield bottom. The RAT-001 flat
  floor reference is fully removed and plays no ground role anywhere (R-3).
- AC-2: The terrain visibly reads as the DEC-003 layered cross-section:
  scanning from the terrain surface downward, three visually distinct depth
  bands appear in a fixed order — surface (grass/earth), then rock, then dark
  interior — each band clearly distinguishable from its neighbor and reading
  darker the deeper it sits. The banding is legible along the whole playfield
  (over hills and dips alike), not only near one reference line, so that the
  craters of later slices expose these layers at any cut point.
- AC-3: The top of the terrain is a continuous, readable surface profile — the
  boundary between sky and terrain — with visible height variation along the
  playfield. It is not the straight horizontal line of the RAT-001 floor band,
  and it contains no overhangs or discontinuities. The geometry character is
  per R-1: deterministic procedural generation (fixed seed plus tunable,
  data-driven parameters) producing moderate hills with occasional flat spans;
  the profile keeps the surface at the rat's column near the RAT-001 staging
  line (~78% of the playfield height) so the composition stays similar.
- AC-4: The terrain geometry (surface profile and mask) is deterministic and
  stable: for a fixed configuration (same playfield definition, same seed and
  parameters — deterministic procedural generation per R-1) the exact same
  terrain is produced on every launch, and within a running session the
  geometry never changes
  (nothing in this slice alters the terrain after it is built).
- AC-5: A MonoGame-free collision-mask representation of the terrain exists in
  `src/Ratillery.Core` and can be constructed and queried by unit tests
  without a graphics window. It answers "solid (terrain) or empty (air)?" for
  any position inside the playfield; positions outside the playfield are
  defined as empty. It also reports, for any column, the terrain surface
  height — the top edge of the topmost solid cell in that column — which is
  the mechanism the rat's placement uses. Coordinates follow the existing Core
  convention (Y grows downward).
- AC-6: The mask and the rendering agree exactly for the whole session: the
  set of positions drawn as terrain is precisely the set of positions the mask
  reports as solid — including along the surface profile, at the playfield
  edges, and after any window resize. Rendering never paints terrain where the
  mask is empty and never leaves a solid position inside the visible playfield
  unpainted as terrain.
- AC-7: The mask represents one contiguous terrain body with no defects:
  in every column the solid cells form a single run starting at the surface
  and continuing down to the bottom of the playfield; there are no voids or
  empty cells inside the terrain body, no floating/disconnected solid cells,
  and nothing solid above the surface. Any shape requiring a deviation from
  this (holes, caverns, floating islands) would require mask mutation, which
  is out of scope for this slice.
- AC-8: The rat stands on the real terrain: keeping its RAT-001 bottom-center
  pivot, right-facing orientation and looping idle animation, the rat's pivot
  rests exactly on the terrain surface at its column — its vertical position
  equals the mask's surface height for that column. The spawn column is the
  horizontal center of the playfield (R-5), preserving the RAT-001
  composition.
  The rat neither floats (visible gap between feet and terrain) nor sinks
  (feet or body visibly inside the terrain), apart from any vertical motion
  inherent to the idle animation itself.
- AC-9: If the real terrain art of R-2 (Option A: one horizontally tileable
  texture per depth band) is missing or fails to load, the game must not crash
  and must not show geometry that contradicts the mask: a clearly identified
  placeholder terrain (per DEC-002 and the confirmed placeholder rules of
  R-2(e), with a visible marker) is rendered with the same surface profile and
  the same three ordered depth bands, and the mask and the rat placement
  behave exactly as they do with real art. Loading real art changes only the
  fill appearance, never the solidity.
- AC-10: `dotnet build` succeeds and `dotnet test` passes. New unit tests in
  `tests/Ratillery.Core.Tests` cover the mask behavior for a representative
  fixed configuration (solid/empty answers at representative points including
  the exact surface boundary rows and out-of-playfield positions, surface
  height per column, the contiguous-solid-run/no-holes property of AC-7, and
  identical results on repeated construction per AC-4). All pre-existing tests
  (rat state/damage, ballistics) remain green. Rendering is not unit-tested
  (project rule).

## Edge Cases

- Real terrain art missing entirely, or only some layer band textures missing
  or corrupt at load: covered by AC-9 — placeholder presentation, no crash;
  the mask and rat placement are unaffected because they never depend on art.
- Window resized while running (any dimension or aspect): the scene stays
  coherent — terrain is still painted only where the mask is solid, the rat
  remains standing on the surface at its column, and neither the rat nor the
  terrain disappears or clips out of the window. Exact strategy per R-4: the
  world/terrain/mask stay fixed at 1280 x 720 world units and the whole
  playfield is drawn uniformly scaled to fit the window (letterboxed if the
  aspect differs; no camera exists yet).
- Query at the exact surface boundary: the top edge of the topmost solid cell
  is the surface; a position at that y is solid, a position one step above is
  empty. The mask's answers are unambiguous at the boundary (AC-5/AC-7).
- Query at or beyond playfield edges: out-of-playfield positions are defined
  as empty and must not throw or return an undefined result (AC-5).
- Idle animation motion: the breathing loop may include subtle body motion;
  the placement rule is about the pivot resting on the mask surface height, so
  animation-inherent motion is not a floating/sinking defect (AC-8).
- Degenerate geometry: a configuration that would leave the rat's column
  without solid terrain, or push the surface off the playfield vertically,
  must be rejected/avoided at construction — the rat must always have solid
  terrain under its column and remain fully on screen (no camera).
- Placeholder → real-art swap later: swapping art must never change the mask
  or the rat placement, only the visual fill (AC-9); this is a design
  invariant, not a runtime event in this slice.
- Unexpected art dimensions or non-tileable content: the asset-processor
  verdict (ADR-002) is the gate; at runtime a delivery that cannot be
  processed or loaded falls back to the placeholder path (AC-9) instead of
  crashing.

## Asset Spec (R-2: Option A confirmed — art DELAYED to a post-slice follow-up)

Per R-2(a) the texture/layer model is confirmed: layered depth bands sampled
by depth below the local surface contour, with one horizontally tileable
texture per band; surface-band overhang above the geometric surface line is
allowed (R-2(c)). The real terrain art has **not** been delivered yet and is
**delayed to a post-slice follow-up** (R-2(d)) — it is NOT a gate for this
story. Until the human generates and delivers raw files under
`assets-source/` and the asset-processor validates them (verdict PROCESSED),
the **shipped presentation for this slice is the clearly identified
placeholder** per DEC-002 / R-2(e).

The table below is the machine-consumable delivery spec for the follow-up. All
values remain **proposed defaults for the human to confirm or adjust** when
the art is actually generated — they are not blocking this slice:

| Field | Proposed value (to confirm) |
| ----- | --------------------------- |
| Purpose | One horizontally tileable texture per depth band, sampled by depth below the local surface: surface cap, rock, dark interior |
| Source files (mirrored) | `assets-source/terrain/layers/surface.png`, `assets-source/terrain/layers/rock.png`, `assets-source/terrain/layers/interior.png` |
| Processed outputs | `assets/terrain/layers/<same names>.png` (+ metadata sidecar if applicable per ADR-002) |
| Format | RGBA PNG, full-bleed, intended opaque (alpha 255) band bodies |
| Dimensions (proposed) | e.g., 512 x 64 px each (width tileable horizontally; height ≥ band thickness at 1:1 playfield scale) |
| Layout | Single horizontal tile per band — NOT a strip of frames; no gutters/frame splitting |
| Tileability | Left/right edges wrap seamlessly (horizontal tiling); interior band may tile vertically or be drawn with a clamped darkest tone below the rock band (human picks) |
| Band order (top → bottom) | 1. surface grass/earth (proposed thickness ~24 playfield px just below the profile), 2. rock (proposed ~64 px), 3. dark interior (remaining depth to the playfield bottom) |
| Style / colors | Illustrated cartoon raster consistent with DEC-003; each band clearly distinct from its neighbor; interior darkest; grass/earth tones on the surface band |
| Transparency rules | Optional: only the top edge of the surface band may carry grass silhouettes in alpha (overhang above the geometric surface line allowed per R-2(c)); otherwise opaque |
| Fallback / placeholder (DEC-002) | Until delivered and PROCESSED: the same solid geometry drawn with flat neutral band colors + an on-screen "TERRAIN PLACEHOLDER" marker; mask and rat placement identical under both presentations |

Note: opaque full-bleed, horizontally tileable textures differ from the
transparent-margin sprite delivery spec in `assets-source/README.md`; the
asset-processor may need a texture/tile processing mode before such deliveries
can be validated (L2 tooling decision — see Dependencies). The human only
decides the delivery rules and drops the sources.

## Scope boundaries

### In scope

- A terrain body spanning the playfield with a clear surface profile and the
  DEC-003 depth-layered look (surface → rock → dark interior), replacing the
  RAT-001 flat staging floor.
- The static collision mask in `src/Ratillery.Core`: solid/empty queries,
  per-column surface-height query, deterministic construction, immutable
  during a session. MonoGame-free and unit-testable.
- New unit tests in `tests/Ratillery.Core.Tests` for the mask (AC-10).
- Rat placement on the terrain surface at the rat's column via the mask's
  surface-height query (no physics, no movement).
- Clearly identified placeholder terrain rendering until real art is delivered
  (DEC-002), including the fallback path when art is missing (AC-9).
- The RAT-001 scene, idle animation and debug HUD continue to work; the flat
  staging floor is removed.

### Out of scope

- Any destruction or mask mutation: craters, explosions, impact detection,
  carving, removal of texture or mask cells (milestone slices 6–8). Nothing in
  this slice changes the terrain after it is built.
- Physics/gravity on the rat, movement, input (beyond the existing Escape
  exit), aiming, firing, projectiles, turns, damage, wind, camera systems.
- Real terrain art production by agents (DEC-006 — human delivers, the
  asset-processor validates/processes); the asset-processor tool extension for
  tile textures, if needed, is L2 tooling work, not part of this story.
- Extra world dressing: props, debris, objects, caves/interior decoration,
  background scenery beyond the existing simple background, parallax.
- Level authoring tooling; persistence; networking.

### Core vs Game placement

Terrain geometry and the collision mask belong in `src/Ratillery.Core`
(MonoGame-free, unit-tested). Rendering (drawing the layered fill or the
placeholder over the solid region), terrain art loading, and scene composition
belong in `src/Ratillery.Game`. The Game reads the Core mask — e.g., the
surface height at the rat's column — to place the rat, but does not own terrain
state. Rendering is never unit-tested; the mask is.

## Dependencies

- RAT-001 (done): scene bootstrap, animated idle rat, bottom-center pivot,
  debug HUD, `assets/` content mirroring (ADR-003). Its flat floor reference
  is replaced by this slice's terrain.
- ADR-002 / ADR-003: terrain art follows the same two-tier raw/processed flow
  once delivered. Terrain textures (opaque, tileable, non-strip) may require
  the asset-processor to support a new texture/tile mode — an L2 engineering
  decision (potentially an ADR-002 amendment) that does not block this story:
  the placeholder path (DEC-002) covers the gap.
- DEC-002 (placeholders until human-provided assets), DEC-003 (visual
  direction: layered terrain cross-section, destructible-world asset family),
  DEC-006 (human delivers raw art; asset-processor validates).
- `docs/product/game-rules.md` (Terrain: destructible = texture + collision
  mask) and `docs/prompts/001-inicializacion-de-repo.md` (Terrain Texture +
  Collision Mask model) — this slice implements only the static
  representation.
- All open questions (OQ-1 … OQ-5) are resolved by the human and recorded as
  R-1 … R-5 below; this story is `status: ready`.

## Decisions

Established constraints, **not re-litigated**:

- Destructible terrain is modeled as terrain texture + collision mask; the
  mask is the authoritative solidity source future slices will carve
  (game-rules.md, product brief).
- The mask lives in `src/Ratillery.Core` (MonoGame-free, unit-testable); the
  rendered terrain lives in `src/Ratillery.Game` and must agree with the mask
  (AC-6).
- Nothing in this slice modifies the mask; impact/explosion/crater are slices
  6–8.
- No camera, movement, aiming, firing, input, or physics in this slice; the
  rat keeps its RAT-001 pivot/animation and is placed statically on the
  terrain surface.
- Coordinates follow the existing Core convention (Y grows downward).
- The surface profile is deterministic procedural generation (fixed seed plus
  tunable, data-driven parameters) — resolved as R-1; no authored/loaded
  heightmap asset in this slice.
- Placeholder terrain until human art delivery (DEC-002), with human visual
  sign-off as the final gate (DEC-006).

### Resolutions (R-n) — provided by the human (product owner)

Resolutions provided by the human (product owner); all open questions OQ-1 …
OQ-5 are closed. These decisions follow the style of RAT-001 and are binding
for the acceptance criteria above.

1. R-1 (terrain surface shape — was OQ-1): deterministic procedural generation
   (Option A, confirmed). The surface profile comes from a fixed seed plus
   tunable, data-driven parameters and is identical on every launch for the
   same configuration. Feel (A2, confirmed): moderate hills with occasional
   flat spans. The profile keeps the rat's feet near the RAT-001 staging line
   (~78% of playfield height) so the composition stays similar and the rat
   remains fully on screen (no camera).
2. R-2 (terrain art delivery and exact assets — was OQ-2):
   (a) Option A (confirmed): layered depth bands sampled by depth below the
   local surface contour (bands hug the surface): surface cap (proposed
   ~24 px) → rock (proposed ~64 px) → dark interior to the playfield bottom,
   with one horizontally tileable texture per band.
   (b) The exact raw deliverables are the machine-consumable Asset Spec table
   above — the three files `assets-source/terrain/layers/surface.png`,
   `assets-source/terrain/layers/rock.png`,
   `assets-source/terrain/layers/interior.png` plus their format/dimension/
   banding values. Those concrete values remain **proposed defaults for the
   human to confirm or adjust** when the art is actually generated; they are
   not blocking this slice.
   (c) Overhang allowed (confirmed): surface-band art (e.g., grass tufts) may
   extend above the geometric surface line; the rat's feet align to the
   geometric mask surface and tufts may pass behind/under the feet.
   (d) Art is DELAYED to a post-slice follow-up (confirmed): the clearly
   identified placeholder is acceptable for merge and visual sign-off; real
   terrain art is NOT a gate for this story.
   (e) Placeholder rules (confirmed): same solid geometry, flat neutral band
   colors, an on-screen "TERRAIN PLACEHOLDER" marker; mask and rat placement
   identical under both presentations.
   (f) L2 note (not a product decision): opaque tileable textures likely need
   a new asset-processor texture/tile mode; the asset-processor agent handles
   feasibility and verdicts.
3. R-3 (fate of the RAT-001 flat floor — was OQ-3): Option A (confirmed) — the
   flat floor reference is fully removed and replaced by the terrain body; the
   terrain is the only ground in the scene, even during the placeholder stage.
4. R-4 (world/playfield definition — was OQ-4): Option A (confirmed) — fixed
   logical playfield of 1280 x 720 world units (matching the current default
   backbuffer), independent of the window; the whole playfield is drawn
   uniformly scaled to fit the window (letterboxed if the aspect differs);
   resizing never changes the world, terrain, or mask; the terrain body runs
   from its surface down to the playfield bottom; the area outside the
   playfield is plain background only.
5. R-5 (rat spawn column and feet-rest verification — was OQ-5): Option A
   (confirmed) — the rat spawns at the horizontal center of the playfield,
   preserving the RAT-001 composition. Feet semantics confirmed: the
   bottom-center pivot equals the mask's surface height at the rat's column,
   with QA checking visually that there is no gap or penetration. Composition
   constraint: the profile keeps the rat's feet near the RAT-001 staging line
   (~78% of playfield height, per R-1).

## QA History

- (appended by the orchestrator/developer from QA reports)

### RAT-002 — QA cycle 1: PASS (2026-09-06)

QA verdict: **PASS** (commit `7218bd8`, branch `feat/RAT-002-2d-terrain-collision-mask`).
All 10 ACs HOLDS. Build 0 errors/0 warnings; `dotnet test` 27/27 green (incl. 19
new terrain-mask tests). Runtime smoke with pixel-probing evidence: flat floor
fully removed; terrain spans full 1280x720 playfield to bottom; three depth bands
(surface grass/earth 120,130,78 ~24px -> rock 104,94,86 ~64px -> interior
64,50,42) hug the surface over hills and dips; surface profile continuous and
non-flat (rows ~520..597) with the rat column anchored at row 562 = round(720*0.78);
cross-launch determinism confirmed (two launches bit-identical); rat feet at row 561
vs surface 562 (no gap/penetration) with idle animation running; letterboxed resize
(1400x600) keeps terrain confined to the playfield rect with pure-sky side bars and
the rat on the surface; "TERRAIN PLACEHOLDER" marker visible. No scope creep; Core
mask MonoGame-free and unit-tested; Game reads Core mask and owns no terrain state.
Non-blocking findings for later slices: FlatSpanChance not range-validated;
MaxDeviationPixels=0 untested; rat-head on-screen guaranteed for shipped config but
not arbitrary configs; Esc-exit verified by code inspection only.
