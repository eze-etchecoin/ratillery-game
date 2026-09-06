# TOOL-001: Asset-processor tile mode — validation and mirrored delivery of terrain layer textures

status: in-progress

## Story

As the asset-delivery operator (the human asset provider and the asset-processor
agent),
I want the asset pipeline (`tools/Ratillery.AssetProcessor`, ADR-002) to gain a
`tile` command that validates the DEC-007 terrain layer textures against their
per-kind structural rules and, when clean, mirrors them into `assets/` as whole,
untouched tiles,
so that the real terrain art can be delivered without the sprite-only rules of
`validate`/`sheet` wrongly rejecting it and without any agent or tool ever
re-interpreting, cropping, or editing pixels.

This is an **L2 tooling story**, not a milestone slice and not a gameplay
feature. Today the pipeline (`inspect`, `validate`, `sheet`) is sprite-only:
every rule assumes a transparent-background subject (RGBA with real alpha,
transparent margins, gutter splits for strips, baseline drift). An opaque
full-bleed tileable texture trips the "no alpha / no transparent pixels"
warnings and the single-frame margin checks (content reaches all canvas edges),
producing a wrong `needs-source-fix` verdict — verified in practice against the
current delivery under `assets-source/terrain/layers/`, which `validate` mis-reads
as "a strip of 3 subjects; add transparent gutters". `sheet` is equally
inapplicable: terrain textures load raw 1:1 at runtime (ADR-003) and must NOT be
cropped, frame-split, bottom-aligned, given transparent padding, or shipped with
`fps`/`loop`/`pivot` metadata.

The product-side delivery model is **binding** (DEC-007; also recorded in
`docs/status.md` and in RAT-002's asset spec). The real art is **3 RGBA PNGs** at
`assets-source/terrain/layers/`, mirrored to `assets/terrain/layers/`:

| File | Role (DEC-007) | Layout rules this story validates |
| ---- | -------------- | -------------------------------- |
| `surface-cap.png` | Thin cap hugging the terrain contour; engine later aligns its earth-top row to the geometric surface line H(x) | Horizontally seamless body only. Transparent headroom rows ABOVE its **earth-top row** (the first row that is fully opaque across the whole width) hold grass-tuft overhang in alpha; from the earth-top row to the bottom of the file the body must be fully opaque (no transparent holes). Solid body height = fileHeight − earthTopRow. |
| `rock.png` | Rock band, depth-below-surface (contour-hugging) | Opaque full-bleed; horizontally seamless only; 1:1 vertical mapping (no vertical repeat); no transparent holes anywhere. |
| `interior.png` | Deep-bulk tile; engine repeats it down to the playfield bottom with a fixed world anchor | Opaque full-bleed; seamless BOTH horizontally AND vertically; no transparent holes anywhere. |

Sampling semantics (cap + rock = depth-below-local-surface per column; interior =
world-anchored vertical tiling) are engine-side and are **not** in this story's
runtime scope — but the tile *validation* must guarantee the delivery properties
above, because that is what makes those semantics possible. Exact pixel
dimensions and band-thickness values are intentionally NOT validated (DEC-007:
band thicknesses stay configurable and are matched to the delivered art 1:1 in a
later engine story).

The story's ceiling: deterministic validation + mirror copy (+ minimal metadata
sidecar). No scaling, no cropping, no color transforms, no re-encoding, no art
editing. Terrain layers are validated and mirrored as **whole textures** (never
sprite-normalized) — a human-facing consequence the documentation must state.

## Acceptance Criteria

### Command, kind selection, contract

- AC-1 (**command and kind selection**): A new deterministic command `tile`
  exists in `tools/Ratillery.AssetProcessor`, documented in the tool's
  usage/help text alongside `inspect`, `validate`, and `sheet`. Invocation:
  `tile <input> [--output <path>] [--json] [--report <path>]`. The tile kind is
  derived from the input file's **stem**, which must be exactly one of the three
  canonical DEC-007 names — `surface-cap`, `rock`, `interior` (lowercase,
  exact). Any other stem is a deterministic usage error (nonzero exit, clear
  message naming the three accepted kinds); no verdict is emitted and nothing is
  written. There is **no kind-selection flag**, so a file can never be validated
  under the wrong kind by mistake.
