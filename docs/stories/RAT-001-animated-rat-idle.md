# RAT-001: Animated rat (Idle)

status: done

## Story

As a player,
I want to see an animated cartoon rat breathing idly on screen, standing on a
flat floor,
so that the game has a living character and establishes the visual identity
that all later gameplay (terrain, aiming, destruction) builds upon.

This is slice 1 of the milestone "A rat can destroy the world". Per DEC-003,
the rat is a 2D side-view stylized cartoon sprite (not pixel art), initially
facing right. The real idle sprite sheet has been delivered by the human and
validated as PROCESSED by the asset-processor (ADR-002); this slice renders
its full looping idle animation. The rat is staged on a simple, flat,
non-destructible floor reference so its bottom-center pivot reads naturally.
Destructible terrain is explicitly out of scope for this slice (milestone
slice 2).

## Acceptance Criteria

- AC-1: Launching the game shows a playable-canvas scene with a simple
  background and a single rat character on screen, large enough to be clearly
  appreciated (roughly 1/8 of the screen height or larger; exact scale is a
  visual-approval decision for the human).
- AC-2: The rat is rendered from the delivered horizontal sprite strip
  representing the **idle** animation only, with a frame geometry matching
  the Asset Spec below (8 frames, 273x265 px each).
- AC-3: The idle animation loops continuously, cycling through all 8 frames
  in order (frame 0 → 1 → … → 7 → 0) at the configured rate (8 fps), with no
  visible glitch or long freeze between loops. The full looping animation is
  required; a static pose is not acceptable.
- AC-4: The animation parameters (frame count, frame width/height, playback
  FPS, loop flag, pivot anchor) are data/configuration-driven — taken from
  the delivered `idle.json` metadata — not hardcoded source constants.
- AC-5: The rat is anchored and placed by its bottom-center pivot
  (x: 0.5, y: 1.0): on screen the rat's feet rest on the top edge of the
  floor reference, so the pivot reads naturally.
- AC-6: A simple, flat floor reference — a horizontal band or line spanning
  the scene width — is rendered below the rat for visual staging only. It is
  non-destructible and non-interactive (no collision, damage, or state) in
  this slice.
- AC-7: If a frame other than frame 0 is missing or fails to load, the rat is
  still rendered using frame 0 (`idle_00`) as a fallback rather than crashing;
  the fallback does not replace the full looping animation when all 8 frames
  are available.
- AC-8: The character faces right by default (per vision.md), consistent with
  the delivered sprite orientation.
- AC-9: The animation advances by elapsed time, not by render-frame count, so
  playback cadence is stable regardless of display frame rate (high-refresh
  displays, pauses, etc.).
- AC-10: `dotnet build` succeeds and `dotnet test` passes; existing Core unit
  tests remain green.

## Edge Cases

- Entire asset set unavailable (sheet or metadata missing, or zero frames
  configured): the game must not crash silently into an invalid state; a
  visible fallback or a clear startup error is acceptable (developer's choice
  at L1).
- Individual frame missing or corrupt at load time: covered by AC-7 (render
  frame 0 as fallback; keep the loop when the remaining frames are present).
- Very short elapsed time between updates (high refresh-rate monitors): the
  animation must not skip frames incorrectly or overflow (AC-9).
- Window paused / alt-tab: on return, the animation resumes cleanly without a
  time-jump burst of skipped frames (AC-9).
- Window resized while running: the rat and the floor reference remain
  visible on screen; the rat must never disappear off-canvas (exact
  rescaling behavior may be minimal — no camera system yet).
- The floor reference must not be mistaken for terrain: nothing in this slice
  may damage, destroy, move, or collide with it (AC-6).

## Asset Spec (DELIVERED — PROCESSED)

The real asset has been delivered by the human and validated by the
asset-processor with verdict PROCESSED (ADR-002). No further asset delivery is
required for this slice.

| Field | Confirmed value |
| ----- | --------------- |
| Source file | `assets-source/sprites/rats/base/idle.png` |
| Processed sheet | `assets/sprites/rats/base/idle.png` (2184 x 265 px, one horizontal strip) |
| Frame count | 8 |
| Frame size | 273 x 265 px (width x height) |
| Playback FPS | 8 |
| Loop | true |
| Pivot | x 0.5, y 1.0 (bottom-center) |
| Metadata sidecar | `assets/sprites/rats/base/idle.json` |
| Individual frames | `assets/sprites/rats/base/frames/idle_00.png` … `idle_07.png` |

Style/orientation: side-view stylized cartoon raster (DEC-003), facing right.
Fallback role: `idle_00.png` (frame 0) acts as the fallback frame per the
Decisions record below (OQ-2).

## Scope boundaries

### In scope

- Minimal game bootstrap: window, game loop, one scene rendering the animated
  rat (bootstrapping allowed for this first story, kept tight).
- Full looping idle animation playback (all 8 frames) using the delivered
  game-ready assets.
- Simple background plus the flat floor reference for staging.
- Frame-0 fallback when frames are missing or fail to load.

### Out of scope

- Destructible terrain, 2D terrain/collision mask, and any floor behavior
  beyond passive visual staging (milestone slice 2).
- Any other animation states (walk, aim, fire, hit, airborne, land, death)
  beyond the sheet structure being ready for them later.
- Physics, combat, turns, damage, wind, input-driven movement, camera
  systems.
- Team differentiation / accessories (DEC-003) — single base rat only.
- Audio.

### Core vs Game placement

Rendering and animation belong exclusively in `src/Ratillery.Game`. Nothing
in this slice requires domain rules; `src/Ratillery.Core` should remain
unchanged except (at most) trivial bootstrap already covered by ADR-001. If
the developer finds a Core need, that is an L2 decision requiring an ADR.

## Dependencies

- ADR-001 (initialization decisions) — assumed already settled.
- ADR-002 (asset pipeline) — this story consumes its processed outputs;
  delivery already completed with verdict PROCESSED.
- DEC-002 (placeholders / fallback), DEC-003 (visual direction).

## Decisions

Resolutions provided by the human (product owner); all prior open questions
are closed.

1. OQ-1 (asset spec) RESOLVED: The real asset has been delivered and
   validated by the asset-processor (verdict PROCESSED). Confirmed spec from
   `assets/sprites/rats/base/idle.json`: 8 frames, frame 273x265, 8 fps,
   loop=true, pivot {x: 0.5, y: 1.0} (bottom-center). Source:
   `assets-source/sprites/rats/base/idle.png`. Processed outputs:
   `assets/sprites/rats/base/idle.png` (sheet 2184x265), `idle.json`,
   `frames/idle_00.png` … `idle_07.png`. These concrete values are the asset
   spec in this story.
2. OQ-2 (placeholder) RESOLVED: placeholder = frame 0 of `idle.png`
   (`assets/sprites/rats/base/frames/idle_00.png`). It is a fallback
   mechanism only: if a frame is missing/not loaded, render frame 0; it does
   not replace the full looping animation.
3. Scope decision (product owner, Option B): RAT-001 DOES include the full
   looping idle animation of all 8 frames as an acceptance criterion. The
   story is: the rat is visible on screen and animated (idle loop).
4. Stage decision (product owner): the rat stands on a SIMPLE, FLAT FLOOR
   REFERENCE (a flat band/line, non-destructible, non-interactive) purely for
   visual staging so the bottom-center pivot reads naturally. DESTRUCTIBLE
   TERRAIN IS OUT OF SCOPE for RAT-001 (it is milestone slice 2, later
   story).

## QA History

- (appended by the orchestrator/developer from QA reports)
