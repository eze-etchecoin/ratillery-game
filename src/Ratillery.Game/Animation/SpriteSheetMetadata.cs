using System.Text.Json;

namespace Ratillery.Game.Animation;

/// <summary>
/// Describes a processed horizontal sprite sheet, as written by the asset
/// pipeline into the JSON sidecar next to each sheet (ADR-002). All animation
/// parameters are driven from this data, never from hardcoded values.
/// </summary>
public sealed class SpriteSheetMetadata
{
    public string Name { get; set; } = string.Empty;

    public int FrameCount { get; set; }

    public int FrameWidth { get; set; }

    public int FrameHeight { get; set; }

    public int Fps { get; set; }

    public bool Loop { get; set; } = true;

    public SpriteSheetPivot Pivot { get; set; } = new();

    public static SpriteSheetMetadata Load(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var metadata = JsonSerializer.Deserialize<SpriteSheetMetadata>(json, options)
            ?? throw new InvalidDataException($"Sprite metadata '{jsonPath}' is empty.");

        if (metadata.FrameCount <= 0 || metadata.FrameWidth <= 0 || metadata.FrameHeight <= 0 || metadata.Fps <= 0)
            throw new InvalidDataException($"Sprite metadata '{jsonPath}' has invalid geometry or fps values.");

        return metadata;
    }
}

public sealed class SpriteSheetPivot
{
    public float X { get; set; }

    public float Y { get; set; }
}
