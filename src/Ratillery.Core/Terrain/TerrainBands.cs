namespace Ratillery.Core.Terrain;

/// <summary>
/// Pure, MonoGame-free mapping of a column's mask surface height and the
/// data-driven visual band thicknesses into the vertical row spans each depth
/// band occupies below the surface. It describes the layered fill appearance
/// only (R-2 Option A / DEC-007) and never defines solidity: the mask is solid
/// from the surface to the playfield bottom in every column regardless of where
/// the bands fall. Boundaries are clamped so a low-lying column never runs a
/// band past the playfield bottom (TOOL-002 AC-3/AC-4).
/// </summary>
public static class TerrainBands
{
    /// <summary>
    /// Computes the row boundaries for a column whose mask surface sits at
    /// <paramref name="surfaceHeight"/>: the cap body occupies
    /// [CapTop, RockTop), rock [RockTop, InteriorTop), interior
    /// [InteriorTop, Bottom). CapTop equals the surface height; rock begins
    /// right after the cap band; interior runs to the playfield bottom.
    /// </summary>
    public static BandBoundaries Compute(int surfaceHeight, int surfaceBandPixels, int rockBandPixels, int bottom)
    {
        var capTop = Math.Clamp(surfaceHeight, 0, bottom);
        var rockTop = Math.Min(bottom, capTop + Math.Max(0, surfaceBandPixels));
        var interiorTop = Math.Min(bottom, rockTop + Math.Max(0, rockBandPixels));
        return new BandBoundaries(capTop, rockTop, interiorTop, bottom);
    }

    /// <summary>
    /// The interior source row sampled at a given world row, so a fixed world
    /// Y maps to the same interior source row in every column (world-anchored
    /// vertical phase, TOOL-002 AC-4).
    /// </summary>
    public static int InteriorSourceRow(int worldRow, int interiorHeight)
        => worldRow % interiorHeight;
}

/// <summary>Row boundaries of the three depth bands for one column (see <see cref="TerrainBands.Compute"/>).</summary>
public readonly record struct BandBoundaries(int CapTop, int RockTop, int InteriorTop, int Bottom);
