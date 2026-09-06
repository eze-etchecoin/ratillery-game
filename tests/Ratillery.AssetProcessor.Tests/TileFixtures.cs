using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor.Tests;

public sealed class TileFixtures : IDisposable
{
    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "ratillery-tile-tests", Guid.NewGuid().ToString("N"));

    public TileFixtures()
    {
        Directory.CreateDirectory(Dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(Dir))
        {
            Directory.Delete(Dir, recursive: true);
        }
    }

    public string SaveRgba(string name, int width, int height, Func<int, int, Rgba32> pixel)
    {
        using Image<Rgba32> image = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = pixel(x, y);
            }
        }
        image.Metadata.GetPngMetadata().ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha;
        string path = Path.Combine(Dir, name);
        image.SaveAsPng(path);
        return path;
    }

    public string SaveRgbGray(string name, int width, int height, byte luma)
    {
        using Image<L8> image = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new L8(luma);
            }
        }
        string path = Path.Combine(Dir, name);
        image.SaveAsPng(path);
        return path;
    }

    public static Rgba32 Gray(byte v) => new(v, v, v, 255);

    public static Rgba32 Periodic(int x, int cycles, int width) =>
        Gray((byte)(128 + 120 * Math.Sin(2 * Math.PI * cycles * x / width)));

    public static Rgba32 HardStepHorizontal(int x, int width) =>
        Gray(x < width / 2 ? (byte)0 : (byte)255);

    public static Rgba32 HardStepVertical(int y, int height) =>
        Gray(y < height / 2 ? (byte)0 : (byte)255);

    public static Rgba32 SeededNoise(int x, int y) => Gray((byte)((x * 73 + y * 131 + 41) % 256));

    public TileReport Validate(string path, TileKind kind, string? target = "out/unused.png") =>
        TileProcessing.Validate(path, kind, target);
}
