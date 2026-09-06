using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ratillery.Core.Entities;
using Ratillery.Game.Animation;
using Ratillery.Game.Rendering;

namespace Ratillery.Game.Scenes;

/// <summary>
/// Playable-canvas scene: simple background, a flat non-interactive floor
/// band and a single animated rat standing on it. Layout follows the current
/// viewport so the rat stays on screen when the window is resized.
/// </summary>
public sealed class MainScene
{
    private const string IdleSpriteDir = "sprites/rats/base";
    private const string IdleSpriteStem = "idle";

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _debugFont;
    private readonly Texture2D _pixel;
    private readonly PlaceholderRat _placeholder;
    private readonly Rat _rat = new() { State = RatState.Idle };

    private SpriteAnimation? _idle;
    private string? _assetError;
    private int _frameCount;
    private float _fpsTimer;
    private int _fps;

    public MainScene(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _debugFont = content.Load<SpriteFont>("Debug");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _placeholder = new PlaceholderRat(graphicsDevice);

        try
        {
            var contentRoot = Path.Combine(AppContext.BaseDirectory, content.RootDirectory);
            _idle = SpriteAnimation.Load(graphicsDevice, contentRoot, IdleSpriteDir, IdleSpriteStem);
            if (!_idle.HasContent)
                _assetError = "Idle sprite unavailable (frame 0 failed to load)";
        }
        catch (Exception ex)
        {
            _assetError = $"Idle sprite failed to load: {ex.Message}";
            _idle = null;
        }
    }

    public void Update(GameTime gameTime)
    {
        var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _idle?.Update(deltaSeconds);

        _frameCount++;
        _fpsTimer += deltaSeconds;
        if (_fpsTimer >= 1f)
        {
            _fps = _frameCount;
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    public void Draw(GameTime gameTime)
    {
        var viewport = _graphicsDevice.Viewport;
        float groundTop = viewport.Height * 0.78f;

        _graphicsDevice.Clear(new Color(143, 183, 216));

        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.PointClamp);

        DrawFloor(viewport.Width, viewport.Height, groundTop);

        var anchorX = viewport.Width * 0.5f;
        _rat.Position = new System.Numerics.Vector2(anchorX, groundTop);
        var anchor = new Vector2(anchorX, groundTop);
        if (_idle is not null && _idle.HasContent)
        {
            var scale = Math.Min(1f, viewport.Height * 0.4f / _idle.PixelHeight);
            _idle.Draw(_spriteBatch, anchor, scale, _rat.FacingRight);
        }
        else
        {
            _placeholder.Draw(_spriteBatch, anchor, _rat.FacingRight);
        }

        _spriteBatch.End();

        _spriteBatch.Begin();
        var message = $"Ratillery\nFPS: {_fps}\nState: {_rat.State}";
        if (_assetError is not null)
            message += $"\n{_assetError} (showing placeholder)";
        _spriteBatch.DrawString(_debugFont, message, new Vector2(12, 12), Color.White);
        _spriteBatch.End();
    }

    private void DrawFloor(int screenWidth, int screenHeight, float groundTop)
    {
        var topY = (int)groundTop;
        _spriteBatch.Draw(_pixel, new Rectangle(0, topY, screenWidth, screenHeight - topY), new Color(88, 70, 52));
        _spriteBatch.Draw(_pixel, new Rectangle(0, topY - 2, screenWidth, 2), new Color(120, 100, 76));
    }
}
