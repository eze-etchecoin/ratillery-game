namespace Ratillery.Core.Terrain;

/// <summary>
/// Static collision mask of the playfield terrain: the authoritative
/// solidity representation future slices will query and carve. Coordinates
/// follow the Core convention (Y grows downward, one cell per world unit).
/// For every column the solid cells form a single run from the surface row to
/// the bottom of the playfield; positions outside the playfield are empty.
/// The mask is immutable once constructed (AC-4).
/// </summary>
public sealed class TerrainMask
{
    private readonly int[] _surfaceRow;

    public int Width { get; }

    public int Height { get; }

    public TerrainMask(TerrainConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Validate(config);

        Width = config.Width;
        Height = config.Height;
        _surfaceRow = GenerateSurface(config);
    }

    /// <summary>
    /// Whether the cell at (column, row) is solid terrain. Any position
    /// outside the playfield is empty; the query never throws (AC-5).
    /// </summary>
    public bool IsSolid(int column, int row)
    {
        if (column < 0 || column >= Width)
            return false;

        var surface = _surfaceRow[column];
        return row >= surface && row < Height;
    }

    /// <summary>
    /// Terrain surface height at a column: the top edge (Y) of the topmost
    /// solid cell in that column. A position at that Y is solid; one step
    /// above is empty (AC-5). Columns outside the playfield clamp to the edge.
    /// </summary>
    public int SurfaceHeight(int column)
    {
        var clamped = Math.Clamp(column, 0, Width - 1);
        return _surfaceRow[clamped];
    }

    private static void Validate(TerrainConfig config)
    {
        if (config.Width < 1)
            throw new ArgumentOutOfRangeException(nameof(config.Width), "Width must be at least 1.");
        if (config.Height < 1)
            throw new ArgumentOutOfRangeException(nameof(config.Height), "Height must be at least 1.");
        if (config.BaseSurfaceFraction <= 0f || config.BaseSurfaceFraction >= 1f)
            throw new ArgumentOutOfRangeException(nameof(config.BaseSurfaceFraction), "BaseSurfaceFraction must be within (0, 1).");

        var baseRow = (int)MathF.Round(config.Height * config.BaseSurfaceFraction);
        if (baseRow < 0 || baseRow >= config.Height)
            throw new ArgumentOutOfRangeException(nameof(config.BaseSurfaceFraction), "BaseSurfaceFraction must place the staging row inside the playfield.");

        if (config.MinSlopePixels < 2 || config.MinSlopePixels > config.MaxSlopePixels)
            throw new ArgumentOutOfRangeException(nameof(config.MinSlopePixels), "Slope range must satisfy 2 <= MinSlopePixels <= MaxSlopePixels.");
        if (config.MinFlatSpanPixels < 1 || config.MinFlatSpanPixels > config.MaxFlatSpanPixels)
            throw new ArgumentOutOfRangeException(nameof(config.MinFlatSpanPixels), "Flat-span range must satisfy 1 <= MinFlatSpanPixels <= MaxFlatSpanPixels.");
        if (config.MaxSlopeStepPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxSlopeStepPixels), "MaxSlopeStepPixels cannot be negative.");
        if (config.SlopeReversion is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(config.SlopeReversion), "SlopeReversion must be within [0, 1].");
        if (config.MaxDeviationPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxDeviationPixels), "MaxDeviationPixels cannot be negative.");
        if (config.SurfaceBandPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(config.SurfaceBandPixels), "SurfaceBandPixels cannot be negative.");
        if (config.RockBandPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(config.RockBandPixels), "RockBandPixels cannot be negative.");
    }

    /// <summary>
    /// Deterministic profile generation (R-1): moderate hill-side transitions
    /// connecting occasional flat spans. Segment lengths, flat placement and
    /// slope steps all come from a single seeded sequence.
    /// </summary>
    private int[] GenerateSurface(TerrainConfig config)
    {
        var random = new Random(config.Seed);
        var rows = new int[config.Width];
        var baseRow = (int)MathF.Round(config.Height * config.BaseSurfaceFraction);
        var minRow = Math.Max(0, baseRow - config.MaxDeviationPixels);
        var maxRow = Math.Min(config.Height - 1, baseRow + config.MaxDeviationPixels);

        var current = (float)baseRow;
        var column = 0;
        while (column < config.Width)
        {
            var remaining = config.Width - column;
            if (random.NextDouble() < config.FlatSpanChance)
            {
                var length = Math.Min(random.Next(config.MinFlatSpanPixels, config.MaxFlatSpanPixels + 1), remaining);
                for (var i = 0; i < length; i++)
                    rows[column++] = RoundToRow(current, minRow, maxRow);
            }
            else
            {
                var length = Math.Min(random.Next(config.MinSlopePixels, config.MaxSlopePixels + 1), remaining);
                var target = baseRow + (current - baseRow) * config.SlopeReversion
                    + (float)((random.NextDouble() * 2.0 - 1.0) * config.MaxSlopeStepPixels);
                target = Math.Clamp(target, minRow, maxRow);

                var last = length - 1;
                for (var i = 0; i < length; i++)
                {
                    var t = last == 0 ? 0f : i / (float)last;
                    var eased = 0.5f - 0.5f * MathF.Cos(MathF.PI * t);
                    rows[column++] = RoundToRow(current + (target - current) * eased, minRow, maxRow);
                }

                current = target;
            }
        }

        AnchorCenterToBaseRow(rows, baseRow);
        return rows;
    }

    /// <summary>
    /// Shifts the whole profile vertically so the rat's column sits exactly on
    /// the staging line, guaranteeing the rat always has solid terrain under
    /// its feet and stays fully on screen (R-5, degenerate-geometry guard).
    /// </summary>
    private void AnchorCenterToBaseRow(int[] rows, int baseRow)
    {
        var delta = baseRow - rows[Width / 2];
        for (var column = 0; column < rows.Length; column++)
            rows[column] = Math.Clamp(rows[column] + delta, 0, Height - 1);
    }

    private static int RoundToRow(float value, int minRow, int maxRow)
    {
        return Math.Clamp((int)MathF.Round(value), minRow, maxRow);
    }
}
