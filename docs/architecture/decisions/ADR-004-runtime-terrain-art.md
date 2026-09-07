# ADR-004: Runtime terrain art presentation (real DEC-007 tiles vs placeholder)

Date: 2026-09-06
Status: accepted

## Context

RAT-002 shipped the terrain fill as a flat DEC-002 placeholder
(`PlaceholderTerrain`) because the real layer art was delayed (R-2(d)). TOOL-001
then delivered and validated three signed-off RGBA tiles plus sidecars at
`assets/terrain/layers/` (`surface-cap.png` 512x32, sidecar earthTopRow 11 /
bodyHeight 21; `rock.png` 512x64; `interior.png` 64x64 seamless H+V). DEC-007
fixes the sampling model: the cap is contour-hugging with its earth-top row
aligned to the mask surface H(x) (transparent tuft overhang above allowed,
R-2(c)); rock sits 1:1 below the cap with no vertical repeat; interior is
world-anchored vertical tiling to the playfield bottom. TOOL-002 swaps only the
visual fill — geometry, solidity, determinism, and rat placement are invariant
(RAT-002 AC-5/AC-6/AC-8/AC-9).

Three implementation details were left open for the developer: the exact
interior world-anchor row, how the loader is organized, and how partial tile
loss falls back. These are L2 architecture choices, recorded here.

## Decision

A MonoGame-free mapping and two Game-layer presentations.

### Core (MonoGame-free, unit-tested): `TerrainBands`

`src/Ratillery.Core/Terrain/TerrainBands.cs` is a pure helper mapping a column's
mask surface height and data-driven band thicknesses to the three vertical band
spans `[H, H+cap)`, `[H+cap, H+cap+rock)`, `[H+cap+rock, bottom)`, all clamped
to the playfield bottom, plus `InteriorSourceRow(worldRow, height)` = the
world-anchored interior source row. `TerrainConfig.SurfaceBandPixels` default
changes 24 → 21 so the fallback config matches the delivered cap body 1:1
(DEC-007 / AC-7). Both presentations derive band rows from this single tested
helper, so real-art and placeholder banding can never drift apart.

### Game renderers

- `Rendering/RealTerrain.cs` loads the three PNGs (`Texture2D.FromFile`, ADR-003)
  and their sidecars (`TerrainTileSidecar`, System.Text.Json) from the mirrored
  output `Content/terrain/layers/`. Cap earth-top and body height come from the
  sidecar; rock band thickness is the rock texture's full height; interior
  period is the interior texture's height. It draws per mask column: the cap as
  a full strip (headroom + body) with its earth-top row on H(x), rock 1:1 below,
  and interior as a per-column loop that repeats the texture every interior
  height with a source row equal to the world row modulo that height. The batch
  uses `BlendState.NonPremultiplied` and `PointClamp` (unchanged), so straight
  alpha tufts composite correctly over the sky.
- `Rendering/PlaceholderTerrain.cs` keeps the DEC-002 flat presentation
  (unchanged look, marker kept) but now derives its three bands through the
  shared `TerrainBands` helper. It is the fallback.
- `Scenes/MainScene.cs` attempts `RealTerrain.TryLoad`; when it returns non-null
  it draws the real renderer and suppresses the "TERRAIN PLACEHOLDER" marker;
  otherwise it draws the placeholder with the marker (AC-1/AC-8). The mask and
  rat placement are untouched and identical under both presentations.

### Interior world anchor

The interior source row is `worldRow % interiorHeight`, anchored to the world
origin (playfield row 0). Because the phase is a function of world Y alone, a
fixed world row samples the same interior source row in every column (AC-4),
and the horizontal wrap is the same `column % textureWidth` sampling used by the
other bands. The 64x64 interior texture is seamless on both axes (validated by
TOOL-001), so both the vertical repeat and the 20 horizontal wraps across the
1280-wide playfield are continuous.

### Loader organization and whole-presentation fallback

Loading is encapsulated in `RealTerrain.TryLoad`, which returns null (never
throws) when any of the three tiles or sidecars is missing, unreadable,
malformed, or lacks the field its role requires (e.g. the cap's
`earthTopRow`/`bodyHeight`). A null result makes `MainScene` fall back as a
whole to the placeholder presentation — never a partial mix of real art and flat
fills (AC-8). Loading changes only the fill appearance, never the mask.

## Consequences

- Real art renders over exactly the mask-solid cells; the only pixels above
  H(x) are the cap's transparent grass-tuft headroom (overhang, R-2(c)), which
  never reads as solid terrain (AC-2/AC-5).
- Determinism and solidity are unchanged: neither renderer mutates the mask; the
  swap is appearance-only (AC-6).
- Band-thickness values are data-driven from the sidecars/textures and the
  `TerrainConfig` defaults match them 1:1, so real-art and fallback banding agree
  (AC-7).
- Any future art respec (different dimensions/earth-top) needs only reprocessing
  through the pipeline (ADR-002) plus a `TerrainConfig` default touch; the
  runtime reads cap alignment from the sidecar, not a constant (AC-2).
- The renderer is intentionally per-column (a few thousand small textured
  draws/frame), consistent with the pre-existing per-column placeholder approach
  and acceptable at 1280x720.
