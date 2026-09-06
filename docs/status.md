# Ratillery — Status & Artifact Index

Single source of truth for development status. Maintained by the
orchestrator; every agent artifact must be registered here.

Last updated: 2026-09-06 (TOOL-001 merged to main; terrain art delivery pending processing)

## Milestone

First playable: **"A rat can destroy the world."**

Progress: 2 / 8 slices

| # | Slice | Story | Status |
| - | ----- | ----- | ------ |
| 1 | Animated rat (Idle) | RAT-001 | done |
| 2 | 2D terrain + collision mask | RAT-002 | done |
| 3 | Aiming | — | pending |
| 4 | Firing + projectile | — | pending |
| 5 | Ballistic trajectory | — | pending |
| 6 | Impact detection | — | pending |
| 7 | Explosion | — | pending |
| 8 | Crater / terrain destruction | — | pending |

## Stories

| ID | Title | Status | QA | File |
| -- | ----- | ------ | -- | ---- |
| RAT-001 | Animated rat (Idle) | done | PASS | `docs/stories/RAT-001-animated-rat-idle.md` |
| RAT-002 | 2D terrain + collision mask | done | PASS | `docs/stories/RAT-002-2d-terrain-collision-mask.md` |
| TOOL-001 | Asset-processor tile mode (terrain layer validation + mirror) | done | PASS | `docs/stories/TOOL-001-asset-processor-tile-mode.md` |

Status: `draft` -> `ready` -> `in-progress` (feature branch
`feat/<ID>-<slug>`) -> `done` (merged to `main` after DEV_APPROVED + QA PASS +
ANALYST_APPROVED). QA column records the last verdict and cycle count
(e.g. `FAIL 2/3`, `PASS`).

## Bugs

| ID | Title | Story | Status | File |
| -- | ----- | ----- | ------ | ---- |
| — | — | — | — | — |

Bugs are `BUG-###` files in `docs/bugs/` (same template as stories, plus
reproduction steps). Status: `open` -> `in-progress` -> `fixed` -> `verified`.

## Architecture decisions

| ID | Title | File |
| -- | ----- | ---- |
| ADR-001 | Initialization decisions (net10.0, DesktopGL, content pipeline) | `docs/architecture/decisions/` |
| ADR-002 | Asset pipeline: `assets-source/` raw vs `assets/` processed + `tools/Ratillery.AssetProcessor` | `docs/architecture/decisions/ADR-002-asset-pipeline.md` |
| ADR-003 | Runtime sprite content delivery: `assets/` mirrored into output `Content/`, raw texture load (no XNB for sprites) | `docs/architecture/decisions/ADR-003-runtime-sprite-content.md` |

## Product decisions

