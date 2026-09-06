# ADR-002: Asset pipeline (raw source vs processed, repo tool)

Date: 2026-09-06
Status: accepted

## Context

Visual assets arrive from the human (or, later, an image-generation engine
over MCP) as AI-generated raster PNGs. Those raw files are not directly
consumable by MonoGame:

- Frames within a strip can touch or overlap (shared ground shadows, limbs),
  so frame boundaries are not on a clean pixel grid and must be derived from
  the alpha silhouette, not assumed by dividing width by an expected frame
  count.
- Each frame carries unused transparent margins that inflate texture size.
- Per-frame content may drift in position; drawing such frames as-is makes
  the character jitter.
- Animating code needs frame count/size and playback data as configuration,
  not hardcoded values (RAT-001 AC-4).

Art must also stay reproducible: agents must never re-interpret pixels at
runtime or hand-edit originals, and the transformation from raw asset to
final sheet must be deterministic and re-runnable.

## Decision

Two-tier asset layout plus a preprocessing tool kept in the repository.

- `assets-source/` holds human/generated deliverables only, never edited by
  agents. Naming is lowercase, mirrors `assets/` categories (e.g.
  `assets-source/rats/base/idle.png`).
- `assets/` holds only final, game-ready material produced by the pipeline.
  Directory names are lowercase (`assets/sprites/rats/base/`). A final sheet
  ships with a sidecar JSON metadata file of the same stem
  (`idle.png` + `idle.json`).
- `tools/Ratillery.AssetProcessor/` (C# console, net10.0, ImageSharp) is the
  pipeline. Commands:
  - `inspect` reports content bounds and the alpha-silhouette frame split for
    a raw strip.
  - `sheet` splits a horizontal strip into N frames, detects each frame's
    visible content via the alpha channel (ignoring pixels with alpha below a
    threshold), crops to a common cell size, centers horizontally, aligns all
    frames on a shared feet baseline at the cell bottom, applies a transparent
    padding margin, and writes the normalized horizontal sheet plus metadata
    JSON (name, frameCount, frameWidth, frameHeight, fps, loop, pivot) and,
    optionally, the individual frames.
  - No scaling or deformation is ever applied.

Metadata is machine-consumable so the future image-generation engine can
write to `assets-source/` and the same pipeline converts it.

## Consequences

- Originals are preserved untouched under `assets-source/`; regenerating a
  cleaned sheet is a single re-runnable command.
- The game consumes only normalized sheets + metadata, so swapping assets is
  config/data-only (RAT-001 AC-4/AC-6).
- The first processed asset is `assets/sprites/rats/base/idle.png` (8 frames,
  273x265 cells, sheet 2184x265) from `assets-source/rats/base/idle.png`
  (2680x682 RGBA). A first AI sheet with touching/overlapping frames (no
  transparent gutters) could not be cropped cleanly — neighbor tails crossed
  the frame boundaries — so the human re-sourced it; delivery rules are in
  `assets-source/README.md`.
- Existing `assets/` placeholders created at initialization with PascalCase
  names were renamed to lowercase; no code referenced the old paths.
- Processed output is deterministic but not artistic QA: the human still
  visually approves frames (e.g. halos, boundaries cutting limbs) before they
  ship.
