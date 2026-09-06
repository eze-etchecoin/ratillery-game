using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ratillery.Core.Entities;
using Ratillery.Core.Terrain;
using Ratillery.Game.Animation;
using Ratillery.Game.Rendering;

namespace Ratillery.Game.Scenes;

/// <summary>
/// Playable-canvas scene (R-4): a fixed 1280 x 720 world-unit playfield holds
/// the procedural terrain — drawn exactly where the Core collision mask is
/// solid, from its surface down to the playfield bottom — and a single
/// animated rat standing on the terrain surface at the playfield's horizontal
/// center. The whole playfield is uniformly scaled and centered to fit the
/// window (letterboxed if the aspect differs); resizing never changes the
/// world, terrain or mask. The RAT-001 flat staging floor is gone (R-3).
/// </summary>
public sealed class MainScene
{
    private const string IdleSpriteDir = "sprites/rats/base";
    private const string IdleSpriteStem = "idle";

    /// <summary>Rat height as a fraction of the playfield height (RAT-001 look).</summary>
    private const float RatSizeWorldFraction = 0.4f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _debugFont;
    private readonly PlaceholderRat _placeholder;
    private readonly PlaceholderTerrain _terrain;
    private readonly TerrainConfig _terrainConfig = new();
    private readonly TerrainMask _terrainMask;
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
        _placeholder = new PlaceholderRat(graphicsDevice);
        _terrain = new PlaceholderTerrain(graphicsDevice);
        _terrainMask = new TerrainMask(_terrainConfig);

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
        _graphicsDevice.Clear(new Color(143, 183, 216));

        var view = ComputePlayfieldView(viewport.Width, viewport.Height);

        // World space: the mask is the only source of terrain geometry.
        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.PointClamp, transformMatrix: view.Matrix);

        _terrain.Draw(_spriteBatch, _terrainMask, _terrainConfig);

        // Bottom-center pivot rests exactly on the mask surface at the
        // horizontal center of the playfield (R-5, AC-8).
        var ratColumn = _terrainMask.Width / 2;
        var ratAnchor = new Vector2(_terrainMask.Width * 0.5f, _terrainMask.SurfaceHeight(ratColumn));
        _rat.Position = new System.Numerics.Vector2(ratAnchor.X, ratAnchor.Y);
        if (_idle is not null && _idle.HasContent)
        {
            var scale = Math.Min(1f, _terrainConfig.Height * RatSizeWorldFraction / _idle.PixelHeight);
            _idle.Draw(_spriteBatch, ratAnchor, scale, _rat.FacingRight);
        }
        else
        {
            _placeholder.Draw(_spriteBatch, ratAnchor, _rat.FacingRight);
        }

        _spriteBatch.End();

        DrawHud(view);
    }

    /// <summary>
    /// Maps the fixed world playfield onto the window: uniform scale to fit,
    /// centered, letterboxed when the aspect differs (R-4).
    /// </summary>
    private PlayfieldView ComputePlayfieldView(int viewportWidth, int viewportHeight)
    {
        var scale = Math.Min(viewportWidth / (float)_terrainConfig.Width, viewportHeight / (float)_terrainConfig.Height);
        var offsetX = (viewportWidth - _terrainConfig.Width * scale) * 0.5f;
        var offsetY = (viewportHeight - _terrainConfig.Height * scale) * 0.5f;
        return new PlayfieldView(scale, offsetX, offsetY);
    }

    private void DrawHud(PlayfieldView view)
    {
        _spriteBatch.Begin();

        var message = $"Ratillery\nFPS: {_fps}\nState: {_rat.State}";
        if (_assetError is not null)
            message += $"\n{_assetError} (showing placeholder)";
        _spriteBatch.DrawString(_debugFont, message, new Vector2(12, 12), Color.White);

        // DEC-002 marker: clearly identifies this placeholder presentation.
        var marker = "TERRAIN PLACEHOLDER";
        var markerSize = _debugFont.MeasureString(marker);
        var bottomCenter = view.WorldToScreen(new Vector2(_terrainConfig.Width * 0.5f, _terrainConfig.Height));
        var markerPosition = new Vector2(bottomCenter.X - markerSize.X * 0.5f, bottomCenter.Y - markerSize.Y - 8f);
        _spriteBatch.DrawString(_debugFont, marker, markerPosition, Color.White);

        _spriteBatch.End();
    }

    private readonly record struct PlayfieldView(float Scale, float OffsetX, float OffsetY)
    {
        public Matrix Matrix => Matrix.CreateScale(Scale) * Matrix.CreateTranslation(OffsetX, OffsetY, 0f);

        public Vector2 WorldToScreen(Vector2 world) => new(world.X * Scale + OffsetX, world.Y * Scale + OffsetY);
    }
}
