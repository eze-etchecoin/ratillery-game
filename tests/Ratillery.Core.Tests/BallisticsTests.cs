using Ratillery.Core.Physics;
using System.Numerics;
using Xunit;

namespace Ratillery.Core.Tests;

public class BallisticsTests
{
    [Fact]
    public void LaunchVelocity_HorizontalShot_MovesRightWithNoVerticalSpeed()
    {
        var velocity = Ballistics.LaunchVelocity(0f, 100f);

        Assert.Equal(100f, velocity.X, precision: 3);
        Assert.Equal(0f, velocity.Y, precision: 3);
    }

    [Fact]
    public void LaunchVelocity_VerticalShot_MovesUp()
    {
        var velocity = Ballistics.LaunchVelocity(MathF.PI / 2f, 100f);

        Assert.Equal(0f, velocity.X, precision: 3);
        Assert.Equal(-100f, velocity.Y, precision: 3);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(MathF.PI / 4f)]
    [InlineData(MathF.PI / 2f)]
    public void LaunchVelocity_PreservesPower(float angle)
    {
        var velocity = Ballistics.LaunchVelocity(angle, 250f);

        Assert.Equal(250f, velocity.Length(), precision: 3);
    }

    [Fact]
    public void Integrate_AppliesGravityAndMovesPosition()
    {
        var position = new Vector2(0f, 0f);
        var velocity = new Vector2(10f, 0f);

        Ballistics.Integrate(ref position, ref velocity, Ballistics.DefaultGravity, 1f);

        Assert.Equal(10f, position.X, precision: 3);
        Assert.Equal(Ballistics.DefaultGravity, position.Y, precision: 3);
        Assert.Equal(Ballistics.DefaultGravity, velocity.Y, precision: 3);
    }
}
