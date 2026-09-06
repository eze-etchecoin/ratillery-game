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
                DONE
        (3 failed cycles -> ESCALATE_TO_HUMAN)
```

## Components

| Component           | File                                          | Role |
| ------------------- | --------------------------------------------- | ---- |
| orchestrator        | `.opencode/agents/orchestrator.md`            | primary agent; routes work, enforces iteration cap |
| functional-analyst  | `.opencode/agents/functional-analyst.md`      | product requirement -> story + AC |
| developer           | `.opencode/agents/developer.md`               | story -> implementation + tests |
| qa                  | `.opencode/agents/qa.md`                      | adversarial validation; PASS/FAIL/BLOCKED |
| create-user-story   | `.opencode/skills/create-user-story/`         | story template and rules |
| implement-story     | `.opencode/skills/implement-story/`           | dev workflow and stop conditions |
| validate-story      | `.opencode/skills/validate-story/`            | QA report format |

## Principles

- Agents coordinate through artifacts (story files, ADRs), not chat.
- QA never edits code; findings flow back through the orchestrator.
- L3 product decisions always escalate to the human.
- Deterministic edges (code-enforced graph) are a future evolution; this
  first version relies on the orchestrator prompt + iteration cap.
