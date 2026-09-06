# RAT-001: Animated rat (Idle)

status: draft

## Story

As a player,
I want to see an animated cartoon rat breathing idly on screen,
so that the game has a living character and establishes the visual identity
that all later gameplay (terrain, aiming, destruction) builds upon.

This is slice 1 of the milestone "A rat can destroy the world". Per DEC-003
and DEC-002, the rat is a 2D side-view stylized cartoon sprite (not pixel
art), initially facing right, and the implementation must work with clearly
identified placeholder assets until the human (graphic designer) delivers the
real idle sprite sheet.

## Acceptance Criteria

- AC-1: Launching the game shows a playable-canvas scene with a solid or
  simple background and a single rat character placed on screen, large
  enough to be clearly appreciated (roughly 1/8 of the screen height or
  larger; exact scale is a visual-approval decision for the human).
- AC-2: The rat is rendered from a horizontal sprite strip (single row of
  equal-size frames) representing the **idle** animation only.
- AC-3: The idle animation loops continuously at a configured frames-per-
  second rate, cycling through all frames in order and returning to the
  first frame without visible glitch or long freeze between loops.
- AC-4: The animation parameters (frame count, frame width/height, frames
  per second) are data/configuration-driven, not hardcoded constants, so
  the human's final sprite sheet can be dropped in without code changes.
- AC-5: While final assets are pending, the game runs with placeholder
  graphics that are clearly identified as placeholders in the asset
  location/naming (per DEC-002), not mistaken for final art.
- AC-6: When the human's real idle sheet replaces the placeholder (same
  strip format), the animation works with no source-code change beyond
  configuration/asset replacement.
- AC-7: The character faces right by default (per vision.md), consistent
  with the sprite orientation defined in the asset spec below.
- AC-8: The animation runs at a stable, frame-rate-independent cadence
  (animation advances by elapsed time, not by render-frame count).
- AC-9: `dotnet build` succeeds and `dotnet test` passes; existing Core
  unit tests remain green.

## Edge Cases

- Sprite sheet whose frame count or frame size differs from the
  placeholder's: covered by AC-4/AC-6 (configuration, not code).
- Very short elapsed time between updates (high refresh-rate monitors):
  animation must not skip frames incorrectly or overflow (AC-8).
- Window resized while running: the rat remains visible on screen; exact
  resize/rescaling behavior may be minimal (no camera system yet) but the
  rat must never disappear off-canvas.
- Placeholder asset missing or zero frames configured: the game must not
  crash silently into an invalid state; a visible fallback or a clear
  startup error is acceptable (developer's choice at L1).
- Alt-tab / pause: on return, animation resumes cleanly without a
  time-jump burst of skipped frames.

## Asset Spec (required from human)

The human must eventually deliver, for this slice:

- `rat-idle` sprite sheet: one horizontal strip, N equal-width frames,
  side view, facing **right**, transparent background, raster illustrated
  cartoon style (DEC-003).
- Required parameters to confirm (see Open Questions): frame count N,
  frame dimensions in pixels (width x height per frame), and intended
  playback FPS.
- Suggested starting point (agent proposal, needs human approval):
  6-10 frames of breathing/subtle-motion loop, square-ish frames
  (~256x256 px), ~8-12 FPS. Placeholders will be generated at these
  numbers so the pipeline is proven before final art arrives.

## Scope boundaries

### In scope

- Minimal game bootstrap: window, game loop, one scene rendering the
  animated rat (bootstrapping allowed for this first story, kept tight).
- Idle animation playback only.
- Placeholder asset(s) clearly identified as such.

### Out of scope

- Any other animation states (walk, aim, fire, hit, airborne, land,
  death) beyond the sheet structure being ready for them later.
- Terrain, physics, combat, turns, damage, input-driven movement or
  camera systems.
- Team differentiation / accessories (DEC-003) — single base rat only.
- Audio.

### Core vs Game placement

Rendering and animation belong exclusively in `src/Ratillery.Game`.
Nothing in this slice requires domain rules; `src/Ratillery.Core` should
remain unchanged except (at most) trivial bootstrap already covered by
ADR-001. If the developer finds a Core need, that is an L2 decision
requiring an ADR.

## Dependencies

- ADR-001 (initialization decisions) — assumed already settled.
- DEC-002 (placeholders), DEC-003 (visual direction).

## Open Questions

- OQ-1: Final asset numbers for the idle sheet — frame count, frame size
  (px), and FPS. Options: accept the suggested proposal (6-10 frames,
  ~256x256, 8-12 FPS) or specify exact values. Needed before status:
  ready? (Story can proceed with placeholders either way, but the spec
  should be confirmed.)
- OQ-2: Should the placeholder be a clearly non-final graphic (e.g.
  simple colored shapes labeled "PLACEHOLDER") or a rough generated
  draft rat? DEC-002 only requires "clearly identified" — visual
  approval is a human decision (L3).

## QA History

- (appended by the orchestrator/developer from QA reports)
