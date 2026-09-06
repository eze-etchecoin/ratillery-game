using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor.Tests;

public sealed class OpacityTests : IClassFixture<TileFixtures>
{
    private readonly TileFixtures _fx;

    public OpacityTests(TileFixtures fx) => _fx = fx;

    [Theory]
    [InlineData(TileKind.Rock)]
    [InlineData(TileKind.Interior)]
    public void OpaqueRgb_NoAlphaChannel_IsAccepted(TileKind kind)
    {
        string path = _fx.SaveRgbGray("tile-rgb.png", 32, 32, 120);
        TileReport report = _fx.Validate(path, kind);
        Assert.Equal("ok", report.Verdict);
        Assert.False(report.HasAlpha);
        Assert.Empty(report.Issues);
    }

    [Theory]
    [InlineData(TileKind.Rock)]
    [InlineData(TileKind.Interior)]
    public void FullyOpaqueRgba_IsAccepted(TileKind kind)
    {
        string path = _fx.SaveRgba("tile-rgba.png", 32, 32, (x, y) => TileFixtures.SeededNoise(x, y));
        TileReport report = _fx.Validate(path, kind);
        Assert.Equal("ok", report.Verdict);
        Assert.True(report.HasAlpha);
        Assert.Empty(report.Issues);
    }

    [Theory]
    [InlineData(TileKind.Rock)]
    [InlineData(TileKind.Interior)]
    public void SinglePixelBelowAlpha255_FailsWithIssue(TileKind kind)
    {
        string path = _fx.SaveRgba("tile-alpha.png", 32, 32, (x, y) =>
            x == 5 && y == 7 ? new Rgba32(10, 20, 30, 254) : TileFixtures.SeededNoise(x, y));
        TileReport report = _fx.Validate(path, kind);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("not fully opaque"));
    }
}
