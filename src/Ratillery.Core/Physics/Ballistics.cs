using System.Numerics;

namespace Ratillery.Core.Physics;

/// <summary>
/// Simple ballistic math for turn-based artillery gameplay.
/// Coordinates use screen convention: Y grows downwards, gravity is positive Y.
/// </summary>
public static class Ballistics
{
    public const float DefaultGravity = 980f;

    /// <summary>
    /// Initial velocity for a shot. Angle in radians, measured from the
    /// horizontal, positive upwards.
    /// </summary>
    public static Vector2 LaunchVelocity(float angleRadians, float power)
    {
        return new Vector2(
            MathF.Cos(angleRadians) * power,
            -MathF.Sin(angleRadians) * power);
    }

    /// <summary>
    /// Semi-implicit Euler integration step for a projectile.
    /// </summary>
    public static void Integrate(ref Vector2 position, ref Vector2 velocity, float gravity, float deltaTime)
    {
        if (deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");

        velocity.Y += gravity * deltaTime;
        position += velocity * deltaTime;
    }
}
