---
name: implement-story
description: Implements a Ratillery user story end to end (code + tests + verification). Use when developing a story from docs/stories/ or when fixing issues from a QA FAIL report.
---

# Story Implementation

1. Read the story `docs/stories/<ID>.md`; confirm `status: ready`.
2. Explore the codebase before changing anything:
   - `src/Ratillery.Core/` — domain logic (no MonoGame).
   - `src/Ratillery.Game/` — rendering, input, scenes, animation.
   - `tests/Ratillery.Core.Tests/` — xUnit tests for Core.
3. Plan the smallest change set that satisfies every acceptance criterion.
4. Implement, following AGENTS.md conventions (nullable enabled, no
   unnecessary comments, simplest solution that can evolve).
5. Add or update unit tests for new Core logic; run:

```bash
dotnet build
dotnet test
```

6. If a QA report was provided, address each FAIL item exactly as reported.
7. Update the story `status:` only as instructed by the orchestrator.
8. If an L2 architecture decision was made, record an ADR in
   `docs/architecture/decisions/`.

## Stop conditions

Return BLOCKED when:

- the story requires changing acceptance criteria or inventing gameplay rules;
- a dependency is missing or broken;
- the change cannot stay within the story scope.
