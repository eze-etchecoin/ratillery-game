# TOOL-002: Swap the DEC-002 placeholder terrain fill for the delivered real art

status: done

## Story

As a player,
I want the terrain to be filled with the real, signed-off art — grass/earth
surface cap hugging the surface contour, a rock band beneath it, and a
world-anchored dark interior down to the playfield bottom — instead of the flat
DEC-002 placeholder band colors,
so that the ground looks like the actual destructible world the human
graphic-designer drew and signed off (DEC-006), while the collision mask and the
rat's placement remain exactly as they were.

RAT-002 shipped the terrain with a clearly identified placeholder fill
(`PlaceholderTerrain` in `src/Ratillery.Game/Rendering/`) because the real art
was delayed (R-2(d)). That art is now delivered, validated and mirrored by
TOOL-001's `tile` command, and visually signed off by the human (DEC-006): three
RGBA PNGs plus their metadata sidecars at
`assets/terrain/layers/{surface-cap,rock,interior}.png` + `.json`.

This story **only** swaps the visual fill. It is the RAT-002 follow-up promised
in RAT-002's asset spec and in DEC-007. Everything about geometry is invariant:
the Core collision mask is untouched and still reports exactly which cells are
solid; the surface profile is unchanged; the rat's pivot still rests on the mask
surface height at its column; determinism is unchanged. The three real textures
replace the three flat placeholder band fills, sampling them over exactly the
solid cells the mask reports.

The binding delivery model (DEC-007; refined in TOOL-001) fixes the art and its
role:

- **`surface-cap.png`** (512x32, RGBA): thin cap hugging the terrain contour.
  Its **earth-top row** (sidecar `earthTopRow` = 11) is aligned to the mask
  surface height H(x) at each column, so the opaque earth body (rows 11..31,
  `bodyHeight` = 21 px) fills `[H(x), H(x)+21)` and any grass-tuft pixels in the
  transparent headroom rows *above* earth-top appear above H(x) in the air
  (R-2(c) overhang). Horizontally seamless only.
- **`rock.png`** (512x64, RGBA opaque): the rock band immediately below the cap.
  Height 64 = its full texture height, mapped 1:1 with no vertical repeat, from
  `H(x)+21` to `H(x)+85`. Horizontally seamless only.
- **`interior.png`** (64x64, RGBA opaque): the deep bulk from below the rock band
  (`H(x)+85`) down to the playfield bottom, variable depth per column, sampled
  with **world-anchored vertical tiling** so the vertical phase is aligned across
  columns (a fixed world Y samples the same source row in every column);
  horizontally and vertically seamless.

Band thicknesses are data-driven from the delivered art (cap `bodyHeight`,
rock full texture height), and the corresponding `TerrainConfig` band values
are matched to them (1:1 px per DEC-007) so real-art and fallback rendering agree
on where the bands sit. The DEC-002 placeholder rendering is **kept** as the
fallback for when real art is unavailable (RAT-002 AC-9): missing/unreadable
tiles must never crash the game or contradict the mask.

## Acceptance Criteria

- AC-1 (**real art renders when present**): When `surface-cap.png`, `rock.png`,
  and `interior.png` and their sidecars all load successfully under
  `assets/terrain/layers/`, launching the game draws the terrain fill by sampling
  those real textures instead of the flat placeholder band colors. The terrain
  still spans the full 1280x720 playfield, runs from its surface down to the
  playfield bottom, and reads as the DEC-003 cross-section top-to-bottom:
  surface cap (grass/earth) → rock → dark interior, the three bands legible over
  the whole surface profile (hills and dips), not just near one reference line.
  The on-screen "TERRAIN PLACEHOLDER" marker is **not** shown when real art is
  present.
