namespace Simulturn.Core.Model.Upgrades;

/// <summary>
/// What an upgrade does at one level. The cases are the closed set of effect kinds; every kind
/// is applied at the one place of its rule (the fight exponent in <see cref="Engine.Combat"/>).
/// </summary>
public union UpgradeEffect(FightExponentBonus)
{
    public override string ToString() => Value?.ToString() ?? nameof(UpgradeEffect);
}

/// <summary>
/// Raises the fight exponent of the unit types the upgrade acts on. The bonus is divided by
/// 100 like <see cref="GameSettings.FightExponent"/>, so 10 means 0.1.
/// </summary>
public sealed record FightExponentBonus(short Bonus);
