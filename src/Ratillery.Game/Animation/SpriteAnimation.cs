using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ratillery.Game.Animation;

/// <summary>
/// Plays a sprite-sheet animation whose parameters come from a
/// <see cref="SpriteSheetMetadata"/> sidecar (frame count, frame size, fps,
/// loop flag, pivot). Frames are loaded per file from the sheet's
/// <c>frames/</c> directory; a frame that is missing or fails to load is
/// replaced by frame 0 so a single broken frame never crashes the game or
/// stops the loop.
/// </summary>
public sealed class SpriteAnimation
{
    private readonly SpriteSheetMetadata _metadata;
    private readonly IReadOnlyList<Texture2D> _frames;
    private float _elapsed;

    public int CurrentFrame { get; private set; }

    public bool HasContent => _frames.Count > 0;

    public int PixelHeight => _frames[0].Height;

    private float FrameDuration => 1f / _metadata.Fps;

    private SpriteAnimation(SpriteSheetMetadata metadata, IReadOnlyList<Texture2D> frames)
    {
        _metadata = metadata;
        _frames = frames;
    }

    public static SpriteAnimation Load(GraphicsDevice graphicsDevice, string contentRoot, string spriteDir, string stem)
    {
        var metadata = SpriteSheetMetadata.Load(Path.Combine(contentRoot, spriteDir, $"{stem}.json"));
        var framesDir = Path.Combine(contentRoot, spriteDir, "frames");
        var frames = new List<Texture2D>(metadata.FrameCount);

        var fallbackFrame = TryLoadFrame(graphicsDevice, framesDir, stem, 0);
        if (fallbackFrame is not null)
        {
            frames.Add(fallbackFrame);
            for (var i = 1; i < metadata.FrameCount; i++)
                frames.Add(TryLoadFrame(graphicsDevice, framesDir, stem, i) ?? fallbackFrame);
        }

        return new SpriteAnimation(metadata, frames);
    }

    private static Texture2D? TryLoadFrame(GraphicsDevice graphicsDevice, string framesDir, string stem, int index)
    {
        try
        {
            return Texture2D.FromFile(graphicsDevice, Path.Combine(framesDir, $"{stem}_{index:D2}.png"));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Update(float deltaSeconds)
    {
        if (!HasContent || deltaSeconds <= 0f)
            return;

        if (!_metadata.Loop && CurrentFrame >= _frames.Count - 1)
            return;

        // Clamp the step so returning from a pause (huge delta) never bursts
        // through many skipped frames; playback stays tied to elapsed time.
        _elapsed += Math.Min(deltaSeconds, FrameDuration * 2f);
        while (_elapsed >= FrameDuration)
        {
            _elapsed -= FrameDuration;
            if (CurrentFrame + 1 < _frames.Count)
            {
                CurrentFrame++;
                continue;
            }

            if (_metadata.Loop)
            {
                CurrentFrame = 0;
                continue;
            }

            _elapsed = 0f;
            break;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 anchor, float scale, bool facingRight)
    {
        if (!HasContent)
            return;

        var frame = _frames[CurrentFrame];
        var origin = new Vector2(frame.Width * _metadata.Pivot.X, frame.Height * _metadata.Pivot.Y);
        var effects = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        spriteBatch.Draw(frame, anchor, null, Color.White, 0f, origin, scale, effects, 0f);
    }
}
