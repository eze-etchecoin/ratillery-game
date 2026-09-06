# Ratillery — Project Context

Ratillery is a 2D turn-based artillery tactics game starring military rats,
inspired conceptually by Worms but with its own identity.

> Tiny rats. Massive damage.

Core pillars: turn-based tactical combat, artillery, ballistic physics,
destructible terrain, multiple weapons/projectiles, expressive sprite-sheet
animation, visual humor.

First playable milestone: **"A rat can destroy the world."** (animated rat,
destructible terrain, aiming, firing, ballistics, explosion, crater).

## Stack

- C# / .NET 10
- MonoGame 3.8 (DesktopGL)
- xUnit for unit tests

## Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Ratillery.Game
```

## Architecture

Strict separation between domain logic and MonoGame integration:

- `src/Ratillery.Core/` — game rules, turns, ballistics, damage, entities,
  world state. No MonoGame dependencies. Must be unit-testable without a
  graphics window.
- `src/Ratillery.Game/` — game loop, rendering, input, asset loading, camera,
  scenes, animation, adaptation between Core and rendering.
- `tests/Ratillery.Core.Tests/` — xUnit tests for Core only. Never test rendering.
- `assets/` — sprites, terrain, weapons, effects, UI, audio. Kept outside source.
- `docs/` — product vision, game rules, user stories, architecture decisions.

Conventions:

- Nullable Reference Types enabled, implicit usings.
- No comments unless they add real information.
- Prefer the simplest solution that can evolve later; no premature abstraction,
  no ECS, no external physics engine, no networking/multiplayer, no persistence.
- Conventional commits, messages in English (e.g. `feat:`, `fix:`, `chore:`,
  `test:`, `docs:`).

## Agent Workflow

Feature development follows a graph:

```text
PRODUCT_REQUEST
    -> functional-analyst   (story + acceptance criteria -> docs/stories/)
    -> developer            (implementation + tests)
    -> qa                   (adversarial validation)

QA PASS  -> DONE
QA FAIL  -> developer -> qa   (max 3 cycles, then ESCALATE_TO_HUMAN)
BLOCKED  -> HUMAN
```

Rules:

- Agents coordinate through artifacts (story files, ADRs), not ad-hoc chat.
- Product decisions are never invented by agents. Gameplay ambiguities
  escalate to the human.
- Decision levels: L1 internal (developer decides), L2 architecture
  (developer decides + ADR in `docs/architecture/decisions/`), L3 product
  (human decides).
- Assets (sprites, audio) are provided by the human; use clearly-identified
  placeholders until then.

Status tracking:

- `docs/status.md` is the single index of agent artifacts (stories, bugs,
  ADRs, product decisions, session log). Every artifact must be registered
  there; the orchestrator keeps it updated after each node transition.

## Definition of Done

A story is complete only when:

- All acceptance criteria are implemented.
- `dotnet build` succeeds and `dotnet test` passes.
- QA reports PASS.
- Relevant architecture decisions are recorded as ADRs.
