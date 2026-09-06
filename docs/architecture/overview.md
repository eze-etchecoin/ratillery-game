# Architecture Overview

## Solution layout

```text
src/Ratillery.Core/          domain: entities, ballistics, turns, world (no MonoGame)
src/Ratillery.Game/          MonoGame DesktopGL: loop, rendering, input, scenes, animation
tests/Ratillery.Core.Tests/  xUnit tests for Core only
assets/                      sprites, terrain, weapons, effects, UI, audio (out of source)
```

## Rules

- `Ratillery.Core` has zero MonoGame references and must be testable without
  a graphics window. Coordinates use screen convention (Y grows downwards;
  gravity is +Y) — see `Ballistics`.
- `Ratillery.Game` adapts Core state to rendering; no gameplay rules here.
- Content pipeline: `src/Ratillery.Game/Content/Content.mgcb`; fonts are
  compiled at build time and loaded from `Content/`. Sprites/JSON ship from
  the repo `assets/` tree, mirrored into the output `Content/` folder at
  build time and loaded at runtime (ADR-003).
- Assets are provided by the human; code ships clearly-identified
  placeholders (e.g. `PlaceholderRat`) until then.

## Decisions

Architecture decisions (ADR format) live in `decisions/`.