- AC-2 (**surface-cap earth-top alignment and overhang**): At each column the
  surface-cap texture is aligned so its earth-top row (from the sidecar,
  `earthTopRow` = 11) coincides with the mask surface height H(x). The opaque
  cap body — rows at/under earth-top, `bodyHeight` = 21 px — fills
  `[H(x), H(x)+21)`. Only transparent headroom/tuft pixels of the cap that exist
  in the texture's rows above earth-top may extend above H(x) into the air
  (R-2(c) overhang); no opaque terrain geometry is painted above H(x). The
  earth-top row and body height come from the cap's sidecar at load, not from a
  hard-coded constant.
- AC-3 (**rock band immediately below the cap**): The rock band begins at the
  row right after the cap body, `H(x)+21`, and maps its full 64-px texture 1:1
  (no vertical repeat) down to `H(x)+85`, clipped to the playfield bottom and to
  solid cells. The rock band thickness is its full texture height (data-driven
  from the art), not a new hard-coded value.
- AC-4 (**interior world-anchored vertical tiling**): The interior fills from
  below the rock band, `H(x)+85`, down to the playfield bottom (variable depth
  per column), sampled from the 64x64 seamless texture using a fixed world
  vertical anchor so the vertical phase is aligned across columns — a given world
  row maps to the same interior source row regardless of column — and horizontal
  sampling wraps seamlessly across the full width. The interior never extends
  above `H(x)+85` and never outside the mask-solid region.
- AC-5 (**mask agreement is invariant — RAT-002 AC-6 preserved**): The set of
  positions drawn with opaque real art (cap body + rock + interior) is precisely
  the set of positions the Core mask reports solid. Rendering never paints opaque
  terrain where the mask is empty (the only pixels above H(x) are transparent
  cap tufts per AC-2) and never leaves a solid position inside the visible
  playfield unpainted as terrain. The surface profile and the solidity are
  unchanged. The rat's placement is unchanged: its pivot still equals the mask
  surface height at its (center) column, with no gap or penetration beyond idle
  animation motion.
- AC-6 (**determinism unchanged**): The terrain geometry and collision mask are
  produced exactly as before (same config, same seed) and never change during a
  session; swapping the fill does not alter geometry or introduce any
  nondeterminism. Band-thickness matching (AC-2/AC-3) does not change the mask:
  the mask is solid from H(x) to the playfield bottom regardless of where the
  visual bands fall.
- AC-7 (**config band values match the art**): The `TerrainConfig` depth-band
  values used by both real-art and fallback rendering match the delivered art at
  1:1 px — `SurfaceBandPixels` equals the cap's `bodyHeight` (21) and
  `RockBandPixels` equals the rock texture's full height (64) — so the real-art
  rock band and the fallback rock band begin at the same row below the surface
  and neither rendering shows a gap or overlap between adjacent bands.
- AC-8 (**missing/corrupt art falls back, never crashes**): If any of the three
  terrain tiles (PNG) or its metadata sidecar is missing, unreadable, fails to
  load/parse, or lacks the field the renderer needs for its role (e.g. the cap's
  `earthTopRow`/`bodyHeight`), the game must not crash and must not show geometry
  that contradicts the mask. It renders the DEC-002 placeholder presentation —
  the existing clearly-identified placeholder fill with its marker and the same
  three ordered depth bands — with the same surface profile and solidity, and
  the rat placement unchanged. When any tile or sidecar is unavailable, the
  terrain falls back as a whole to the placeholder presentation rather than
  mixing partial real art with partial flat fills. Loading real art changes only
  the fill appearance, never the solidity.
- AC-9 (**unit tests stay in Core; rendering not unit-tested**): `dotnet build`
  succeeds and `dotnet test` passes. Any render-independent, MonoGame-free logic
  this story introduces (e.g. deriving per-column band row ranges
  `[H(x), H(x)+cap]`, `[H(x)+cap, H(x)+cap+rock]` from a surface height and
  data-driven band thicknesses, or computing the interior world-anchored phase
  alignment) lives where `tests/Ratillery.Core.Tests` can exercise it without a
  graphics window, following the Core vs Game placement rule, and is covered by
  new unit tests there (representative surface heights including the clipped
  bottom/`H(x)+85 >= playfield bottom` cases and boundary rows). Existing
  tests (rat, mask, ballistics) remain green. Texture loading, sidecar parsing
  that feeds rendering, and actual drawing remain in the Game layer and are not
  unit-tested (project rule).

