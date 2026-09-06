using Ratillery.Core.Terrain;
using Xunit;

namespace Ratillery.Core.Tests;

public class TerrainMaskTests
{
    /// <summary>Representative fixed configuration = the shipped playfield definition (AC-10).</summary>
    private static TerrainConfig DefaultConfig() => new();

    [Fact]
    public void SurfaceHeight_CenterColumn_MatchesStagingFraction()
    {
        var mask = new TerrainMask(DefaultConfig());
        var expected = (int)MathF.Round(DefaultConfig().Height * DefaultConfig().BaseSurfaceFraction);

        Assert.Equal(expected, mask.SurfaceHeight(mask.Width / 2));
    }

    [Fact]
    public void IsSolid_AtExactSurfaceBoundary_IsSolidAtSurfaceAndEmptyOneStepAbove()
    {
        var mask = new TerrainMask(DefaultConfig());
        var centerColumn = mask.Width / 2;
        var surface = mask.SurfaceHeight(centerColumn);

        Assert.True(mask.IsSolid(centerColumn, surface));
        Assert.False(mask.IsSolid(centerColumn, surface - 1));
        Assert.True(mask.IsSolid(centerColumn, mask.Height - 1));
        Assert.False(mask.IsSolid(centerColumn, mask.Height));
        Assert.False(mask.IsSolid(centerColumn, 0));
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(1280, 100)]
    [InlineData(-5000, -5000)]
    [InlineData(5000, 5000)]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void IsSolid_OutOfPlayfield_IsEmptyAndDoesNotThrow(int column, int row)
    {
        var mask = new TerrainMask(DefaultConfig());

        Assert.False(mask.IsSolid(column, row));
    }

    [Fact]
    public void SurfaceHeight_OutOfPlayfieldColumns_ClampsToNearestEdgeColumn()
    {
        var mask = new TerrainMask(DefaultConfig());

        Assert.Equal(mask.SurfaceHeight(0), mask.SurfaceHeight(-10));
        Assert.Equal(mask.SurfaceHeight(mask.Width - 1), mask.SurfaceHeight(mask.Width + 10));
    }

    [Fact]
    public void EveryColumn_HasSurfaceInsidePlayfield_SolidDownToBottom_EmptyAbove()
    {
        var mask = new TerrainMask(DefaultConfig());
        var violations = 0;

        for (var column = 0; column < mask.Width; column++)
        {
            var surface = mask.SurfaceHeight(column);
            if (surface < 0 || surface >= mask.Height)
                violations++;
            if (!mask.IsSolid(column, surface))
                violations++;
            if (!mask.IsSolid(column, mask.Height - 1))
                violations++;
            if (surface > 0 && mask.IsSolid(column, surface - 1))
                violations++;
        }

        Assert.Equal(0, violations);
    }

    [Fact]
    public void SolidCells_FormExactlyOneContiguousRunPerColumn_NoHolesOrFloatingCells()
    {
        var mask = new TerrainMask(DefaultConfig());
        var mismatches = 0;

        for (var column = 0; column < mask.Width; column++)
        {
            var surface = mask.SurfaceHeight(column);
            for (var row = 0; row < mask.Height; row++)
            {
                if (mask.IsSolid(column, row) != (row >= surface))
                    mismatches++;
            }
        }

        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void RepeatedConstruction_SameConfig_ProducesIdenticalTerrain()
    {
        var first = new TerrainMask(DefaultConfig());
        var second = new TerrainMask(DefaultConfig());
        var mismatches = 0;

        for (var column = 0; column < first.Width; column++)
        {
            if (first.SurfaceHeight(column) != second.SurfaceHeight(column))
                mismatches++;
        }

        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void DefaultConfig_ProfileHasHeightVariation_NotAStraightLine()
    {
        var mask = new TerrainMask(DefaultConfig());
        var distinctHeights = new HashSet<int>();
        for (var column = 0; column < mask.Width; column++)
            distinctHeights.Add(mask.SurfaceHeight(column));

        Assert.True(distinctHeights.Count > 1, "The shipped profile should show height variation along the playfield (AC-3).");
    }

    [Theory]
    [InlineData(0, 720, 0.78f)]
    [InlineData(1280, 0, 0.78f)]
    [InlineData(1280, 720, 0f)]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 0.78f, 0, 100)]
    public void Constructor_InvalidConfiguration_Throws(int width, int height, float fraction, int minSlope = 80, int maxSlope = 200)
    {
        var config = new TerrainConfig
        {
            Width = width,
            Height = height,
            BaseSurfaceFraction = fraction,
            MinSlopePixels = minSlope,
            MaxSlopePixels = maxSlope,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainMask(config));
    }

    [Fact]
    public void Constructor_VeryNarrowPlayfield_StillProducesSolidColumns()
    {
        var config = new TerrainConfig { Width = 5, Height = 100, BaseSurfaceFraction = 0.5f };
        var mask = new TerrainMask(config);

        for (var column = 0; column < mask.Width; column++)
            Assert.True(mask.IsSolid(column, mask.Height - 1));
    }
}
