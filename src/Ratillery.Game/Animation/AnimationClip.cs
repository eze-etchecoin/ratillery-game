using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ratillery.Game.Animation;

/// <summary>
/// A horizontal sprite strip animation. All frames share the same dimensions
/// and the character stays aligned to a consistent pivot.
/// </summary>
public sealed class AnimationClip
{
    public required string Name { get; init; }

    public required Texture2D Texture { get; init; }

    public int FrameWidth { get; init; }

    public int FrameHeight { get; init; }

    public int FrameCount { get; init; }

    public float FrameDuration { get; init; } = 1f / 10f;

    public bool Loop { get; init; } = true;

    public AnimationClip(ContentManager content, string assetName, int frameWidth, int frameHeight, int frameCount)
    {
        Name = assetName;
        Texture = content.Load<Texture2D>(assetName);
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCount = frameCount;
    }
}
