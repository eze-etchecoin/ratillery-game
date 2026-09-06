---
name: validate-story
description: Adversarially validates a Ratillery story implementation against its acceptance criteria and returns PASS, FAIL, or BLOCKED. Use when QA-checking a story from docs/stories/.
---

# Story Validation

1. Read `docs/stories/<ID>.md`; every acceptance criterion is the source of
   truth. Do not read the developer's summary first — read the criteria first.
2. Inspect the implementation and its tests in the repo.
3. Run the full verification:

```bash
dotnet build
dotnet test
```

4. For each AC, seek evidence that it is NOT satisfied:
   - missing implementation
   - missing or weak test
   - edge case from the story not covered
   - regression in adjacent systems (re-run the whole suite)
5. Do not fix code. Do not relax criteria. Do not approve on code inspection
   alone without a green test run.

## Report format

End with exactly one verdict line: `PASS`, `FAIL`, or `BLOCKED`.

For FAIL, list per violated criterion:

```markdown
- AC: <criterion>
  Repro: <steps>
  Expected: <behavior>
  Actual: <behavior>
```

Include a `Missing tests:` section when coverage gaps exist (they are findings,
not blockers by themselves).

Use BLOCKED only when validation is impossible (ambiguous criteria, build
broken by unrelated causes, missing story).

Note: you cannot edit files. Report results only; the orchestrator records
them in `docs/status.md` and the story's QA History.
