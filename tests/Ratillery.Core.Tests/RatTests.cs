using Ratillery.Core.Entities;
using Xunit;

namespace Ratillery.Core.Tests;

public class RatTests
{
    [Fact]
    public void NewRat_StartsIdleAndAlive()
    {
        var rat = new Rat();

        Assert.Equal(RatState.Idle, rat.State);
        Assert.True(rat.IsAlive);
        Assert.True(rat.FacingRight);
    }

    [Fact]
    public void TakeDamage_ReducesHealthAndKillsAtZero()
    {
        var rat = new Rat();

        rat.TakeDamage(60);
        Assert.Equal(40, rat.Health);
        Assert.True(rat.IsAlive);

        rat.TakeDamage(60);
        Assert.Equal(0, rat.Health);
        Assert.False(rat.IsAlive);
        Assert.Equal(RatState.Dead, rat.State);
    }
}
