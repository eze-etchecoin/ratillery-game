namespace Ratillery.Core.Terrain;

/// <summary>
/// Data-driven description of the playfield terrain (R-4). The profile
/// parameters drive the deterministic surface generation (R-1): the same
/// configuration always produces the exact same terrain (AC-4). The depth-band
/// thicknesses only describe the layered fill appearance (R-2 Option A) and
/// never affect solidity: the mask is solid from the surface to the bottom of
/// the playfield in every column.
/// </summary>
public sealed class TerrainConfig
{
    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public int Seed { get; set; } = 20260906;

    /// <summary>
    /// Staging surface height as a fraction of <see cref="Height"/>. The
    /// surface at the center column (where the rat spawns, R-5) is anchored to
    /// this line so the composition matches the RAT-001 staging (~78%).
    /// </summary>
    public float BaseSurfaceFraction { get; set; } = 0.78f;

    // --- Surface profile (deterministic procedural generation) ---

    /// <summary>Shortest horizontal extent of a hill-side transition, in pixels.</summary>
    public int MinSlopePixels { get; set; } = 80;

    /// <summary>Longest horizontal extent of a hill-side transition, in pixels.</summary>
    public int MaxSlopePixels { get; set; } = 200;

    /// <summary>Maximum vertical change per hill-side transition, in pixels.</summary>
    public int MaxSlopeStepPixels { get; set; } = 36;

    /// <summary>
    /// How strongly each hill-side transition pulls the surface back toward the
    /// staging line (0 = free walk, 1 = immediate return).
    /// </summary>
    public float SlopeReversion { get; set; } = 0.45f;

    /// <summary>Probability that a generated segment is a flat span.</summary>
    public float FlatSpanChance { get; set; } = 0.4f;

    /// <summary>Shortest flat span, in pixels.</summary>
    public int MinFlatSpanPixels { get; set; } = 40;

    /// <summary>Longest flat span, in pixels.</summary>
    public int MaxFlatSpanPixels { get; set; } = 150;

    /// <summary>Hard cap on how far the surface may wander from the staging line, in pixels.</summary>
    public int MaxDeviationPixels { get; set; } = 90;

    // --- Depth bands (fill appearance only) ---

    /// <summary>Vertical thickness of the surface grass/earth band below the local surface, in pixels.</summary>
    public int SurfaceBandPixels { get; set; } = 24;

    /// <summary>Vertical thickness of the rock band below the surface band, in pixels.</summary>
    public int RockBandPixels { get; set; } = 64;
}
