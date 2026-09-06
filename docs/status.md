# Ratillery — Status & Artifact Index

Single source of truth for development status. Maintained by the
orchestrator; every agent artifact must be registered here.

Last updated: 2026-09-06 (initialization complete, no stories yet)

## Milestone

First playable: **"A rat can destroy the world."**

Progress: 0 / 8 slices

| # | Slice | Story | Status |
| - | ----- | ----- | ------ |
| 1 | Animated rat (Idle) | — | pending |
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
| — | — | — | — | — |

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

## Product decisions

| ID | Decision | Author | Recorded in |
| -- | -------- | ------ | ----------- |
| DEC-001 | First milestone: "A rat can destroy the world" (8 slices above) | human | `docs/product/vision.md` |
| DEC-002 | Placeholder assets until human provides sprites/audio | human | `AGENTS.md` |

## Session log

Chronological summary per dev cycle. One line per story/bug resolution.

| Date | Artifact | Outcome |
| ---- | -------- | ------- |
| 2026-09-06 | — | Repo + agent graph initialized |
