using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ratillery.Game.Rendering;

/// <summary>
/// Procedural placeholder rat drawn with rectangles until the real sprite
/// sheets are available. Body grey, belly light, ears/tail pink, green helmet.
/// </summary>
public sealed class PlaceholderRat
{
    private readonly Texture2D _pixel;

    public Texture2D Pixel => _pixel;

    public PlaceholderRat(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, bool facingRight)
    {
        const int scale = 4;
        const int bodyW = 10, bodyH = 6;
        const int headW = 6, headH = 6;

        int dir = facingRight ? 1 : -1;
        float x = position.X;
        float y = position.Y;

        void Rect(float rx, float ry, int w, int h, Color color) =>
            spriteBatch.Draw(_pixel, new Rectangle((int)(x + rx * dir * scale), (int)(y + ry * scale), w * scale * dir, h * scale), color);

        // Body
        Rect(-bodyW, -bodyH, bodyW, bodyH, new Color(140, 120, 110));
        // Belly
        Rect(-bodyW + 1, -bodyH + 3, bodyW - 2, 3, new Color(214, 200, 190));
        // Head
        Rect(0, -bodyH - headH + 2, headW, headH, new Color(140, 120, 110));
        // Ear
        Rect(headW - 4, -bodyH - headH - 1, 2, 2, new Color(232, 160, 160));
        // Snout
        Rect(headW, -2, 2, 2, new Color(232, 160, 160));
        // Legs
        Rect(-bodyW + 1, 0, 2, 1, new Color(232, 160, 160));
        Rect(-3, 0, 2, 1, new Color(232, 160, 160));
        // Tail
        Rect(-bodyW - 4, -3, 4, 1, new Color(232, 160, 160));
        // Helmet
        Rect(-1, -bodyH - headH - 1, headW + 2, 3, new Color(86, 110, 62));
    }
}
