using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ratillery.Core.Terrain;

namespace Ratillery.Game.Rendering;

/// <summary>
/// Real DEC-007 terrain fill: samples the three delivered, signed-off layer
/// tiles (<c>assets/terrain/layers/</c>) over exactly the mask-solid cells,
/// replacing the DEC-002 flat placeholder presentation (TOOL-002 AC-1). The cap
/// is contour-hugging — its earth-top row aligns to the mask surface H(x) with
/// transparent tuft overhang above (AC-2); rock maps its full texture 1:1 right
/// below the cap (AC-3); interior is world-anchored vertical tiling to the
/// playfield bottom (AC-4). Geometry/solidity are unchanged: only the fill
/// appearance differs (AC-5/AC-6). Created only when all three tiles and their
/// sidecars load (AC-8); otherwise the placeholder presentation is used.
/// </summary>
public sealed class RealTerrain
{
    private readonly Texture2D _cap;
    private readonly Texture2D _rock;
    private readonly Texture2D _interior;

    /// <summary>surface-cap sidecar: the texture row aligned to the mask surface H(x).</summary>
    private readonly int _capEarthTopRow;

    /// <summary>surface-cap sidecar: opaque cap body thickness below earth-top (fills [H(x), H(x)+body)).</summary>
    private readonly int _capBodyPixels;

    private RealTerrain(Texture2D cap, Texture2D rock, Texture2D interior, int capEarthTopRow, int capBodyPixels)
    {
        _cap = cap;
        _rock = rock;
        _interior = interior;
        _capEarthTopRow = capEarthTopRow;
        _capBodyPixels = capBodyPixels;
    }

    /// <summary>
    /// Loads the three terrain layer tiles plus their sidecars from
    /// <paramref name="layersDir"/>. Returns null (never throws) when any tile
    /// or sidecar is missing, unreadable, or lacks the field its role needs, so
    /// the caller falls back to the placeholder presentation (AC-8).
    /// </summary>
    public static RealTerrain? TryLoad(GraphicsDevice graphicsDevice, string layersDir)
    {
        var cap = default(Texture2D);
        var rock = default(Texture2D);
        var interior = default(Texture2D);
        try
        {
            cap = Texture2D.FromFile(graphicsDevice, Path.Combine(layersDir, "surface-cap.png"));
            rock = Texture2D.FromFile(graphicsDevice, Path.Combine(layersDir, "rock.png"));
            interior = Texture2D.FromFile(graphicsDevice, Path.Combine(layersDir, "interior.png"));

            var capSidecar = TerrainTileSidecar.Load(Path.Combine(layersDir, "surface-cap.json"));
            TerrainTileSidecar.Load(Path.Combine(layersDir, "rock.json"));
            TerrainTileSidecar.Load(Path.Combine(layersDir, "interior.json"));

            if (capSidecar.Kind != "surface-cap"
                || capSidecar.EarthTopRow is not { } earthTopRow || earthTopRow < 0 || earthTopRow >= cap.Height
                || capSidecar.BodyHeight is not { } bodyHeight || bodyHeight <= 0
                || bodyHeight != cap.Height - earthTopRow)
                return null;

            return new RealTerrain(cap, rock, interior, earthTopRow, bodyHeight);
        }
        catch (Exception)
        {
            cap?.Dispose();
            rock?.Dispose();
            interior?.Dispose();
            return null;
        }
    }

    public void Draw(SpriteBatch spriteBatch, TerrainMask mask)
    {
        // Rock band thickness is the rock texture's full height (data-driven).
        var rockBandPixels = _rock.Height;

        for (var column = 0; column < mask.Width; column++)
        {
            var surface = mask.SurfaceHeight(column);
            var bands = TerrainBands.Compute(surface, _capBodyPixels, rockBandPixels, mask.Height);

            // Cap: align earth-top row to H(x); transparent headroom/tuft rows
            // above it hang over the surface into the air (AC-2, R-2(c)).
            var capTop = surface - _capEarthTopRow;
            spriteBatch.Draw(
                _cap,
                new Rectangle(column, capTop, 1, _cap.Height),
                new Rectangle(column % _cap.Width, 0, 1, _cap.Height),
                Color.White);

            // Rock: 1:1 immediately below the cap body, no vertical repeat.
            var rockHeight = bands.InteriorTop - bands.RockTop;
            if (rockHeight > 0)
                spriteBatch.Draw(
                    _rock,
                    new Rectangle(column, bands.RockTop, 1, rockHeight),
                    new Rectangle(column % _rock.Width, 0, 1, rockHeight),
                    Color.White);

            // Interior: world-anchored vertical tiling to the playfield bottom.
            var sourceColumn = column % _interior.Width;
            var row = bands.InteriorTop;
            while (row < mask.Height)
            {
                var sourceRow = TerrainBands.InteriorSourceRow(row, _interior.Height);
                var height = Math.Min(_interior.Height - sourceRow, mask.Height - row);
                spriteBatch.Draw(
                    _interior,
                    new Rectangle(column, row, 1, height),
                    new Rectangle(sourceColumn, sourceRow, 1, height),
                    Color.White);
                row += height;
            }
        }
    }
}