## Edge Cases

- **One or two tiles load but not all**: per AC-8 the terrain falls back as a
  whole to the placeholder presentation; no partial real/placeholder mix and no
  crash.
- **Sidecar present but missing a required field** (e.g. cap without
  `earthTopRow`/`bodyHeight`): treated as the tile being unavailable (AC-8);
  the renderer must not guess a fallback earth-top.
- **Surface so deep that `H(x) + cap + rock` reaches or exceeds the playfield
  bottom** (e.g. a low-lying column): the interior region for that column is
  empty or truncated; rock is clipped at the playfield bottom; no out-of-range
  sampling, no crash, and nothing painted above the surface. (Shipped default
  keeps H(x) near 78% of height, but the clipped path must be handled and
  tested.)
- **Cap tuft overhang on a hill or over a dip**: transparent tuft pixels above
  the earth-top row draw above H(x) only where the cap texture actually has
  them; they must read as grass overhang (R-2(c)), never as solid geometry that
  would contradict AC-5, and must not be mistaken for the rat resting higher.
- **Horizontal wrap of non-integer multiples**: the world is 1280 wide while the
  cap/rock textures are 512 (2.5 wraps) and interior 64 (20 wraps); sampling
  must wrap seamlessly with no visible seam (TOOL-001 validated seamlessness).
- **World-anchored interior phase**: because interior is anchored to a fixed
  world row and each column's interior starts at its own `H(x)+85`, the source
  row at a column's interior top varies by column; the fill stays continuous
  because the phase is a function of world Y alone (AC-4).
- **Rat overlapping cap tufts**: per R-2(c)/DEC-007, tufts may pass behind/under
  the rat's feet; the rat's pivot stays on H(x) (AC-5), so overlap is expected
  decoration, not a floating/sinking defect.
- **Window resized / letterboxed**: unchanged behavior — the world stays fixed at
  1280x720 and is uniformly scaled; the art swap is purely a fill change inside
  that world.
- **Blending**: straight-alpha art (tufts with partial alpha) must be drawn with
  a non-premultiplied blend, consistent with ADR-003's existing sprite drawing,
  so tufts and the opaque body composite correctly over the sky/terrain.

## Out of scope

- Any change to the collision mask, terrain geometry, surface profile, rat,
  ballistics, aiming, firing, projectiles, input, turns, damage, wind, camera,
  or debug HUD beyond the placeholder-marker removal when real art is present.
- Any editing, cropping, scaling, color/alpha transforming, or regeneration of
  art; any re-processing of `assets/` or `assets-source/`; the art is used as
  delivered and signed off.
- Milestone slices 3–8 and any destruction/cratering/impact behavior (the mask
  stays immutable here; nothing carves it).
- Level authoring, terrain design tooling, UI, background scenery, props.
- Changing the `tile` command, TOOL-001 sidecar schema, ADR-002, or the asset
  pipeline.

## Scope boundaries

### In scope

- Loading the three processed terrain tiles and their sidecars from
  `assets/terrain/layers/` at runtime using the raw-texture load approach
  ADR-003 established (mirrored `assets/` → output `Content/`, raw PNG load plus
  JSON sidecar parsing in the Game layer).
- Replacing the `PlaceholderTerrain` flat fill with a real-art renderer that
  draws each band by sampling its texture over exactly the mask-solid cells,
  per AC-1..AC-6: cap earth-top aligned to H(x) with tuft overhang, rock 1:1
  below the cap, world-anchored interior to the bottom.
- Keeping the existing DEC-002 placeholder rendering (and its marker) as the
  fallback for unavailable art (AC-8), and suppressing the placeholder marker
  when real art is present (AC-1).
