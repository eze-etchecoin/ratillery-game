using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor.Tests;

public sealed class EarthTopTests : IClassFixture<TileFixtures>
{
    private const int Width = 64;
    private const int Height = 16;
    private const int Headroom = 3;

    private readonly TileFixtures _fx;

    public EarthTopTests(TileFixtures fx) => _fx = fx;

    private static Rgba32 CapWithHoles(int x, int y, int? holeX = null, int? holeY = null, int? opaqueRow = null)
    {
        if (opaqueRow == y)
        {
            return TileFixtures.HardStepHorizontal(x, Width);
        }
        if (y < Headroom)
        {
            return x == 0 ? new Rgba32(0, 200, 0, 128) : new Rgba32(0, 0, 0, 0);
        }
        if (holeX == x && holeY == y)
        {
            return new Rgba32(120, 80, 40, 128);
        }
        return TileFixtures.Periodic(x, 1, Width);
    }

    [Fact]
    public void DetectsEarthTopRow_AtExactBoundary()
    {
        string path = _fx.SaveRgba("cap-t.png", Width, Height, (x, y) => CapWithHoles(x, y));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("ok", report.Verdict);
        Assert.Equal(Headroom, report.EarthTopRow);
        Assert.Equal(Height - Headroom, report.BodyHeight);
    }

    [Fact]
    public void EarthTopRowAtBottomRow_IsStructurallyAllowed()
    {
        string path = _fx.SaveRgba("cap-bottom.png", Width, Height, (x, y) =>
            y < Height - 1
                ? (x == 1 ? new Rgba32(0, 0, 0, 128) : new Rgba32(0, 0, 0, 0))
                : TileFixtures.Gray(150));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("ok", report.Verdict);
        Assert.Equal(Height - 1, report.EarthTopRow);
        Assert.Equal(1, report.BodyHeight);
    }

    [Fact]
    public void FullyOpaqueTuftRowAboveBody_MovesEarthTopUp_AndHoleBelowFails()
    {
        string path = _fx.SaveRgba("cap-tuft-row.png", Width, Height, (x, y) =>
            CapWithHoles(x, y, holeX: 10, holeY: 1, opaqueRow: 0));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("transparent hole below the earth-top row"));
    }

    [Fact]
    public void TransparentPixelBelowEarthTopRow_Fails()
    {
        string path = _fx.SaveRgba("cap-hole.png", Width, Height, (x, y) =>
            CapWithHoles(x, y, holeX: 40, holeY: Height - 1));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("transparent hole below the earth-top row"));
    }

    [Fact]
    public void NoFullyOpaqueFullWidthRow_Fails()
    {
        string path = _fx.SaveRgba("cap-no-t.png", Width, Height, (x, y) =>
            x == y % Width ? new Rgba32(0, 0, 0, 128) : TileFixtures.Periodic(x, 1, Width));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("no earth-top row found"));
    }

    [Fact]
    public void FullyOpaqueCap_Fails()
    {
        string path = _fx.SaveRgba("cap-opaque.png", Width, Height, (x, y) => TileFixtures.SeededNoise(x, y));
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("fully opaque surface-cap"));
    }

    [Fact]
    public void RgbCap_NoAlphaChannel_Fails()
    {
        string path = _fx.SaveRgbGray("cap-rgb.png", Width, Height, 100);
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Contains(report.Issues, i => i.Severity == "error" && i.Message.Contains("no alpha channel"));
    }

    [Fact]
    public void FreeFormHeadroomAboveEarthTop_Passes()
    {
        string path = _fx.SaveRgba("cap-freeform.png", Width, Height, (x, y) =>
        {
            if (y == 0)
            {
                return x is < 10 and > 3 ? TileFixtures.Gray(30) : new Rgba32(0, 200, 0, 200);
            }
            if (y == 1)
            {
                return new Rgba32(0, 0, 0, 0);
            }
            if (y == 2)
            {
                return x % 3 == 0 ? new Rgba32(0, 255, 0, 255) : new Rgba32(0, 0, 0, 0);
            }
            return TileFixtures.Periodic(x, 1, Width);
        });
        TileReport report = _fx.Validate(path, TileKind.SurfaceCap);
        Assert.Equal("ok", report.Verdict);
        Assert.Equal(3, report.EarthTopRow);
    }
}
