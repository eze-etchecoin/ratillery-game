using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor.Tests;

public sealed class SeamTests : IClassFixture<TileFixtures>
{
    private readonly TileFixtures _fx;

    public SeamTests(TileFixtures fx) => _fx = fx;

    private static Rgba32 CapPixel(int x, int y, int width, int headroom)
    {
        if (y < headroom)
        {
            return new Rgba32(0, 0, 0, 0);
        }
        return TileFixtures.Periodic(x, 1, width);
    }

    [Fact]
    public void SurfaceCap_SeamlessBody_PassesHorizontalSeam()
    {
        string path = _fx.SaveRgba("cap-seamless.png", 64, 32, (x, y) => CapPixel(x, y, 64, 8));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
    }

    [Fact]
    public void SurfaceCap_HardStepInBody_FailsHorizontalSeam()
    {
        string path = _fx.SaveRgba("cap-step.png", 64, 32, (x, y) =>
            y < 8 ? new Rgba32(0, 0, 0, 0) : TileFixtures.HardStepHorizontal(x, 64));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.False(report.HorizontalSeam!.Pass);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("left/right"));
    }

    [Fact]
    public void Rock_SeamlessNoise_PassesHorizontalSeam()
    {
        string path = _fx.SaveRgba("rock-ok.png", 64, 64, (x, y) => TileFixtures.SeededNoise(x, y));
        TileReport report = _fx.Validate(path, TileKind.Rock);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
        Assert.Null(report.VerticalSeam);
    }

    [Fact]
    public void Rock_HardStep_FailsHorizontalSeam()
    {
        string path = _fx.SaveRgba("rock-step.png", 64, 64, (x, y) => TileFixtures.HardStepHorizontal(x, 64));
        TileReport report = _fx.Validate(path, TileKind.Rock);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.False(report.HorizontalSeam!.Pass);
    }

    [Fact]
    public void Interior_UniformSolidTile_IsTriviallySeamlessOnBothAxes()
    {
        string path = _fx.SaveRgba("interior-solid.png", 32, 32, (_, _) => TileFixtures.Gray(90));
        TileReport report = _fx.Validate(path, TileKind.Interior);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
        Assert.True(report.VerticalSeam!.Pass);
    }

    [Fact]
    public void Interior_SeamlessNoise_PassesBothAxes()
    {
        string path = _fx.SaveRgba("interior-ok.png", 64, 64, (x, y) => TileFixtures.SeededNoise(x, y));
        TileReport report = _fx.Validate(path, TileKind.Interior);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
        Assert.True(report.VerticalSeam!.Pass);
    }

    [Fact]
    public void Interior_HardHorizontalStep_FailsHorizontalSeamOnly()
    {
        string path = _fx.SaveRgba("interior-hstep.png", 64, 64, (x, y) => TileFixtures.HardStepHorizontal(x, 64));
        TileReport report = _fx.Validate(path, TileKind.Interior);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.False(report.HorizontalSeam!.Pass);
        Assert.True(report.VerticalSeam!.Pass);
    }

    [Fact]
    public void Interior_HardVerticalStep_FailsVerticalSeamOnly()
    {
        string path = _fx.SaveRgba("interior-vstep.png", 64, 64, (x, y) =>
        {
            byte v = TileFixtures.Periodic(x, 1, 64).R;
            return TileFixtures.Gray(y < 32 ? v : (byte)(255 - v));
        });
        TileReport report = _fx.Validate(path, TileKind.Interior);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
        Assert.False(report.VerticalSeam!.Pass);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("top/bottom"));
    }

    [Fact]
    public void SurfaceCap_NonSeamlessHeadroom_IsExemptFromBodySeamCheck()
    {
        string path = _fx.SaveRgba("cap-tuft-edge.png", 64, 32, (x, y) =>
        {
            if (y == 0)
            {
                return x < 32 ? TileFixtures.Gray(0) : new Rgba32(255, 255, 255, 254);
            }
            if (y == 1)
            {
                return new Rgba32(0, 0, 0, 0);
            }
            return TileFixtures.Periodic(x, 1, 64);
        });
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HorizontalSeam!.Pass);
        Assert.Equal(2, report.EarthTopRow);
        Assert.Equal(30, report.BodyHeight);
    }
}