| ID | Decision | Author | Recorded in |
| -- | -------- | ------ | ----------- |
| DEC-001 | First milestone: "A rat can destroy the world" (8 slices above) | human | `docs/product/vision.md` |
| DEC-002 | Placeholder assets until human provides sprites/audio | human | `AGENTS.md` |
| DEC-003 | Visual direction: 2D side-view stylized cartoon; small big-headed rats; oversized weapons; visible terrain destruction; illustrated raster sprites (not pixel art); modular sprite sheet (idle/walk/aim/fire/hit/airborne/land/death); teams differentiated by accessories + secondary color. Human acts as graphic designer, provides assets on demand. | human | `temp/02-conversacion-chatgpt-idea-inicial.md` |
| DEC-004 | Human roles: product owner + graphic designer/asset provider. Temporary: to be replaced by an image-generation engine via MCP; asset specs stay machine-consumable. | human | `AGENTS.md` (Human Roles) |
| DEC-005 | Lowercase asset layout: raw files under `assets-source/`, game-ready files under `assets/`; preprocessing pipeline lives in-repo as `tools/Ratillery.AssetProcessor`. | human | ADR-002 |
| DEC-006 | The human stays apart from the asset pipeline: sources are dropped into `assets-source/` (mirrors `assets/` 1:1) and the `asset-processor` agent validates (deterministic verdict) + normalizes them; agents never generate or edit art. | human | AGENTS.md / ADR-002 |
| DEC-007 | Terrain real-art delivery model (refines R-2 proposed defaults): 3 visual depth zones kept — surface cap → rock → dark interior. Delivery is 3 RGBA PNGs at `assets-source/terrain/layers/`: `surface-cap.png` (thin cap hugging the contour; horizontally seamless only; the file carries transparent headroom rows above its **earth-top row** — the first row that is opaque full-width, which the engine aligns to the geometric surface line H(x), R-2(c) overhang); grass tufts live in alpha above the earth-top; the solid earth body below it has height = visible band thickness; `rock.png` (rock band, height 64px = RockBandPixels, horizontally seamless only, 1:1 vertical mapping, no vertical repeat); `interior.png` (deep-bulk tile, seamless BOTH horizontally and vertically, ~64x64, engine repeats it down to the playfield bottom with a fixed world anchor so the phase aligns across columns). Sampling semantics: cap + rock are depth-below-local-surface (contour-hugging) per column; interior is world-anchored vertical tiling. Band thicknesses stay configurable (`TerrainConfig.SurfaceBandPixels` / `RockBandPixels`) and will be matched to the delivered art (1:1 px) at implementation. | human | this file / RAT-002 Asset Spec |

## Open follow-ups

Handoff notes for the next working session.

| Item | Origin | Action needed | Reference |
| ---- | ------ | ------------- | --------- |
| Terrain real art (3 zones, DEC-007) | RAT-002, R-2(d) — art DELIVERED, pending processing | Human delivered 3 RGBA PNGs at `assets-source/terrain/layers/` (`surface-cap.png`, `rock.png`, `interior.png`; committed on main). Next: asset-processor validates/processes them with the new `tile` command (TOOL-001); then a dev story swaps the placeholder fill to real art; human visual sign-off (DEC-006). | DEC-007 below / TOOL-001 |
| Terrain feel tune (optional) | RAT-002 shipped default (seed 20260906) | If the human wants more/less relief, adjust `TerrainConfig` defaults (L1). | `src/Ratillery.Core/Terrain/TerrainConfig.cs` |
| Slice 3: Aiming | Milestone backlog | Needs a PRODUCT_REQUEST + product decisions from the human (input scheme, reticle/aim UI, angle/power UX) before the functional-analyst drafts the story. | `docs/status.md` milestone table |

## Session log

Chronological summary per dev cycle. One line per story/bug resolution.

