# Ratillery — Status & Artifact Index

Single source of truth for development status. Maintained by the
orchestrator; every agent artifact must be registered here.

Last updated: 2026-09-06 (initialization complete, no stories yet)

## Milestone

First playable: **"A rat can destroy the world."**

Progress: 0 / 8 slices

| # | Slice | Story | Status |
| - | ----- | ----- | ------ |
| 1 | Animated rat (Idle) | RAT-001 | draft |
| 2 | 2D terrain + collision mask | — | pending |
| 3 | Aiming | — | pending |
| 4 | Firing + projectile | — | pending |
| 5 | Ballistic trajectory | — | pending |
| 6 | Impact detection | — | pending |
| 7 | Explosion | — | pending |
| 8 | Crater / terrain destruction | — | pending |

## Stories

| ID | Title | Status | QA | File |
| -- | ----- | ------ | -- | ---- |
| RAT-001 | Animated rat (Idle) | draft | — | `docs/stories/RAT-001-animated-rat-idle.md` |

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

## Product decisions

| ID | Decision | Author | Recorded in |
| -- | -------- | ------ | ----------- |
| DEC-001 | First milestone: "A rat can destroy the world" (8 slices above) | human | `docs/product/vision.md` |
| DEC-002 | Placeholder assets until human provides sprites/audio | human | `AGENTS.md` |
| DEC-003 | Visual direction: 2D side-view stylized cartoon; small big-headed rats; oversized weapons; visible terrain destruction; illustrated raster sprites (not pixel art); modular sprite sheet (idle/walk/aim/fire/hit/airborne/land/death); teams differentiated by accessories + secondary color. Human acts as graphic designer, provides assets on demand. | human | `temp/02-conversacion-chatgpt-idea-inicial.md` |
| DEC-004 | Human roles: product owner + graphic designer/asset provider. Temporary: to be replaced by an image-generation engine via MCP; asset specs stay machine-consumable. | human | `AGENTS.md` (Human Roles) |
| DEC-005 | Lowercase asset layout: raw files under `assets-source/`, game-ready files under `assets/`; preprocessing pipeline lives in-repo as `tools/Ratillery.AssetProcessor`. | human | ADR-002 |

## Session log

Chronological summary per dev cycle. One line per story/bug resolution.

| Date | Artifact | Outcome |
| ---- | -------- | ------- |
| 2026-09-06 | — | Repo + agent graph initialized |
| 2026-09-06 | DEC-003 | Visual direction approved by human; slice 1 story requested |
| 2026-09-06 | ADR-002 / DEC-005 | Asset pipeline built; rat idle processed to `assets/sprites/rats/base/idle.png` (8 frames, 273x265) + `idle.json` + `frames/` |
| 2026-09-06 | RAT-001 asset | QA found gutter-less source: tails crossed frame boundaries (frame 0 showed neighbor's tail tip; frames 1-7 had sectioned tails). Human re-sourced a clean sheet (2680x682, 8x335 cells with gutters) and it reprocessed correctly; generic delivery spec added at `assets-source/README.md`. |
