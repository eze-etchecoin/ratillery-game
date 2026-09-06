using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor;

public enum TileKind
{
    SurfaceCap,
    Rock,
    Interior,
}

public sealed record TileIssue(string Severity, string Message);

public sealed record TileSeamCheck(
    bool Pass,
    double EdgeMeanDiff,
    double InteriorMeanDiff,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Ratio);

public sealed record TileReport(
    string Input,
    string Kind,
    int Width,
    int Height,
    int ColorType,
    bool HasAlpha,
    int? EarthTopRow,
    int? BodyHeight,
    TileSeamCheck? HorizontalSeam,
    TileSeamCheck? VerticalSeam,
    object Thresholds,
    string? Output,
    string Verdict,
    IReadOnlyList<TileIssue> Issues);

public sealed record PngHeader(int Width, int Height, int ColorType, bool HasAlpha);

public static class TileProcessing
{
    public const double SeamAbsoluteFloor = 6.0;
    public const double SeamRatioLimit = 3.0;

    public static string[] AcceptedStems => new[] { "surface-cap", "rock", "interior" };

    public static TileKind? KindFromStem(string stem) => stem switch
    {
        "surface-cap" => TileKind.SurfaceCap,
        "rock" => TileKind.Rock,
        "interior" => TileKind.Interior,
        _ => null,
    };

