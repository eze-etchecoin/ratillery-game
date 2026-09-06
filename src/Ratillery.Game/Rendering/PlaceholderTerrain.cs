using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ratillery.Core.Terrain;

namespace Ratillery.Game.Rendering;

/// <summary>
/// DEC-002 placeholder for the real terrain art (R-2, delayed): draws exactly
/// the cells the Core collision mask reports as solid, in three flat ordered
/// depth bands (surface grass/earth, rock, dark interior) that read darker
/// with depth (AC-2/AC-6/AC-9). Real art, when delivered and processed, will
/// only replace this fill appearance — never the mask or the rat placement.
/// </summary>
public sealed class PlaceholderTerrain
{
    private static readonly Color SurfaceColor = new(120, 130, 78);
    private static readonly Color RockColor = new(104, 94, 86);
    private static readonly Color InteriorColor = new(64, 50, 42);

    private readonly Texture2D _pixel;

    public PlaceholderTerrain(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, TerrainMask mask, TerrainConfig config)
    {
        for (var column = 0; column < mask.Width; column++)
        {
            var surfaceTop = mask.SurfaceHeight(column);
            var rockTop = Math.Min(mask.Height, surfaceTop + config.SurfaceBandPixels);
            var interiorTop = Math.Min(mask.Height, surfaceTop + config.SurfaceBandPixels + config.RockBandPixels);

            Fill(spriteBatch, column, surfaceTop, rockTop, SurfaceColor);
            Fill(spriteBatch, column, rockTop, interiorTop, RockColor);
            Fill(spriteBatch, column, interiorTop, mask.Height, InteriorColor);
        }
    }

    private void Fill(SpriteBatch spriteBatch, int column, int top, int bottom, Color color)
    {
        if (bottom > top)
            spriteBatch.Draw(_pixel, new Rectangle(column, top, 1, bottom - top), color);
    }
}
