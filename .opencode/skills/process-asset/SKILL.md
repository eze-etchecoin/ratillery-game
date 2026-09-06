---
name: process-asset
description: Validates and normalizes raw sprite/image assets for Ratillery (ADR-002). Use when processing or validating files the human drops into assets-source/, e.g. sprite sheets, animations, weapons, objects, icons, or when answering whether a delivered PNG is usable. Run by the asset-processor agent.
---

# process-asset

Validate that raw images delivered under `assets-source/` are processable and
usable, then normalize the clean ones into `assets/` with the pipeline tool.
You never generate or hand-edit art.

## Reference

- Spec + delivery rules: `assets-source/README.md`.
- Layout: `assets-source/<rel>` mirrors `assets/<rel>` one-to-one.
- Tool: `tools/Ratillery.AssetProcessor` (`validate`, `sheet`, `inspect`).

## Checklist (per file)

1. Read `assets-source/README.md` once for the delivery spec.
2. Determine the intended frame count N for the file:
   - animated strip -> N from the human's request;
   - single-frame sprite (weapon/object/icon/prop) -> `1`;
   - if unknown, run `validate` without `--frames` and use its auto-detected
     `frameCount`, or ask the human when ambiguous.
3. Run the deterministic verdict:

   ```bash
   dotnet run --project tools/Ratillery.AssetProcessor -- validate <input> [--frames N] --json
   ```

4. Read the JSON verdict. Map it as follows:
   - `verdict == "ok"` -> processable; continue.
   - `verdict == "needs-source-fix"` -> do NOT process. Report each `issues[]`
     item verbatim, tag the file `NEEDS_SOURCE_FIX`, and ask the human to
     re-deliver per `assets-source/README.md`. No art edits, ever.
5. Process the clean file into the mirrored `assets/` path:

   ```bash
   dotnet run --project tools/Ratillery.AssetProcessor -- sheet <input> --frames N \
     --output assets/<rel>.png --frames-dir assets/<rel-dir>/frames --name <name>
   ```

   - `<name>`: human-provided name, else the file stem (e.g. `idle`); prefer a
     `category-item` style (`rat-idle`) when clear from the path.
   - single-frame sprites: use `--frames 1` (trims margins, writes metadata).
6. Sanity-check the outputs it produced (metadata JSON exists; for animated:
   N frames written, consistent cell size) and, when the source was a strip,
   re-run `validate` on the produced sheet is unnecessary - the pipeline is
   deterministic once the source passed.

## Report

End with exactly one status line per file and a short summary:

```text
PROCESSED          <source> -> assets/<rel>.png (N frames, cell WxH)
NEEDS_SOURCE_FIX   <source>: <validate issues, verbatim>
BLOCKED            <source>: <what is missing to proceed>
```

PROCESSED still needs the human's final visual sign-off; say so in the summary.

## Boundaries

- Never edit raw files or touch pixels of any image.
- Never run `sheet` on a `needs-source-fix` source unless the human overrides.
- Never commit; leave committing to the human.
- If the target `assets/` path is unclear or N is unknown, return `BLOCKED`
  with the specific question instead of inventing an answer.
