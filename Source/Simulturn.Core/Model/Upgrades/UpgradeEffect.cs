namespace Simulturn.Core.Model.Upgrades;

/// <summary>
/// What an upgrade does at one level. The cases are the closed set of effect kinds; every kind
/// is applied at the one place of its rule: the fight exponent in <see cref="Engine.Combat.GetExponent"/>,
/// the start matter in <see cref="Engine.UpgradeRules.StartMatter"/>.
/// </summary>
public union UpgradeEffect(FightExponentBonus, StartMatterBonus)
{
    public override string ToString() => Value?.ToString() ?? nameof(UpgradeEffect);
}

/// <summary>
/// Raises the fight exponent of the unit types the upgrade acts on. The bonus is divided by
/// 100 like <see cref="GameSettings.FightExponent"/>, so 10 means 0.1.
/// </summary>
public sealed record FightExponentBonus(short Bonus);

/// <summary>
/// Raises the matter a player starts the game with. It counts only for a player who has the
/// upgrade from the start (<see cref="GameSettings.StartUpgrades"/>); researched during the
/// game it changes nothing. It acts on no unit type, so no fight shows it.
/// </summary>
public sealed record StartMatterBonus(int Matter);
