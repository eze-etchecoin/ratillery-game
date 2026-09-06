# Architecture Decision Records

L2 decisions (developer-owned but with lasting impact) are recorded here as
`ADR-###.md`.

## Template

```markdown
# ADR-###: <title>

Date: YYYY-MM-DD
Status: accepted | superseded by ADR-###

## Context

<what forces the decision>

## Decision

<what was decided>

## Consequences

<tradeoffs and impact>
```

## Recorded decisions

- ADR-001: .NET 10, MonoGame DesktopGL, net10.0 uniform, content pipeline
  with `FontDescriptionProcessor` (see git history of the initialization
  commit for details).
- ADR-002: asset pipeline — `assets-source/` raw vs `assets/` processed,
  lowercase layout, `tools/Ratillery.AssetProcessor` preprocessing tool.
