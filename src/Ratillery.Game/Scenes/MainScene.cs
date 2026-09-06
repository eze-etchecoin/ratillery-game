using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ratillery.Core.Entities;
using Ratillery.Game.Rendering;

namespace Ratillery.Game.Scenes;

/// <summary>
/// Minimal playable scene: background, a placeholder rat on simple ground
/// and a debug overlay with FPS.
/// </summary>
public sealed class MainScene
{
    private readonly SpriteBatch _spriteBatch;
    private readonly PlaceholderRat _rat;
    private readonly SpriteFont _debugFont;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Rat _ratEntity = new()
    {
        Position = new System.Numerics.Vector2(160, 400),
    };

    private int _frameCount;
    private float _fpsTimer;
    private int _fps;

    public MainScene(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _rat = new PlaceholderRat(graphicsDevice);
        _debugFont = content.Load<SpriteFont>("Debug");
    }

    public void Update(GameTime gameTime)
    {
        _frameCount++;
        _fpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 1f)
        {
            _fps = _frameCount;
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(106, 150, 190));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawGround();
        _rat.Draw(_spriteBatch, new Vector2(_ratEntity.Position.X, _ratEntity.Position.Y), _ratEntity.FacingRight);

        _spriteBatch.End();

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_debugFont, $"Ratillery - proof of concept\nFPS: {_fps}\nState: {_ratEntity.State}", new Vector2(12, 12), Color.White);
        _spriteBatch.End();
    }

    private void DrawGround()
    {
        _spriteBatch.Draw(_rat.Pixel, new Rectangle(0, 424, 960, 96), new Color(96, 74, 58));
    }
}
