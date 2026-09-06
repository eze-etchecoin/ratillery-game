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

            examples:
              dotnet run --project tools/Ratillery.AssetProcessor -- inspect assets-source/rats/base/idle.png --frames 8
              dotnet run --project tools/Ratillery.AssetProcessor -- sheet assets-source/rats/base/idle.png --frames 8 --output assets/sprites/rats/base/idle.png --frames-dir assets/sprites/rats/base/frames --name rat-idle
            """);
        return 0;
    }

    private sealed class ArgSet
    {
        private readonly List<string> _positionals = new();
        private readonly Dictionary<string, string> _options = new(StringComparer.Ordinal);

        public static ArgSet Parse(IEnumerable<string> tokens)
        {
            var set = new ArgSet();
            using IEnumerator<string> e = tokens.GetEnumerator();
            while (e.MoveNext())
            {
                string token = e.Current;
                if (token.StartsWith("--", StringComparison.Ordinal))
                {
                    string name = token[2..];
                    if (!e.MoveNext())
                    {
                        throw new ArgumentException($"missing value for --{name}");
                    }
                    set._options[name] = e.Current;
                }
                else
                {
                    set._positionals.Add(token);
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
    }
}
