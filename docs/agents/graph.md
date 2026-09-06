# Agent Graph

Implementation of the agent workflow described in `AGENTS.md`, based on
opencode agents (`.opencode/agents/`) and skills (`.opencode/skills/`).

```text
              ┌──────────────┐
              │    HUMAN     │
              │ Product/Lead │
              └──────┬───────┘
                     │ product requests / decisions
                     ▼
              ┌───────────────┐
              │ ORCHESTRATOR  │  (.opencode/agents/orchestrator.md, primary)
              └───────┬───────┘
                      │
              ┌───────▼────────┐
              │ functional-    │  subagent, temp 0.2
              │ analyst        │  story -> docs/stories/GAME-###.md
              └───────┬────────┘
                      │
               open questions? ──yes──► HUMAN
                      │ no
                      ▼
              ┌───────────────┐
              │ developer     │  subagent, temp 0.2
              └───────┬───────┘
                BLOCKED ──► HUMAN
                      ▼
              ┌───────────────┐
              │ qa            │  subagent, temp 0.1, edit: deny
              └───────┬───────┘
          PASS ───┼───┴── FAIL -> developer (max 3 cycles)
                  ▼
             MERGE GATE (orchestrator)
                  DEV_APPROVED + QA PASS + ANALYST_APPROVED
                  ▼
            merge --no-ff to main -> DONE (branch deleted)
        (3 failed cycles or missing approval -> ESCALATE_TO_HUMAN)
```

Asset lane (separate from the dev graph, DEC-006 / ADR-002):

```text
HUMAN generates source PNG
        -> drops into assets-source/<rel>      (mirrors assets/ one-to-one)
        -> asset-processor                     (subagent, edit: deny)
             validate  needs-source-fix? -> HUMAN re-delivers
             sheet     PROCESSED -> HUMAN visual sign-off -> assets/<rel>
```

## Components

| Component           | File                                          | Role |
| ------------------- | --------------------------------------------- | ---- |
| orchestrator        | `.opencode/agents/orchestrator.md`            | primary agent; routes work, enforces iteration cap |
| functional-analyst  | `.opencode/agents/functional-analyst.md`      | product requirement -> story + AC |
| developer           | `.opencode/agents/developer.md`               | story -> implementation + tests |
| qa                  | `.opencode/agents/qa.md`                      | adversarial validation; PASS/FAIL/BLOCKED |
| asset-processor     | `.opencode/agents/asset-processor.md`         | validates + normalizes raw assets (ADR-002, DEC-006) |
| create-user-story   | `.opencode/skills/create-user-story/`         | story template and rules |
| implement-story     | `.opencode/skills/implement-story/`           | dev workflow and stop conditions |
| validate-story      | `.opencode/skills/validate-story/`            | QA report format |
| process-asset       | `.opencode/skills/process-asset/`             | asset validate/sheet checklist + report format |

## Principles

- Agents coordinate through artifacts (story files, ADRs), not chat.
- `main` holds only validated work; the developer works on
  `feat/<STORY-ID>-<slug>` branches and never merges them.
- The orchestrator merges only after three approvals: DEV_APPROVED (dev),
  PASS (qa), ANALYST_APPROVED (scope conformance).
- QA never edits code; findings flow back through the orchestrator.
- The asset-processor never edits or generates art; it runs the deterministic
  pipeline (`validate`/`sheet`) and returns PROCESSED / NEEDS_SOURCE_FIX /
  BLOCKED (ADR-002).
- L3 product decisions always escalate to the human.
- Deterministic edges (code-enforced graph) are a future evolution; this
  first version relies on the orchestrator prompt + iteration cap.