| Date | Artifact | Outcome |
| ---- | -------- | ------- |
| 2026-09-06 | — | Repo + agent graph initialized |
| 2026-09-06 | DEC-003 | Visual direction approved by human; slice 1 story requested |
| 2026-09-06 | ADR-002 / DEC-005 | Asset pipeline built; rat idle processed to `assets/sprites/rats/base/idle.png` (8 frames, 273x265) + `idle.json` + `frames/` |
| 2026-09-06 | RAT-001 asset | QA found gutter-less source: tails crossed frame boundaries (frame 0 showed neighbor's tail tip; frames 1-7 had sectioned tails). Human re-sourced a clean sheet (2680x682, 8x335 cells with gutters) and it reprocessed correctly; generic delivery spec added at `assets-source/README.md`. |
| 2026-09-06 | DEC-006 / asset-processor | Added `validate` subcommand (deterministic JSON verdict) + `asset-processor` agent & `process-asset` skill; assets-source mirrors assets 1:1; docs updated (AGENTS, ADR-002, graph.md). |
| 2026-09-06 | RAT-001 | Story finalized `draft` → `ready`. Human resolved OQ-1 (idle asset PROCESSED: 8 frames 273x265 @ 8 fps, loop, pivot bottom-center 0.5/1.0), OQ-2 (placeholder = frame 0, fallback only), scope Option B (full 8-frame looping idle animation in scope), and stage decision (flat non-destructible floor reference; destructible terrain stays slice 2). |
| 2026-09-06 | RAT-001 | Developer implemented on `feat/RAT-001-animated-rat-idle`: data-driven looping idle animation (all params from `idle.json`), flat floor staging, frame-0 fallback, time-based playback; ADR-003 records the sprite content delivery decision. `dotnet build` + `dotnet test` green; awaiting QA. |
| 2026-09-06 | RAT-001 | QA PASS (10/10 AC, build/test green, runtime smoke ok, no scope creep). ANALYST_APPROVED (scope conformance ok). Merge gate 3/3 complete; pending human visual sign-off per AC-1/DEC-006 before merge to main. |
| 2026-09-06 | RAT-001 | Human visual sign-off granted. Merged to main (--no-ff), story `done`, slice 1/8 complete, feature branch deleted. |
| 2026-09-06 | RAT-002 | Slice 2 requested by human ("continuar con la generación del terreno"). Functional-analyst drafted story `RAT-002-2d-terrain-collision-mask.md` (status `draft`): rendered terrain (DEC-003 layers) + static Core collision mask, rat on real surface, placeholder per DEC-002. 5 open questions (OQ-1..5) raised for human (procedural vs fixed shape; art model + exact assets; fate of RAT-001 flat floor; playfield definition; rat spawn column). Art delivery pending OQ-2 resolution. |
| 2026-09-06 | RAT-002 | Human resolved OQ-1..5: procedural deterministic terrain (A2 moderate hills + flat spans, rat feet near ~78% line); art model Option A (one tileable texture per depth band, overhang allowed), art DELAYED to post-slice follow-up — placeholder is shipped presentation; flat floor fully removed; fixed 1280x720 playfield (letterboxed, resize-independent); rat spawns at horizontal center, pivot = mask surface height. Analyst finalized story to `status: ready` (Resolutions R-1..R-5 recorded). Ready for developer. |
| 2026-09-06 | RAT-002 | Developer implemented on `feat/RAT-002-2d-terrain-collision-mask` (commit 7218bd8): `TerrainConfig` + `TerrainMask` in Core (deterministic procedural profile, solid/empty + per-column surface height, config validation), `PlaceholderTerrain` + MainScene rework in Game (fixed 1280x720 playfield letterboxed, flat floor removed, rat pivot on mask surface at center column, placeholder marker). `dotnet build` 0 warnings/errors, `dotnet test` 27/27 green (19 new terrain-mask tests). DEV_APPROVED; story in-progress awaiting QA. |
| 2026-09-06 | RAT-002 | QA PASS (1/1): all 10 ACs hold; build/test green; runtime pixel-probe evidence (terrain to bottom, 3 ordered bands darker with depth over hills/dips, non-flat continuous profile anchored at 78% row 562, cross-launch determinism, rat feet on surface no gap/sink, letterboxed resize OK, placeholder marker). No scope creep. Non-blocking notes recorded for later slices. Awaiting ANALYST_APPROVED + human merge consent. |
| 2026-09-06 | RAT-002 | ANALYST_APPROVED (scope conformance): diff implements exactly AC-1..AC-10 + R-1..R-5, no invented rules/scope, no AC rewritten. Merge gate 3/3 (DEV_APPROVED + QA PASS + ANALYST_APPROVED). Human granted merge consent + push. Merged to main (--no-ff), story `done`, slice 2/8 complete, feature branch deleted, pushed to origin. |
| 2026-09-06 | — | Human closed the working day after RAT-002. "Open follow-ups" section added to this file: terrain art delivery pending (R-2(d)), optional TerrainConfig feel tune, and Slice 3 (Aiming) as next backlog item. Status docs change left uncommitted on main (orchestrator commits only on explicit request). |
| 2026-09-06 | DEC-007 | Human (asset provider) asked how a horizontal rectangle/strip terrain texture would work against the procedural irregular terrain; orchestrator explained the per-column contour-hugging band model (depth-below-surface sampling) + interior bulk. Human proposed splitting the cap from the deep bulk for easy vertical tiling; confirmed **3 zones** (surface cap → rock → dark interior) with the revised file set `surface-cap.png` / `rock.png` / `interior.png` (roles + tileability per DEC-007). Recorded as DEC-007 and reflected in the Open follow-ups row. Human to generate the art next; asset-processor validates on delivery. Status docs change left uncommitted on main. |
| 2026-09-06 | DEC-007 (correction) | While scoping tile validation, orchestrator flagged that DEC-007's surface-cap wording ("top file edge = surface line") contradicted the confirmed grass-tuft overhang (R-2(c)); human approved the corrected model: the surface-cap file carries transparent headroom rows above its **earth-top row** (first fully opaque full-width row), which the engine will align to H(x). DEC-007 wording, `temp/terrain-art-spec.md`, and the delivery notes updated. Status docs change left uncommitted on main. |
| 2026-09-06 | TOOL-001 | Human asked whether the sprite-only AssetProcessor needed adjusting before the terrain-tile delivery; orchestrator confirmed yes (current `validate` would mis-reject opaque full-bleed tiles) and human approved starting the L2 tooling. functional-analyst drafted story `TOOL-001-asset-processor-tile-mode.md` (status `ready`, no open questions): new deterministic `tile` command (per-kind DEC-007 validation + byte-copy mirror + metadata sidecar), new tool test project, docs updates (README terrain section, process-asset skill, ADR-002 amendment). A first human delivery `assets-source/terrain/layers/surface-cap.png` is present (untouched per DEC-006). Story `ready` -> handing to developer on `feat/TOOL-001-asset-processor-tile-mode`. |
| 2026-09-06 | TOOL-001 | Two developer runs returned empty (suspected model issue); human switched the default model and the retry succeeded. Developer implemented on `feat/TOOL-001-asset-processor-tile-mode` (uncommitted working tree): `tile` command with per-kind validation (opacity policy, earth-top rule, threshold-based seam metric edge<=max(6,3*interior) over cap body rows), byte-copy mirror + sidecar, new `tests/Ratillery.AssetProcessor.Tests` (34 tests), docs (README terrain section, process-asset skill, ADR-002 amendment, help text). `dotnet build` 0w/0e; `dotnet test` 61/61 green (27 Core + 34 tool). Real art untouched. DEV_APPROVED; awaiting QA. |
| 2026-09-06 | TOOL-001 | QA PASS (cycle 1/3): all AC-1..AC-12 hold; build 0w/0e, tests 61/61; independent CLI verification with own synthetic fixtures in a temp sandbox (happy paths, all failure modes, stem/mirror/usage errors, determinism, byte-copy hash check, sidecar shape, regression on `validate` + no sprite assets regenerated); no scope creep (`src/` untouched, `assets/` untouched, raw art never processed). Non-blocking notes: shared `ReadPngHeader` naming, culture-dependent console summary, ratio null on floor-only case, repo-root cwd assumption for default mirror. Awaiting ANALYST_APPROVED (scope conformance) + human merge consent. |
| 2026-09-06 | TOOL-001 | ANALYST_APPROVED (scope conformance): every changed file maps to a story item; ACs match the drafted AC-1..AC-12 verbatim; rules in `TileProcessing` match story + DEC-007 (kinds, earth-top + headroom, opacity, seam axes, byte-copy, no pixel transforms); out of scope respected (no `src/`, no `assets/`, RAT stories untouched). Non-blocking: `ReadPngHeader` error-message wording reads oddly on the `validate` path (later cleanup). Merge gate 3/3 (DEV_APPROVED + QA PASS + ANALYST_APPROVED); pending human merge consent. |
| 2026-09-06 | TOOL-001 | Human granted merge consent + push. Merged to main (--no-ff, `aea3d79`), story `done`, feature branch deleted. Raw terrain art (3 files per DEC-007) committed separately on main per human request. Next: asset-processor validates the delivery via the new `tile` command (human said "lo probaremos con los assets"). |
