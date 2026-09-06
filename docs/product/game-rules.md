# Ratillery — Game Rules (draft)

Working set of established gameplay rules. The functional analyst must not
contradict this document silently; changes belong to the human.

## Format

- 2D side-view terrain, camera follows the action.
- Turn-based: one rat acts per turn (move + one action per turn is the
  working assumption until a story formalizes it).

## Physics

- Ballistic projectiles: gravity, launch angle, power; wind planned.
- Semi-implicit Euler integration (see `Ratillery.Core.Physics.Ballistics`).
- No external physics engine; ballistics implemented internally.

## Terrain

- Destructible: explosion removes a circular region (texture + collision
  mask), producing craters.

## Combat

- Damage and explosive weapons; details (falloff, radius, per-weapon stats)
  to be defined story by story.

## Open rules (require human decision)

- Can the player act without moving first?
- Fall damage?
- Can rats jump? How high?
- Turn timer?
