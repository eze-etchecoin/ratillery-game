using System.Text.Json;

namespace Ratillery.Game.Rendering;

/// <summary>
/// Metadata sidecar written by the asset pipeline next to each processed
/// terrain tile (ADR-002 `tile`, TOOL-001). It drives how each layer is sampled
/// at runtime (earth-top alignment, band thickness, tiling axes) — no hardcoded
/// values. Parsing feeds rendering, so it lives in the Game layer and is not
/// unit-tested (project rule).
/// </summary>
public sealed class TerrainTileSidecar
{
    public string Kind { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public bool HorizontalSeamless { get; set; }

    public bool VerticalSeamless { get; set; }

    /// <summary>surface-cap only: the row aligned to the geometric surface H(x).</summary>
    public int? EarthTopRow { get; set; }

    /// <summary>surface-cap only: opaque body thickness below the earth-top row.</summary>
    public int? BodyHeight { get; set; }

    public static TerrainTileSidecar Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var sidecar = JsonSerializer.Deserialize<TerrainTileSidecar>(json, options)
            ?? throw new InvalidDataException($"Terrain tile sidecar '{jsonPath}' is empty.");

        if (string.IsNullOrWhiteSpace(sidecar.Kind) || sidecar.Width <= 0 || sidecar.Height <= 0)
            throw new InvalidDataException($"Terrain tile sidecar '{jsonPath}' has invalid kind or dimensions.");

        return sidecar;
    }
}
