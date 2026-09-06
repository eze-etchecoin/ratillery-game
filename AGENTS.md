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
dotnet run --project tools/Ratillery.AssetProcessor -- sheet <raw> --frames N --output <sheet>
```

Asset pipeline (ADR-002): raw human/generated assets go under
`assets-source/` and are normalized by `tools/Ratillery.AssetProcessor`
(`inspect`/`sheet`) into game-ready files under `assets/`. Never hand-edit or
commit to `assets-source/`; reprocess instead.

## Architecture

Strict separation between domain logic and MonoGame integration:

- `src/Ratillery.Core/` — game rules, turns, ballistics, damage, entities,
  world state. No MonoGame dependencies. Must be unit-testable without a
  graphics window.
- `src/Ratillery.Game/` — game loop, rendering, input, asset loading, camera,
  scenes, animation, adaptation between Core and rendering.
- `tests/Ratillery.Core.Tests/` — xUnit tests for Core only. Never test rendering.
- `assets-source/` — raw human/generated assets (sprites, audio). Never
  modified by agents; game-ready copies are produced from here (ADR-002).
- `assets/` — processed, game-ready material (lowercase layout) plus sidecar
  `.json` metadata next to each sprite sheet.
- `tools/Ratillery.AssetProcessor/` — asset preprocessing pipeline (ADR-002).
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
- Assets (sprites, audio) are provided by the human as raw files under
  `assets-source/`; agents never modify originals and produce game-ready
  copies through the pipeline into `assets/` (ADR-002). Until delivered, use
  clearly-identified placeholders.

## Human Roles

The human fills these roles (DEC-004):

- **Product owner** — decides on gameplay, scope, and anything tagged L3.
- **Graphic designer / asset provider** — supplies all visual assets
  (sprites, concept art, UI art, terrain art) for the game. Agents never
  generate final art; when a story needs an asset, the story file must
  include an explicit asset spec (format, frame count, frame size,
  orientation, style reference) and the human delivers it as a raw file into
  `assets-source/` per ADR-002.
  Until delivered, agents use clearly-identified placeholders (DEC-002).
- This arrangement is **temporary**: the plan is to replace the human asset
  provider with an image-generation engine connected via MCP. Agents should
  keep asset specs machine-consumable (structured, in the story file) to
  make that swap easy. No other workflow changes are required for it.

Status tracking:

- `docs/status.md` is the single index of agent artifacts (stories, bugs,
  ADRs, product decisions, session log). Every artifact must be registered
  there; the orchestrator keeps it updated after each node transition.

Branching:

- `main` holds only finished, validated work; never commit features directly
  to it.
- The developer implements each story on `feat/<STORY-ID>-<slug>`.
- The orchestrator merges into `main` only after three approvals:
  `DEV_APPROVED` (developer), `PASS` (QA), `ANALYST_APPROVED` (scope
  conformance). Merge with `--no-ff`, then mark the story `done`.

## Definition of Done

A story is complete only when:

- All acceptance criteria are implemented.
- `dotnet build` succeeds and `dotnet test` passes.
- QA reports PASS.
- Relevant architecture decisions are recorded as ADRs.
