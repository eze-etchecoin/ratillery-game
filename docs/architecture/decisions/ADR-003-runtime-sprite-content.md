# ADR-003: Runtime sprite content delivery (mirrored `assets/`, raw texture load)

Date: 2026-09-06
Status: accepted

## Context

RAT-001 must render the delivered idle sprite sheet
(`assets/sprites/rats/base/idle.png`, 8 frames, 273x265) plus its `idle.json`
metadata sidecar and per-frame files. MonoGame content traditionally goes
through the content pipeline (`Content.mgcb`), which compiles sources into
XNB files and copies them to the output `Content/` folder (as done for
`Debug.spritefont`).

Game-ready sprites live under the repo `assets/` tree (ADR-002: processed
output, single source of truth, lowercase layout, JSON sidecar next to each
sheet). Two tensions appear:

1. The asset pipeline (ADR-002) forbids duplicating game-ready material and
   wants asset swaps to be config/data-only (reprocess + rebuild).
2. The `.mgcb` builder does not support referencing files outside the
   `Content/` directory: an entry pointing at `../../../assets/...` is either
   ignored or emits its XNB outside `Content/bin`, where it never reaches the
   game output folder. Verified empirically during RAT-001.

Additionally RAT-001 AC-4 requires animation parameters to come from
`idle.json` at runtime, and AC-7 requires per-frame load failure to fall back
to frame 0 without crashing.

## Decision

Sprites (and their JSON sidecars) bypass XNB compilation:

- `src/Ratillery.Game/Ratillery.Game.csproj` mirrors the whole `assets/` tree
  at build time into the game output as `Content/` (`<None Include="..\..\assets\**\*"
  Link="Content\%(RecursiveDir)%(Filename)%(Extension)"
  CopyToOutputDirectory="PreserveNewest" />`). The runtime `Content.RootDirectory`
  convention is unchanged; the pipeline-built font still ships as XNB.
- The Game project loads PNGs at runtime with `Texture2D.FromFile` (DesktopGL)
  and parses the JSON sidecar with `System.Text.Json`. Sprite batches that
  draw these straight-alpha textures use `BlendState.NonPremultiplied`.
- `Animation/SpriteAnimation` loads each frame file individually; any frame
  that is missing or fails to load is replaced by frame 0 (RAT-001 AC-7),
  keeping the full loop when all frames are present.
- `Content.mgcb` keeps only font content; sprites are not added there.

## Consequences

- Single source of truth stays in `assets/`: replacing art = run the asset
  pipeline then rebuild; no XNB copies to keep in sync.
- Frame-level load failure is expressible (per-frame files), which satisfies
  the RAT-001 AC-7 fallback requirement; a compiled single-texture XNB would
  make per-frame failure impossible to represent.
- Sprites are not premultiplied by the pipeline; the Game must draw them with
  `BlendState.NonPremultiplied` (or premultiply manually later).
- Whole-`assets/` mirroring copies future assets automatically; an unused
  subtree can be excluded later if it grows.