- Matching the `TerrainConfig` band values to the delivered art (AC-7) and any
  band-thickness/mapping reads that come from the sidecar/texture data rather
  than hard-coded constants.
- New unit tests in `tests/Ratillery.Core.Tests` for any MonoGame-free mapping
  logic introduced (AC-9).

### Core vs Game placement

Terrain geometry, the collision mask, surface height, and the data-driven band
configuration (which describe fill appearance only, not solidity) belong in
`src/Ratillery.Core` (MonoGame-free, unit-tested). Texture loading, sidecar
parsing used for rendering, sampling/sprite drawing, and the decision of which
presentation (real art vs placeholder) to draw belong in `src/Ratillery.Game`.
The Game reads the Core mask surface height to align the bands but owns no
terrain state. Rendering is never unit-tested; any render-independent mapping
logic is Core-side and unit-tested (AC-9).

## Dependencies

- RAT-002 (done): delivered the placeholder terrain and the invariant this swap
  preserves — the real art changes only the fill, never the mask or rat
  placement (AC-9 of RAT-002).
- TOOL-001 (done): produced and validated `assets/terrain/layers/{surface-cap,
  rock,interior}.png` + sidecars (`surface-cap.json` carries `earthTopRow`/
  `bodyHeight`; rock/interior carry width/height/seam fields). ADR-003 already
  mirrors `assets/` into the output `Content/`, so the tiles reach the game at
  build time.
- DEC-007 (binding delivery model: earth-top alignment to H(x), cap + rock
  depth-below-surface, interior world-anchored vertical tiling), DEC-002
  (placeholder fallback), DEC-006 (human visual sign-off), ADR-002 / ADR-003
  (asset flow and raw load path).
- `TerrainConfig` (`SurfaceBandPixels`, `RockBandPixels`) and `TerrainMask`
  (`SurfaceHeight`) in `src/Ratillery.Core` as the source of surface height and
  band configuration.

## Open Questions

- None. (DEC-007 and TOOL-001 fix the art facts and delivery model; ADR-003
  fixes the load path; the remaining choices — exact interior world-anchor row,
  how the loader is organized, mixing-vs-whole fallback on partial tile loss —
  are non-gameplay implementation details for the developer.)

## QA History

- (appended by the orchestrator/developer from QA reports)

### TOOL-002 — QA cycle 1: PASS (2026-09-06)

QA verdict: **PASS** (branch `feat/TOOL-002-real-terrain-art-swap`, uncommitted
working tree). All AC-1..AC-9 HOLDS. `dotnet build` 0 warnings/0 errors;
`dotnet test` 74/74 green (Core 40 incl. 13 new `TerrainBandsTests` + tool 34);
runtime smoke: game started and ran ~12s without crashing or stderr.
Cap earth-top (sidecar 11) aligned to H(x) with `bodyHeight` 21 (data-driven,
no hard-coded constant); rock 1:1 full 64px below cap clipped at the playfield
bottom; interior world-anchored vertical tiling (source row = world row %
height, phase aligned across columns; horizontal wrap % width); rock/interior
spans derived via shared Core `TerrainBands`; mask-agreement invariant
(RAT-002 AC-6) preserved — cap/rock/interior contiguous within the solid run
[H(x), bottom), only cap transparent headroom above H(x); rat placement
unchanged; determinism unchanged (`SurfaceBandPixels` 24→21 is fill-only, not
used in surface generation); whole-presentation fallback to placeholder +
marker on any missing/corrupt tile/sidecar (TryLoad returns null, never throws,
no partial mix); marker suppressed when real art renders. Scope clean: no
changes to `TerrainMask`, rat, ballistics, aiming, input; RAT-001/RAT-002
stories and `assets/`/`assets-source/` untouched. Non-blocking observations:
possible `Texture2D` leak on the invalid-cap early `return null` in `RealTerrain.TryLoad`;
cap strip not bottom-clamped for a hypothetical surface H>699 (unreachable in
the shipped config); final in-game human visual sign-off (DEC-006) still pending.
