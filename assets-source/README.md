# assets-source — Raw asset delivery

This folder holds raw, human/generated originals (sprites, animations,
props, icons, terrain, UI art). Per ADR-002:

- Drop new raw files here, then process them into `assets/` with
  `tools/Ratillery.AssetProcessor`. Never hand-edit or commit processed
  results under this folder; reprocess instead.
- Raw files are never modified after delivery. Re-run the pipeline if the
  output needs to change.

## Processing

```bash
# Inspect a file first: reports content bounds and frame candidates.
dotnet run --project tools/Ratillery.AssetProcessor -- inspect <input> [--frames N]

# Normalize an ANIMATED strip of N frames.
dotnet run --project tools/Ratillery.AssetProcessor -- sheet <input> --frames N --output <dest>.png --frames-dir <dest-dir> --name <name>

# Normalize a SINGLE-frame sprite (object/weapon/prop/icon): crops margins.
dotnet run --project tools/Ratillery.AssetProcessor -- sheet <input> --frames 1 --output <dest>.png --name <name>
```

Every `sheet` run also writes `<dest>.json` (name, frameCount, frameWidth,
frameHeight, fps, loop, pivot). Frames are split automatically from the alpha
silhouette, so a clean transparent gutter between frames guarantees a correct
crop. Playback `fps` is data (passed via `--fps`), not something derived from
the image.

## Generic source spec

Use this spec for any subject you deliver here — animated character, weapon,
object, icon, prop — so it survives the pipeline without manual fixes.

### Applies to everything (single-frame and animated)

- **Real transparency.** RGBA PNG with actual alpha; no background fill and no
  chroma-key green/magenta. The tool cannot cleanly separate an illustrated
  subject from a flattened background.
- **Fully contained subject.** Nothing touches, clips, or leaves the canvas or
  cell edges. Keep at least ~8 px of transparent margin on all four sides of
  every cell.
- **No stray external effects.** Shadows, glow, trails, particles, or
  attachments (muzzle flashes, dropped objects) stay inside the subject's own
  margin and never bleed into a neighbor's space.
- **Clean antialiasing.** Fade to transparency inside the subject's cell; no
  hard or haloed borders where pixels end.
- **Exact dimensions on a clean grid.** Give precise integer pixel sizes. For a
  single frame, any size is fine; for a strip, see below.

### Applies to animated strips

- One horizontal row of **N equal frames**; total width divisible by N
  (frame width = width / N). No rows/columns of different sizes.
- A transparent **gutter of at least ~24 px** between the visible content of
  consecutive frames. Subjects must never touch or overlap their neighbors.
- Every frame contains the **full subject**: trailing parts (tails, capes,
  hair, recoil, smoke) must end well inside their own cell.
- Keep the subject in the **same horizontal region** and on the **same ground
  line / baseline** across all frames, so playback does not jump.
- Frame order is left to right = playback order. Playback speed is provided
  separately as metadata (`fps`).

### Typical AI-generation failures to avoid

- Contact-sheet strips where frames touch or overlap (no gutter) — impossible
  to crop cleanly, because parts of one subject land inside the neighbor's
  cell.
- Off-grid or unequal frames, or a width not divisible by the frame count.
- Background painted instead of alpha; green-screen output; edge halos.
- Tails/limbs/shadow that extend past a subject's own cell into the next.

When a delivery looks risky, run `inspect` before processing to confirm the
splits land in empty gutter; adjust the source, not the output.
