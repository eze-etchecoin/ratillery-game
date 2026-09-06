using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Ratillery.AssetProcessor;

namespace Ratillery.AssetProcessor;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            return args[0] switch
            {
                "inspect" => Inspect(args.Skip(1).ToArray()),
                "sheet" => Sheet(args.Skip(1).ToArray()),
                "validate" => Validate(args.Skip(1).ToArray()),
                "help" or "--help" or "-h" => PrintUsage(),
                _ => Unknown(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int Inspect(string[] args)
    {
        ArgSet a = ArgSet.Parse(args);
        string input = a.RequiredPositional(0, "inspect <input> requires an image path");
        int threshold = a.OptionInt("alpha-threshold", 10);
        int minRunWidth = a.OptionInt("min-run-width", 80);

        using Image<Rgba32> image = Image.Load<Rgba32>(input);
        int[] top = SpriteAnalysis.ColumnTop(image, threshold);
        (int left, int topY, int right, int bottom) = SpriteAnalysis.ContentBounds(image, threshold);

        Console.WriteLine($"image: {image.Width}x{image.Height}");
        Console.WriteLine($"content: x {left}..{right}, y {topY}..{bottom} (bbox {right - left + 1}x{bottom - topY + 1})");

        int? frames = a.OptionIntNullable("frames");
        if (frames is int n)
        {
            (int[] Separators, int Cut) split = SpriteAnalysis.FindSplit(top, n, minRunWidth);
            PrintSplit(image, split.Separators, split.Cut, threshold);
        }
        else
        {
            Console.WriteLine("wide-run counts per cut (frames where a run >= min-run-width):");
            var buckets = new Dictionary<int, int>();
            int minTop = top.Where(t => t >= 0).DefaultIfEmpty(int.MaxValue).Min();
            int maxTop = top.Where(t => t >= 0).Max();
            for (int cut = minTop; cut <= maxTop; cut++)
            {
                buckets[SpriteAnalysis.WideRuns(top, cut, minRunWidth).Count] = cut;
            }

            foreach (KeyValuePair<int, int> kv in buckets.OrderBy(k => k.Key))
            {
                Console.WriteLine($"  {kv.Key} run(s): stable up to cut y={kv.Value}");
            }
        }

        return 0;
    }

    private static void PrintSplit(Image<Rgba32> image, int[] separators, int cut, int threshold)
    {
        Console.WriteLine($"split: cut y={cut}, {separators.Length + 1} frames");
        if (separators.Length > 0)
        {
            Console.WriteLine($"separators: x {string.Join(", ", separators)}");
        }

        FrameBounds[] boxes = SpriteAnalysis.FrameBoxes(image, separators, threshold);
        for (int k = 0; k < boxes.Length; k++)
        {
            int regionLeft = k == 0 ? 0 : separators[k - 1];
            int regionRight = k == boxes.Length - 1 ? image.Width : separators[k];
            Console.WriteLine($"  frame {k}: region x {regionLeft}..{regionRight - 1}, bbox {boxes[k]}");
        }
    }

    private static int Sheet(string[] args)
    {
        ArgSet a = ArgSet.Parse(args);
        string input = a.RequiredPositional(0, "sheet <input> requires an image path");
        int frameCount = a.OptionInt("frames", 0);
        if (frameCount < 1)
        {
            throw new ArgumentException("--frames N (>=1) is required");
        }

        string output = a.RequiredOption("output", "--output <path> is required");
        int threshold = a.OptionInt("alpha-threshold", 10);
        int minRunWidth = a.OptionInt("min-run-width", 80);
        int padding = a.OptionInt("padding", 4);
        int fps = a.OptionInt("fps", 8);
        string name = a.Option("name", Path.GetFileNameWithoutExtension(output));

        using Image<Rgba32> image = Image.Load<Rgba32>(input);
        int[] top = SpriteAnalysis.ColumnTop(image, threshold);
        (int[] Separators, int Cut) split = SpriteAnalysis.FindSplit(top, frameCount, minRunWidth);
        FrameBounds[] boxes = SpriteAnalysis.FrameBoxes(image, split.Separators, threshold);
        int count = boxes.Length;

        int maxWidth = boxes.Max(b => b.Width);
        int maxHeight = boxes.Max(b => b.Height);
        int cellWidth = maxWidth + 2 * padding;
        int cellHeight = maxHeight + padding;

        using Image<Rgba32> sheet = new(cellWidth * count, cellHeight);
        for (int k = 0; k < count; k++)
        {
            int destX = (cellWidth - boxes[k].Width) / 2;
            int destY = cellHeight - boxes[k].Height;
            SpriteAnalysis.CopyRegion(image, sheet, boxes[k], k * cellWidth + destX, destY);
        }

        string? outputDir = Path.GetDirectoryName(output);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        sheet.SaveAsPng(output);

        string metadataPath = a.Option("metadata", Path.Combine(
            Path.GetDirectoryName(output) ?? ".",
            Path.GetFileNameWithoutExtension(output) + ".json"));
        WriteMetadata(metadataPath, name, count, cellWidth, cellHeight, fps);

        string? framesDir = a.OptionNullablePath("frames-dir");
        if (framesDir is not null)
        {
            Directory.CreateDirectory(framesDir);
            string stem = Path.GetFileNameWithoutExtension(output);
            for (int k = 0; k < count; k++)
            {
                using Image<Rgba32> frame = new(cellWidth, cellHeight);
                int destX = (cellWidth - boxes[k].Width) / 2;
                int destY = cellHeight - boxes[k].Height;
                SpriteAnalysis.CopyRegion(image, frame, boxes[k], destX, destY);
                frame.SaveAsPng(Path.Combine(framesDir, $"{stem}_{k:00}.png"));
            }
        }

        Console.WriteLine($"input:   {input}");
        Console.WriteLine($"split:   {count} frames, cut y={split.Cut}");
        Console.WriteLine($"cells:   {cellWidth}x{cellHeight} each (max content {maxWidth}x{maxHeight}, padding {padding})");
        Console.WriteLine($"output:  {output} ({cellWidth * count}x{cellHeight})");
        Console.WriteLine($"metadata:{metadataPath}");
        if (framesDir is not null)
        {
            Console.WriteLine($"frames:  {framesDir}/");
        }

        return 0;
    }

    private static void WriteMetadata(
        string path,
        string name,
        int frameCount,
        int frameWidth,
        int frameHeight,
        int fps)
    {
        object metadata = new
        {
            name,
            frameCount,
            frameWidth,
            frameHeight,
            fps,
            loop = true,
            pivot = new { x = 0.5f, y = 1.0f },
        };

        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        File.WriteAllText(path, json + Environment.NewLine);
    }

    private static int Validate(string[] args)
    {
        ArgSet a = ArgSet.Parse(args);
        string input = a.RequiredPositional(0, "validate <input> requires an image path");
        int threshold = a.OptionInt("alpha-threshold", 10);
        int minRunWidth = a.OptionInt("min-run-width", 80);
        int marginMin = a.OptionInt("margin-min", 4);
        int gutterMin = a.OptionInt("gutter-min", 8);
        int baselineTol = a.OptionInt("baseline-tol", 8);
        int? requestedFrames = a.OptionIntNullable("frames");
        bool jsonOut = a.Has("json");
        string? reportPath = a.OptionNullablePath("report");

        PngHeader header = ReadPngHeader(input);

        using Image<Rgba32> image = Image.Load<Rgba32>(input);
        bool fullyOpaque = IsFullyOpaque(image);
        int[] top = SpriteAnalysis.ColumnTop(image, threshold);
        (int left, int topY, int right, int bottom) = SpriteAnalysis.ContentBounds(image, threshold);

        var problems = new List<Problem>();

        int minTop = int.MaxValue;
        int maxTop = int.MinValue;
        foreach (int t in top)
        {
            if (t < 0)
            {
                continue;
            }
            if (t < minTop) minTop = t;
            if (t > maxTop) maxTop = t;
        }

        int maxWide = 0;
        for (int cut = minTop; cut <= maxTop; cut++)
        {
            int count = SpriteAnalysis.WideRuns(top, cut, minRunWidth).Count;
            if (count > maxWide)
            {
                maxWide = count;
            }
        }

        string kind;
        int chosen;
        if (requestedFrames is int n)
        {
            chosen = Math.Max(n, 1);
            kind = n > 1 ? "strip" : "single";
            if (kind == "single" && maxWide > 1)
            {
                problems.Add(new Problem("warning", $"image looks like a strip of {maxWide}+ subjects; if this is an animation, pass --frames <N>"));
            }
        }
        else if (maxWide > 1)
        {
            kind = "strip";
            var strips = new List<(int Count, int MinGap)>();
            for (int cnt = 2; cnt <= 12; cnt++)
            {
                if (TrySplitMinGap(top, image, cnt, minRunWidth, threshold) is (int count, int minGap))
                {
                    strips.Add((count, minGap));
                }
            }

            var good = strips.Where(s => s.MinGap >= gutterMin).OrderByDescending(s => s.Count).ToList();
            chosen = good.Count > 0
                ? good[0].Count
                : (strips.Count > 0 ? strips.OrderByDescending(s => s.MinGap).First().Count : 0);
        }
        else
        {
            kind = "single";
            chosen = 1;
        }

        int[]? separators = null;
        FrameBounds[]? boxes = null;
        string? splitError = null;
        if (chosen > 0)
        {
            try
            {
                (int[] Separators, int Cut) split = SpriteAnalysis.FindSplit(top, chosen, minRunWidth);
                if (split.Separators.Length + 1 != chosen)
                {
                    throw new InvalidOperationException("silhouette split did not yield the expected frame count");
                }
                separators = split.Separators;
                boxes = SpriteAnalysis.FrameBoxes(image, separators, threshold);
            }
            catch (InvalidOperationException ex)
            {
                splitError = ex.Message;
            }
        }

        List<FrameInfo> frames = new();
        int minGutter = int.MaxValue;
        int maxGutter = 0;
        int baselineDrift = 0;

        if (boxes is not null)
        {
            int width = image.Width;
            for (int k = 0; k < boxes.Length; k++)
            {
                int regionLeft = k == 0 ? 0 : separators![k - 1];
                int regionRight = k == boxes.Length - 1 ? width : separators![k];
                int boxRight = boxes[k].Left + boxes[k].Width - 1;
                int boxBottom = boxes[k].Top + boxes[k].Height - 1;
                frames.Add(new FrameInfo(
                    k,
                    new Rc(regionLeft, 0, regionRight - 1, image.Height - 1),
                    new Rc(boxes[k].Left, boxes[k].Top, boxRight, boxBottom),
                    boxes[k].Left - regionLeft,
                    regionRight - 1 - boxRight));
            }

            int[] bottoms = boxes.Select(b => b.Top + b.Height - 1).ToArray();
            baselineDrift = bottoms.Max() - bottoms.Min();

            for (int k = 0; k < boxes.Length - 1; k++)
            {
                int gap = boxes[k + 1].Left - (boxes[k].Left + boxes[k].Width - 1) - 1;
                if (gap < minGutter) minGutter = gap;
                if (gap > maxGutter) maxGutter = gap;
            }
        }

        if (!header.HasAlpha)
        {
            problems.Add(new Problem("error", "no alpha channel (PNG color type " + header.ColorType + "); deliver RGBA PNG with transparent background per assets-source/README.md"));
        }
        else if (fullyOpaque)
        {
            problems.Add(new Problem("warning", "no fully transparent pixels found; the subject may have a flattened background"));
        }

        if (kind == "single")
        {
            Margins canvas = boxes is null
                ? new Margins(0, 0, 0, 0)
                : new Margins(boxes[0].Left, boxes[0].Top, header.Width - 1 - (boxes[0].Left + boxes[0].Width - 1), header.Height - 1 - (boxes[0].Top + boxes[0].Height - 1));
            if (canvas.Left < marginMin) problems.Add(new Problem("error", $"content reaches the left canvas edge ({canvas.Left}px); keep >= {marginMin}px transparent margin"));
            if (canvas.Top < marginMin) problems.Add(new Problem("error", $"content reaches the top canvas edge ({canvas.Top}px); keep >= {marginMin}px transparent margin"));
            if (canvas.Right < marginMin) problems.Add(new Problem("error", $"content reaches the right canvas edge ({canvas.Right}px); keep >= {marginMin}px transparent margin"));
            if (canvas.Bottom < marginMin) problems.Add(new Problem("error", $"content reaches the bottom canvas edge ({canvas.Bottom}px); keep >= {marginMin}px transparent margin"));
        }
        else
        {
            if (chosen == 0 || boxes is null)
            {
                problems.Add(new Problem("error", "could not isolate any frame split; frames likely overlap or touch — add transparent gutters between subjects (assets-source/README.md)"));
            }
            else if (splitError is not null)
            {
                problems.Add(new Problem("error", $"cannot isolate {chosen} frame(s): {splitError}"));
            }
            else
            {
                if (minGutter < gutterMin)
                {
                    problems.Add(new Problem("error", $"frames touch or overlap: only {minGutter} empty px between subjects (need >= {gutterMin}); add transparent gutters"));
                }
                else if (minGutter < 24)
                {
                    problems.Add(new Problem("warning", $"gutter between frames is only {minGutter}px (recommended >= 24px)"));
                }

                if (baselineDrift > baselineTol)
                {
                    problems.Add(new Problem("error", $"feet baseline drifts {baselineDrift}px across frames (tolerance {baselineTol}px); align ground contact in the source"));
                }

                foreach (FrameInfo f in frames)
                {
                    if (f.MarginLeft < marginMin || f.MarginRight < marginMin)
                    {
                        problems.Add(new Problem("warning", $"frame {f.Index} content sits within {Math.Min(f.MarginLeft, f.MarginRight)}px of its split boundary"));
                    }
                    if (f.Box.Top == 0)
                    {
                        problems.Add(new Problem("warning", $"frame {f.Index} touches the top canvas edge (possible clipping)"));
                    }
                }
            }
        }

        string verdict = problems.Any(p => p.Severity == "error") ? "needs-source-fix" : "ok";
        int? reportFrameCount = kind == "single" ? 1 : chosen > 0 ? chosen : null;

        object report = new
        {
            input,
            width = header.Width,
            height = header.Height,
            colorType = header.ColorType,
            hasAlpha = header.HasAlpha,
            fullyOpaque,
            content = new Rc(left, topY, right, bottom),
            kind,
            frameCount = reportFrameCount,
            frames,
            gutter = kind == "single" ? null : boxes is null ? null : new Extents(minGutter, maxGutter),
            baselineDrift,
            thresholds = new { marginMin, gutterMin, baselineTol, alphaThreshold = threshold },
            verdict,
            issues = problems,
        };

        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        if (!jsonOut)
        {
            Console.WriteLine($"image: {header.Width}x{header.Height}, colorType {header.ColorType}, hasAlpha {header.HasAlpha}, fullyOpaque {fullyOpaque}");
            Console.WriteLine($"content: x {left}..{right}, y {topY}..{bottom}");
            Console.WriteLine($"kind: {kind} (frames: {(reportFrameCount?.ToString() ?? "none")})");
            if (kind != "single" && boxes is not null)
            {
                Console.WriteLine($"gutter: {minGutter}..{maxGutter}px empty between frames; baseline drift {baselineDrift}px");
            }
            Console.WriteLine($"verdict: {verdict}");
            foreach (Problem p in problems)
            {
                Console.WriteLine($"  [{p.Severity}] {p.Message}");
            }
        }
        else
        {
            Console.WriteLine(json);
        }

        if (reportPath is not null)
        {
            string? dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(reportPath, json + Environment.NewLine);
        }

        return 0;
    }

    private static (int Count, int MinGap)? TrySplitMinGap(int[] top, Image<Rgba32> image, int count, int minRunWidth, int threshold)
    {
        try
        {
            (int[] Separators, int Cut) split = SpriteAnalysis.FindSplit(top, count, minRunWidth);
            if (split.Separators.Length + 1 != count)
            {
                return null;
            }
            FrameBounds[] boxes = SpriteAnalysis.FrameBoxes(image, split.Separators, threshold);
            int minGap = int.MaxValue;
            for (int k = 0; k < boxes.Length - 1; k++)
            {
                int gap = boxes[k + 1].Left - (boxes[k].Left + boxes[k].Width - 1) - 1;
                if (gap < minGap)
                {
                    minGap = gap;
                }
            }
            return (count, minGap);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool IsFullyOpaque(Image<Rgba32> image)
    {
        bool translucent = false;
        for (int y = 0; y < image.Height && !translucent; y++)
        {
            image.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A < 255)
                    {
                        translucent = true;
                        return;
                    }
                }
            });
        }
        return !translucent;
    }

    private static PngHeader ReadPngHeader(string input)
    {
        byte[] head = new byte[33];
        using FileStream fs = File.OpenRead(input);
        int read = 0;
        while (read < head.Length)
        {
            int n = fs.Read(head, read, head.Length - read);
            if (n <= 0)
            {
                break;
            }
            read += n;
        }

        if (read < head.Length ||
            head[0] != 0x89 || head[1] != 0x50 || head[2] != 0x4E || head[3] != 0x47)
        {
            throw new ArgumentException("only PNG images are supported (validation reads the alpha/color-type header)");
        }

        return new PngHeader(
            Be32(head, 16),
            Be32(head, 20),
            head[25],
            head[25] is 4 or 6);
    }

    private static int Be32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'");
        PrintUsage();
        return 2;
    }

    private static int PrintUsage()
    {
        Console.WriteLine(
            """
            Ratillery.AssetProcessor — deterministic sprite-sheet normalization.

            usage:
              dotnet run --project tools/Ratillery.AssetProcessor -- <command> [options]

            commands:
              inspect <input> [--frames N] [--alpha-threshold N] [--min-run-width N]
                  Report content bounds and the alpha-silhouette frame split.
              validate <input> [--frames N] [--json] [--report <path>] [options]
                  Deterministic processability verdict: RGBA/alpha, single vs
                  strip, frame isolation, gutters, margins, baseline drift.
              sheet <input> --frames N --output <path> [options]
                  Split a horizontal sprite strip into N frames, crop each to its
                  alpha content, bottom-align on a shared baseline, and write a
                  normalized sheet + metadata JSON.

            sheet options:
              --output <path>          sheet output (also names the .json metadata)
              --metadata <path>        metadata JSON output (default: output with .json)
              --frames-dir <dir>       emit individual frames <stem>_00.png ...
              --name <string>          metadata name (default: output file stem)
              --fps <int>              playback fps recorded in metadata (default 8)
              --padding <int>          transparent px around content (default 4)
              --alpha-threshold <int>  alpha cutoff for "visible" pixels (default 10)
              --min-run-width <int>    min px a silhouette run must span to count (default 80)

            validate options:
              --frames N               expected frames for a strip (else auto-detect)
              --json                   print the report as a single JSON object
              --report <path>          also write the JSON report to a file
              --margin-min <int>       min transparent px at a subject edge (default 4)
              --gutter-min <int>       min empty px between strip frames (default 8)
              --baseline-tol <int>     max px of feet-baseline drift (default 8)
              --alpha-threshold <int>  alpha cutoff for "visible" pixels (default 10)
              --min-run-width <int>    min px a silhouette run must span to count (default 80)

            examples:
              dotnet run --project tools/Ratillery.AssetProcessor -- inspect assets-source/rats/base/idle.png --frames 8
              dotnet run --project tools/Ratillery.AssetProcessor -- validate assets-source/rats/base/idle.png --frames 8 --json
              dotnet run --project tools/Ratillery.AssetProcessor -- sheet assets-source/rats/base/idle.png --frames 8 --output assets/sprites/rats/base/idle.png --frames-dir assets/sprites/rats/base/frames --name rat-idle
            """);
        return 0;
    }

    private sealed record Rc(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left + 1;
        public int Height => Bottom - Top + 1;
    }

    private sealed record Margins(int Left, int Top, int Right, int Bottom);

    private sealed record Extents(int Min, int Max);

    private sealed record Problem(string Severity, string Message);

    private sealed record FrameInfo(int Index, Rc Region, Rc Box, int MarginLeft, int MarginRight);

    private sealed record PngHeader(int Width, int Height, int ColorType, bool HasAlpha);

    private sealed class ArgSet
    {
        private readonly List<string> _positionals = new();
        private readonly Dictionary<string, string> _options = new(StringComparer.Ordinal);
        private readonly HashSet<string> _flags = new(StringComparer.Ordinal);

        public static ArgSet Parse(IEnumerable<string> tokens)
        {
            var set = new ArgSet();
            string[] ts = tokens as string[] ?? tokens.ToArray();
            for (int i = 0; i < ts.Length; i++)
            {
                string token = ts[i];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    set._positionals.Add(token);
                    continue;
                }

                string name = token[2..];
                if (i + 1 < ts.Length && !ts[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    set._options[name] = ts[++i];
                }
                else
                {
                    set._flags.Add(name);
                }
            }

            return set;
        }

        public string RequiredPositional(int index, string message) =>
            _positionals.Count > index ? _positionals[index] : throw new ArgumentException(message);

        public string Option(string name, string fallback) =>
            _options.TryGetValue(name, out string? value) ? value : fallback;

        public string RequiredOption(string name, string message) =>
            _options.TryGetValue(name, out string? value) ? value : throw new ArgumentException(message);

        public int OptionInt(string name, int fallback)
        {
            if (!_options.TryGetValue(name, out string? value))
            {
                return fallback;
            }
            return int.TryParse(value, out int parsed) ? parsed : throw new ArgumentException($"--{name} expects an integer");
        }

        public int? OptionIntNullable(string name) =>
            _options.TryGetValue(name, out string? value) && int.TryParse(value, out int parsed) ? parsed : null;

        public string? OptionNullablePath(string name) =>
            _options.TryGetValue(name, out string? value) ? value : null;

        public bool Has(string name) => _flags.Contains(name) || _options.ContainsKey(name);
    }
}