- AC-2 (**verdict contract**): On a valid invocation, `tile` emits a
  deterministic JSON report following the same shape and conventions as
  `validate` (camelCase, indented): `verdict` (`"ok"` | `"needs-source-fix"`),
  `issues[]` (each `{severity: "error" | "warning", message}`), plus useful
  tile-specific fields: `input`, `width`, `height`, `colorType`, `hasAlpha`,
  `kind`, the thresholds applied, per-axis seam measures (measured value +
  pass/fail per applicable axis), `earthTopRow` and `bodyHeight` for
  surface-cap (absent/null otherwise), and the target output path when the
  verdict is ok. Console default is a human-readable summary like `validate`;
  `--json` prints only the JSON report; `--report <path>` also writes it to a
  file. Any `error` issue ⇒ `needs-source-fix`; warnings alone keep the verdict
  `ok`. A verdict-bearing run exits 0 whether the verdict is `ok` or
  `needs-source-fix` (matching `validate`); usage/IO errors exit nonzero.
- AC-3 (**determinism**): Identical input + options produce an identical report
  on every run (no timestamps, random values, or volatile ordering); the JSON is
  byte-identical across repeat runs, and re-running a clean file is idempotent
  (the same output bytes are written again).

### Per-kind validation rules

- AC-4 (**opacity policy — rock and interior**): `rock` and `interior` must be
  fully opaque. A PNG without an alpha channel (opaque by construction) is
  accepted; for any PNG that carries alpha, every decoded pixel must have
  alpha == 255. A single pixel with alpha < 255 anywhere is an `error` issue
  ("not fully opaque"). No alpha threshold is applied — alpha < 255 means
  non-opaque, exactly.
- AC-5 (**format/alpha policy — surface-cap**): `surface-cap` must carry a real
  alpha channel: a PNG whose header has no alpha channel is rejected with an
  `error`. The cap must also contain actual transparency: a fully opaque file
  (zero pixels with alpha < 255) has no headroom above its earth-top row and is
  rejected with an `error` explaining that DEC-007 requires transparent headroom
  (tuft overhang) above the earth-top row.
