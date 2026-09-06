using System.Numerics;

namespace Ratillery.Core.Entities;

public sealed class Rat
{
    public Vector2 Position { get; set; }

    public RatState State { get; set; } = RatState.Idle;

    public int Health { get; private set; } = 100;

    public bool FacingRight { get; set; } = true;

    public bool IsAlive => Health > 0 && State != RatState.Dead;

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");

        Health = Math.Max(0, Health - amount);
        if (Health == 0)
            State = RatState.Dead;
    }
}
