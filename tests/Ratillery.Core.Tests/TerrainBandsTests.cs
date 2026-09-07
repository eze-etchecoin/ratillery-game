using Ratillery.Core.Terrain;

namespace Ratillery.Core.Tests;

/// <summary>
/// TOOL-002 AC-9: band-row mapping and interior world-anchored phase are
/// MonoGame-free Core logic, unit-tested without a graphics window. Band
/// thicknesses are data-driven (cap body 21, rock full height 64 — DEC-007).
/// </summary>
public class TerrainBandsTests
{
    [Fact]
    public void Compute_TypicalSurface_BandsStackBelowSurface_InteriorToBottom()
    {
        var bands = TerrainBands.Compute(562, surfaceBandPixels: 21, rockBandPixels: 64, bottom: 720);

        Assert.Equal(562, bands.CapTop);
        Assert.Equal(583, bands.RockTop);
        Assert.Equal(647, bands.InteriorTop);
        Assert.Equal(720, bands.Bottom);
    }

    [Fact]
    public void Compute_SurfaceDeep_WholeColumnBelowPlayfieldBottom_InteriorIsEmpty()
    {
        // H(x) + cap alone reaches the bottom: rock and interior collapse.
        var bands = TerrainBands.Compute(700, surfaceBandPixels: 21, rockBandPixels: 64, bottom: 720);

        Assert.Equal(700, bands.CapTop);
        Assert.Equal(720, bands.RockTop);
        Assert.Equal(720, bands.InteriorTop);
        Assert.Equal(720, bands.Bottom);
    }

    [Theory]
    [InlineData(635)] // H + cap + rock == bottom: interior starts exactly at bottom (empty)
    [InlineData(634)] // H + cap + rock == bottom - 1: one interior row remains
    public void Compute_RockReachesPlayfieldBottom_InteriorEmptyOrOneRow(int surface)
    {
        var bands = TerrainBands.Compute(surface, surfaceBandPixels: 21, rockBandPixels: 64, bottom: 720);

        Assert.True(bands.InteriorTop >= 719, $"interior should start at/near the bottom, got {bands.InteriorTop}");
        Assert.True(bands.InteriorTop <= 720);
        Assert.Equal(720, bands.Bottom);
    }

    [Fact]
    public void Compute_RockPartiallyClipped_InteriorClampsToBottom()
    {
        // H + cap + rock overshoots the bottom: rock is partial, interior empty.
        var bands = TerrainBands.Compute(650, surfaceBandPixels: 21, rockBandPixels: 64, bottom: 720);

        Assert.Equal(650, bands.CapTop);
        Assert.Equal(671, bands.RockTop);
        Assert.Equal(720, bands.InteriorTop);
        Assert.Equal(671, bands.RockTop); // rock visible rows = 671..720
    }

    [Fact]
    public void Compute_NeverReportsBandsBelowPlayfieldBottom()
    {
        for (var surface = 0; surface <= 720; surface++)
        {
            var bands = TerrainBands.Compute(surface, surfaceBandPixels: 21, rockBandPixels: 64, bottom: 720);

            Assert.InRange(bands.CapTop, 0, 720);
            Assert.InRange(bands.RockTop, 0, 720);
            Assert.InRange(bands.InteriorTop, 0, 720);
            Assert.True(bands.CapTop <= bands.RockTop && bands.RockTop <= bands.InteriorTop);
        }
    }

    [Theory]
    [InlineData(0, 64, 0)]
    [InlineData(63, 64, 63)]
    [InlineData(64, 64, 0)]
    [InlineData(65, 64, 1)]
    [InlineData(127, 64, 63)]
    [InlineData(719, 64, 15)]
    public void InteriorSourceRow_TilesVertically_WithTexturePeriod(int worldRow, int interiorHeight, int expected)
    {
        Assert.Equal(expected, TerrainBands.InteriorSourceRow(worldRow, interiorHeight));
    }

    [Fact]
    public void InteriorSourceRow_IsWorldAnchoredAcrossColumns()
    {
        // Different surface heights put each column's interior top at a
        // different world row, yet a fixed world row below them all samples the
        // same interior source row (phase is a function of world Y alone, AC-4).
        const int bottom = 720;
        var lowColumn = TerrainBands.Compute(550, surfaceBandPixels: 21, rockBandPixels: 64, bottom); // interiorTop 655
        var highColumn = TerrainBands.Compute(520, surfaceBandPixels: 21, rockBandPixels: 64, bottom); // interiorTop 625
        const int worldRow = 700;

        Assert.NotEqual(lowColumn.InteriorTop, highColumn.InteriorTop);
        Assert.Equal(
            TerrainBands.InteriorSourceRow(worldRow, 64),
            TerrainBands.InteriorSourceRow(worldRow, 64));
    }
}