- AC-6 (**earth-top rule — surface-cap**): The earth-top row `T` is the first
  row from the top (y = 0 upward) that is fully opaque across the whole width
  (every pixel alpha == 255), and every row y ≥ T must also be fully opaque.
  Any pixel with alpha < 255 at y ≥ T is an `error` ("transparent hole below the
  earth-top row"). If no row is fully opaque across the full width, `tile`
  reports an `error` ("no earth-top row found"). Rows y < T (the headroom) are
  free-form: tufts may be transparent, partial, or opaque and are subject to no
  rule other than AC-5. The definition of `T` stays deterministic and
  unambiguous even when a headroom row is itself fully opaque full-width: `T`
  then resolves to that higher row, and any transparency below it fails the rule
  above. `bodyHeight` (= fileHeight − `T`) is computed and reported.
- AC-7 (**horizontal seam continuity — surface-cap, rock, interior**): The tile
  content must continue across the left/right wrap (world column x = 0 of one
  copy abuts column x = W−1 of the previous copy) without a hard, visible
  discontinuity. The check is deterministic and threshold-based: it must accept
  hand-painted seamless textures (no per-pixel edge equality is required) and
  must reject an obvious step/edge at the wrap that is not representative of the
  texture's interior variation. For `surface-cap`, the check applies to the
  fully opaque body rows (`T` .. height−1) **only**; the transparent
  headroom/tuft rows above the earth-top row are exempt and may differ at the
  wrap without failing. The exact metric and threshold values are an L2
  implementation decision; the applied thresholds and the measured value per
  axis appear in the report, and the pass/fail behavior must be proven by
  tests using clearly seamless (pass) and clearly non-seamless (fail) synthetic
  fixtures.
- AC-8 (**vertical seam continuity — interior only**): `interior` must
  additionally tile vertically: its content must continue across the top/bottom
  wrap (the bottom row of one copy abuts the top row of the next), using the
  same deterministic threshold-based check; a hard vertical seam is an `error`.
  `rock` and `surface-cap` are 1:1 vertically mapped per DEC-007 (no vertical
  repeat), so no vertical wrap check applies to them and none is performed.

### Output, sidecar, tests, docs

- AC-9 (**mirror copy and sidecar on ok**): When the verdict is `ok`, `tile`
  writes the tile PNG to its target path. The default target is the mirrored
  path: an input under the `assets-source/` root maps to the identical relative
  path under `assets/` (e.g. `assets-source/terrain/layers/surface-cap.png` →
  `assets/terrain/layers/surface-cap.png`); an explicit `--output <path>`
  overrides it; an input NOT under `assets-source/` without `--output` is a
  usage error. Directories are created as needed. The PNG write is a
  **byte-for-byte copy** of the source (no decode/re-encode, no scaling,
  cropping, or color/alpha transforms) so the shipped texture is bit-identical
  to the delivered art and the human's final visual sign-off is on the exact
  art. When the verdict is `needs-source-fix`, `tile` writes nothing (no PNG, no
  sidecar) — only the report.
- AC-10 (**metadata sidecar**): For every written PNG, `tile` writes a sidecar
  JSON `<stem>.json` next to it (same stem convention as ADR-002 sprite
  metadata), with machine-consumable fields for the future game-side loader:
  `name`/stem, `kind`, `width`, `height`, `earthTopRow` and `bodyHeight`
  (surface-cap only), `horizontalSeamless` (all kinds, the validated result),
  `verticalSeamless` (interior only). The sidecar contains **none** of the
  sprite metadata (`frameCount`, `frameWidth`, `frameHeight`, `fps`, `loop`,
  `pivot`).
- AC-11 (**automated tests**): The tile verdict logic is unit-tested in a new
  test project under `tests/` for the AssetProcessor (exact name/layout is the
  developer's L2 choice, following the existing xUnit conventions of
  `tests/Ratillery.Core.Tests`, registered in the solution). Tests use synthetic
  fixtures generated in code; **no real art files are required** to implement or
  verify this story. Required coverage at minimum:
  (a) seam prove-cases: clearly seamless horizontal passes for surface-cap body,
  rock, and interior; clearly non-seamless horizontal fails for each; clearly
  seamless vertical passes and clearly non-seamless vertical fails for interior;
  (b) rock/interior opacity: fully opaque RGB and fully opaque RGBA pass; a
  single pixel with alpha < 255 fails with the issue;
  (c) surface-cap earth-top: correct `T` detection including exact boundary
  rows, free-form headroom above `T` passes, a transparent pixel below `T`
  fails, a file with no fully opaque full-width row fails, a fully opaque cap
  fails, an RGB (no-alpha) cap fails;
  (d) surface-cap headroom is exempt from the horizontal seam check (a cap with
  a seamless body but non-seamless headroom passes);
  (e) `ok` ⇒ output written bit-identical to the input and sidecar content
  correct; `needs-source-fix` ⇒ nothing written;
  (f) determinism: the same fixture validated twice yields identical reports.
  `dotnet build` succeeds and `dotnet test` passes; all pre-existing tests
  (`tests/Ratillery.Core.Tests`) remain green; the existing `inspect`,
  `validate`, and `sheet` commands keep their current behavior and outputs, and
  existing processed sprite assets are not regenerated or altered by this story.
- AC-12 (**documentation**): Documentation updates land with the implementation:
  (a) `assets-source/README.md` gains a human-consumable "Terrain tiles
  (DEC-007)" delivery section covering the three files and mirrored paths, the
  per-kind layout rules (surface-cap: RGBA with transparent headroom above the
  earth-top row and a fully opaque body below; rock/interior: opaque full-bleed;
  seam axes: cap and rock horizontal-only, interior horizontal + vertical), and
  stating clearly that terrain layers are validated and mirrored as **whole
  textures** — never cropped, split, baseline-aligned, or given sprite metadata —
  so the generic sprite spec in that README does not apply to them;
  (b) `.opencode/skills/process-asset/SKILL.md` is updated so the asset-processor
  agent recognizes `assets-source/terrain/layers/*` and runs `tile` (not
  `validate`/`sheet`) for them, mapping `ok` → PROCESSED and
  `needs-source-fix` → NEEDS_SOURCE_FIX, preserving the
  PROCESSED/NEEDS_SOURCE_FIX/BLOCKED reporting contract, the no-edit rule, and
  the final human visual sign-off note;
  (c) the tool's usage/help text documents `tile` and its options;
  (d) ADR-002 is amended with a paragraph recording the new `tile` command
  (per-kind DEC-007 validation, byte-copy mirror + sidecar, new tool test
  project) as the L2 architecture record.

## Edge Cases

- **Sprite file run through `tile`**: any input whose stem is not one of the
  three canonical names (`idle.png`, `rock-copy.png`, …) is a deterministic
  usage error; `tile` never guesses a kind.
- **Mistaken file placement**: a sprite wrongly named `rock.png` is validated as
  a rock tile and rejected by the opacity rule (its transparent margins contain
  alpha < 255 pixels) — deterministic, no false `ok`.
- **Fully opaque surface-cap** (RGBA all-255 or RGB): rejected per AC-5 — the
  DEC-007 cap model requires transparent headroom (tuft overhang) above the
  earth-top row; a flat, tuft-less cap contradicts the binding delivery model and
  usually signals a flattened alpha.
- **Full-width opaque tuft row above the body**: `T` resolves to that row (the
  definition is "first fully opaque full-width row", deterministically); if any
  transparency exists below it (e.g. a gap before the real body top), the file
  is rejected as a hole below the earth-top row. This is the documented,
  unambiguous reading of DEC-007, not a guess.
- **Earth-top at the canvas top** (y = 0): only possible when no transparency
  exists → rejected per AC-5. **Earth-top at the very bottom row**: structurally
  allowed (bodyHeight 1) — no minimum body thickness is validated (band
  thicknesses are engine-config, matched later per DEC-007).
- **Cap headroom differences at the wrap**: tufts may begin or end near the left
  or right edge; the horizontal seam check ignores rows above `T` (AC-7).
- **Interior with a vertical-only or horizontal-only hard seam**: fails the
  corresponding check; a uniform solid-color tile is trivially seamless on both
  axes and passes — correct, since it tiles perfectly.
- **Seam false positives on hand-painted textures**: the check is threshold-based
  by design (AC-7/AC-8); the synthetic fixtures must prove clean separation of
  clearly-seamless vs clearly-non-seamless verdicts. Metric and threshold tuning
  is the developer's L2 decision; edge-case fixtures that are *almost* seamless
  are not required to pass or fail — only the clearly separable prove-cases are.
- **Rock/interior with subtle alpha artifacts** (any pixel below 255): fails the
  exact opacity rule — the artist must deliver at full opacity; there is no
  alpha threshold fudge for tiles.
- **RGB (no alpha) rock/interior**: accepted (opaque by construction); RGB
  surface-cap: rejected (AC-5).
- **Output handling**: target directory missing → created (as `sheet` does);
  target file already present → overwritten (pipeline outputs are derived data);
  the derived mirror mapping is applied after normalizing path separators, and
  the tool invocation assumes the repo-root working directory, consistent with
  the existing documented examples.
- **Non-PNG, corrupt, or missing input**: clear error message and nonzero exit,
  consistent with the current commands.
- **Real-art independence**: this story does not require, process, or gate on the
  real terrain files (including any partial or in-progress delivery already
  sitting under `assets-source/terrain/layers/`); the tool and its tests are
  fully exercisable with synthetic fixtures. Processing an actual human delivery
  happens later through the asset-processor agent flow, on human request.

## Scope boundaries

### In scope

- The `tile` command in `tools/Ratillery.AssetProcessor`: per-kind validation
  (AC-4..AC-8), the verdict/report contract (AC-1..AC-3), byte-copy mirror and
  metadata sidecar (AC-9, AC-10), usage/help text.
- A new unit-test project under `tests/` for the AssetProcessor tile logic with
  synthetic fixtures (AC-11).
- Documentation: `assets-source/README.md` terrain-tiles section, the
  `process-asset` agent skill update, tool usage/help, and an ADR-002 amendment
  paragraph (AC-12).

### Out of scope

- Any game-side change in `src/Ratillery.Game` or `src/Ratillery.Core`:
  swapping the DEC-002 placeholder terrain for the real textures (loading,
  sampling, earth-top alignment to H(x), seam-aware tiling, band-thickness
  matching against `TerrainConfig`) is a **separate future story** and must not
  be implemented here.
- Changing the behavior of the existing `inspect`, `validate`, or `sheet`
  commands or any existing sprite output/metadata.
- Generating or editing art, and any pixel transform (crop, scale, color,
  alpha, re-encode).
- Engine-side sampling semantics (depth-below-surface vs world-anchored tiling).
- Milestone slices 3–8 content; milestone progress is unaffected (this is a
  tooling story, not a slice).
- Adding seam/seamless properties or earth-top awareness to any existing JSON
  schema beyond the new tile sidecar.

## Dependencies

- ADR-002 / DEC-005 / DEC-006 (asset pipeline and delivery roles): the flow this
  command plugs into; ADR-002 is amended by this story.
- ADR-003 (whole-`assets/` mirroring into the game output): the mirrored terrain
  textures will automatically reach the game at build time once produced; no
  runtime change here.
- DEC-007 (binding terrain delivery model — the validation rules this story
  encodes; recorded in `docs/status.md` and RAT-002).
- DEC-002 (placeholders until real art) and RAT-002 (done): the real-art swap
  this tooling enables is the RAT-002 follow-up, out of scope here.
- No dependency on unreleased stories; existing tests must stay green.

## Open Questions

- None.

## QA History

- (appended by the orchestrator/developer from QA reports)

### TOOL-001 — QA cycle 1: PASS (2026-09-06)

QA verdict: **PASS** (branch `feat/TOOL-001-asset-processor-tile-mode`, uncommitted
working tree). All AC-1..AC-12 HOLDS. `dotnet build` 0 warnings/0 errors;
`dotnet test` 61/61 green (27 Core + 34 new Ratillery.AssetProcessor.Tests).
Independent CLI verification with QA-owned synthetic fixtures in a temp sandbox
(never in the repo): happy paths for the three kinds (ok, exit 0, correct
earthTopRow/bodyHeight and seam reports); all failure paths (non-seamless
horizontal per kind, non-seamless vertical interior, single alpha-254 pixel in
rock, surface-cap RGB / fully-opaque / hole-below-T / no-earth-top); wrong stem
and outside-source usage errors (exit 1, nothing written); `--output` override
(PNG hash identical to source — byte-copy confirmed); default mirror mapping
verified in sandbox; determinism (byte-identical reports/outputs on repeat);
needs-source-fix writes nothing (8 fixtures); regression: `validate` on the raw
idle sprite unchanged (exit 0, ok), help documents `tile`, no sprite assets
regenerated, `src/` and `assets/` untouched, raw art never processed.
Non-blocking observations: shared `ReadPngHeader` lives in `TileProcessing`
(cosmetic); console summary uses OS culture while JSON is invariant;
`ratio` is null in the floor-only seam case; default mirror assumes repo-root
cwd (documented; `--output` disambiguates).
