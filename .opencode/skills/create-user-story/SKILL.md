---
name: create-user-story
description: Creates a structured Ratillery user story with acceptance criteria and edge cases. Use when writing or revising a story from a product requirement, before implementation begins.
---

# User Story Creation

1. Read `docs/product/game-rules.md` and `docs/product/vision.md` for
   established rules; never contradict them silently.
2. List the existing stories in `docs/stories/` to pick the next sequential
   ID (`GAME-001`, `GAME-002`, ...).
3. Write the story file as `docs/stories/<ID>.md` using the template below.
4. Keep every requirement declarative and verifiable; no classes, patterns,
   or implementation hints.

## Template

```markdown
# <ID>: <title>

status: draft | ready | done

## Story

As a player,
I want ...,
so that ...

## Acceptance Criteria

- AC-1: ...
- AC-2: ...

## Edge Cases

- ...

## Dependencies

- <ID or feature>, if any

## Open Questions

- ... (must be empty before status: ready)

## QA History

- (appended by the orchestrator/developer from QA reports)
```

## Rules

- One story per file; keep it small enough for one dev/QA cycle.
- If a gameplay decision is ambiguous and materially affects the game, do not
  decide: leave it in Open Questions and report OPEN_QUESTIONS to the caller.
- Set `status: ready` only when Open Questions is empty.