    public static string KindName(TileKind kind) => kind switch
    {
        TileKind.SurfaceCap => "surface-cap",
        TileKind.Rock => "rock",
        TileKind.Interior => "interior",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static object ThresholdsPayload => new
    {
        seamAbsoluteFloor = SeamAbsoluteFloor,
        seamRatioLimit = SeamRatioLimit,
    };

    public static string? MirrorTarget(string input)
    {
        string normalized = input.Replace('\\', '/');
        int index = normalized.IndexOf("assets-source/", StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }
        return "assets/" + normalized[(index + "assets-source/".Length)..];
    }

    public static TileReport Validate(string input, TileKind kind, string? outputTarget)
    {
        PngHeader header = ReadPngHeader(input);
        using Image<Rgba32> image = Image.Load<Rgba32>(input);

        var issues = new List<TileIssue>();
        int? earthTopRow = null;
        int? bodyHeight = null;
        TileSeamCheck? horizontal = null;
        TileSeamCheck? vertical = null;

        if (kind == TileKind.SurfaceCap)
        {
            if (!header.HasAlpha)
            {
                issues.Add(new TileIssue("error",
                    $"no alpha channel (PNG color type {header.ColorType}); surface-cap requires RGBA with transparent headroom (tuft overhang) above the earth-top row (DEC-007)"));
            }
            else
            {
                bool[] rowOpaque = FullyOpaqueRows(image);
                int t = FirstTrue(rowOpaque);
                if (t < 0)
                {
                    issues.Add(new TileIssue("error",
                        "no earth-top row found: no row is fully opaque across the full width"));
                }
                else if (t == 0 && AllTrue(rowOpaque))
                {
                    issues.Add(new TileIssue("error",
                        "fully opaque surface-cap: DEC-007 requires transparent headroom (tuft overhang) above the earth-top row"));
                }
                else
                {
                    if (HasTransparentPixelFrom(image, t))
                    {
                        issues.Add(new TileIssue("error",
                            $"transparent hole below the earth-top row (first at y={FirstTransparentRowFrom(image, t)}); rows y >= earth-top row must be fully opaque"));
                    }
                    else
                    {
                        earthTopRow = t;
                        bodyHeight = image.Height - t;
                        horizontal = CheckHorizontalSeam(image, t, image.Height - 1);
                        if (!horizontal.Pass)
                        {
                            issues.Add(new TileIssue("error", SeamIssueMessage("left/right", horizontal)));
                        }
                    }
                }
            }
        }
        else
        {
            if (header.HasAlpha)
            {
                int nonOpaque = CountNonOpaque(image);
                if (nonOpaque > 0)
                {
                    issues.Add(new TileIssue("error",
                        $"not fully opaque: {nonOpaque} pixel(s) with alpha < 255; rock/interior tiles must be fully opaque (DEC-007)"));
                }
            }

            horizontal = CheckHorizontalSeam(image, 0, image.Height - 1);
            if (!horizontal.Pass)
            {
                issues.Add(new TileIssue("error", SeamIssueMessage("left/right", horizontal)));
            }

            if (kind == TileKind.Interior)
            {
                vertical = CheckVerticalSeam(image);
                if (!vertical.Pass)
                {
                    issues.Add(new TileIssue("error", SeamIssueMessage("top/bottom", vertical)));
                }
            }
        }

        string verdict = issues.Any(i => i.Severity == "error") ? "needs-source-fix" : "ok";
        string? output = verdict == "ok" ? outputTarget : null;

        return new TileReport(
            input,
            KindName(kind),
            header.Width,
            header.Height,
            header.ColorType,
            header.HasAlpha,
            earthTopRow,
            bodyHeight,
            horizontal,
            vertical,
            ThresholdsPayload,
            output,
            verdict,
            issues);
    }

    public static void WriteOutputs(TileReport report, string sourcePng, string outputPng)
    {
        string? dir = Path.GetDirectoryName(outputPng);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(sourcePng, outputPng, true);

        var sidecar = new Dictionary<string, object?>
        {
            ["name"] = Path.GetFileNameWithoutExtension(outputPng),
            ["kind"] = report.Kind,
            ["width"] = report.Width,
            ["height"] = report.Height,
            ["horizontalSeamless"] = report.HorizontalSeam?.Pass == true,
        };
        if (report.Kind == "surface-cap")
        {
            sidecar["earthTopRow"] = report.EarthTopRow;
            sidecar["bodyHeight"] = report.BodyHeight;
        }
        if (report.Kind == "interior")
        {
            sidecar["verticalSeamless"] = report.VerticalSeam?.Pass == true;
        }

        string sidecarPath = Path.Combine(
            Path.GetDirectoryName(outputPng) ?? ".",
            Path.GetFileNameWithoutExtension(outputPng) + ".json");
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar, JsonOptions()) + Environment.NewLine);
    }

    public static string RenderJson(TileReport report) =>
        JsonSerializer.Serialize(report, JsonOptions());

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string SeamIssueMessage(string axis, TileSeamCheck seam) =>
        $"hard seam at the {axis} wrap: edge discontinuity {seam.EdgeMeanDiff} exceeds what the interior variation ({seam.InteriorMeanDiff}) allows (thresholds: floor {SeamAbsoluteFloor}, ratio limit {SeamRatioLimit})";

    private static TileSeamCheck CheckHorizontalSeam(Image<Rgba32> image, int yStart, int yEnd)
    {
        int w = image.Width;
        double edgeSum = 0;
        double interiorSum = 0;
        long interiorCount = 0;
        long rows = 0;

        for (int y = yStart; y <= yEnd; y++)
        {
            edgeSum += Math.Abs(Luma(image[0, y]) - Luma(image[w - 1, y]));
            rows++;
            for (int x = 1; x < w; x++)
            {
                interiorSum += Math.Abs(Luma(image[x, y]) - Luma(image[x - 1, y]));
                interiorCount++;
            }
        }

        return BuildSeamCheck(edgeSum / rows, interiorCount > 0 ? interiorSum / interiorCount : 0);
    }

    private static TileSeamCheck CheckVerticalSeam(Image<Rgba32> image)
    {
        int h = image.Height;
        double edgeSum = 0;
        double interiorSum = 0;
        long interiorCount = 0;
        long cols = 0;

        for (int x = 0; x < image.Width; x++)
        {
            edgeSum += Math.Abs(Luma(image[x, 0]) - Luma(image[x, h - 1]));
            cols++;
            for (int y = 1; y < h; y++)
            {
                interiorSum += Math.Abs(Luma(image[x, y]) - Luma(image[x, y - 1]));
                interiorCount++;
            }
        }

        return BuildSeamCheck(edgeSum / cols, interiorCount > 0 ? interiorSum / interiorCount : 0);
    }

    private static TileSeamCheck BuildSeamCheck(double edgeMean, double interiorMean)
    {
        edgeMean = Math.Round(edgeMean, 3);
        interiorMean = Math.Round(interiorMean, 3);
        if (interiorMean < 0.001)
        {
            return new TileSeamCheck(edgeMean <= SeamAbsoluteFloor, edgeMean, interiorMean, null);
        }
        double ratio = Math.Round(edgeMean / interiorMean, 3);
        bool pass = edgeMean <= Math.Max(SeamAbsoluteFloor, SeamRatioLimit * interiorMean);
        return new TileSeamCheck(pass, edgeMean, interiorMean, ratio);
    }

    private static double Luma(Rgba32 p) => 0.299 * p.R + 0.587 * p.G + 0.114 * p.B;

    private static bool[] FullyOpaqueRows(Image<Rgba32> image)
    {
        var rows = new bool[image.Height];
        for (int y = 0; y < image.Height; y++)
        {
            bool opaque = true;
            for (int x = 0; x < image.Width && opaque; x++)
            {
                opaque = image[x, y].A == 255;
            }
            rows[y] = opaque;
        }
        return rows;
    }

    private static int FirstTrue(bool[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i])
            {
                return i;
            }
        }
        return -1;
    }

    private static bool AllTrue(bool[] values) => Array.TrueForAll(values, v => v);

    private static bool HasTransparentPixelFrom(Image<Rgba32> image, int yStart)
    {
        for (int y = yStart; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image[x, y].A < 255)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static int FirstTransparentRowFrom(Image<Rgba32> image, int yStart)
    {
        for (int y = yStart; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image[x, y].A < 255)
                {
                    return y;
                }
            }
        }
        return -1;
    }

    private static int CountNonOpaque(Image<Rgba32> image)
    {
        int count = 0;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image[x, y].A < 255)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public static PngHeader ReadPngHeader(string input)
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
            throw new ArgumentException("only PNG images are supported (tile validation reads the alpha/color-type header)");
        }

        return new PngHeader(
            Be32(head, 16),
            Be32(head, 20),
            head[25],
            head[25] is 4 or 6);
    }

    private static int Be32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
