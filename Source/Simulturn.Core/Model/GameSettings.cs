using System.Collections.Frozen;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Core.Model;

public sealed record GameSettings
{
    public GameSettings()
    {
    }

    public int StartMatter { get; init; }
    public required Army ArmyCost { get; init; }
    public required Army RequiredSpace { get; init; }
    public required Army TrainingDuration { get; init; }
    public required Army Income { get; init; }
    public required Army StructureDamage { get; init; }

    /// <summary>
    /// The number of hexagons every unit type moves per turn. The rule is applied by <see cref="Engine.Movement"/>.
    /// </summary>
    public required Army MovementRange { get; init; }
    public required byte PartialVisibilityRange { get; init; }
    public required byte VisibilityRange { get; init; }

    /// <summary>
    /// The values are divided by 100, so 100 means 1.0. It is the exponent for the fight calculation.
    /// Dots have no exponent: they count one by one, and the value for dots is never read.
    /// </summary>
    public required Army FightExponent { get; init; }
    public required Compound CompoundCost { get; init; }
    public required Compound ProvidedSpace { get; init; }
    public required Compound ConstructionDuration { get; init; }
    public required Compound Armor { get; init; }
    public required FrozenDictionary<Hexagon, HexagonSettings> HexagonSettings { get; init; }
    public required FrozenDictionary<string, ImmutableArray<Upgrade>> StartUpgrades { get; init; }
    public required FrozenDictionary<Upgrade, UpgradeDefinition> Upgrades { get; init; }
    public int Seed { get; init; }

    /// <summary>
    /// The players of the settings in seat order, seat 1 first: everyone who starts on a hexagon,
    /// sorted by id. The order means nothing to the rules, it only names the seats. This is the
    /// only place that puts the players in an order; whoever seats someone (a bot, a user) asks here.
    /// </summary>
    public ImmutableArray<string> SeatOrder()
    {
        return
        [
            .. HexagonSettings.Values
                .Where(x => x.PlayerInitialization is not null)
                .Select(x => x.PlayerInitialization!.Value.StartingPlayerId)
                .Distinct()
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// The number of levels defined for the upgrade in <see cref="Upgrades"/>. An upgrade without
    /// a definition has no levels and can never be researched, no matter which hexagon offers it.
    /// </summary>
    public int MaxUpgradeLevel(Upgrade upgrade)
    {
        return Upgrades.TryGetValue(upgrade, out var definition) ? definition.Levels.Length : 0;
    }

    /// <summary>
    /// The level a research of the upgrade buys for a player with the given number of completed
    /// levels, or null when the upgrade has no further level. This is the only place that reads
    /// the level after the completed ones.
    /// </summary>
    public UpgradeLevel? NextUpgradeLevel(Upgrade upgrade, int completedLevels)
    {
        // A level of 1 means the first level, which is at index 0.
        return completedLevels < MaxUpgradeLevel(upgrade) ? Upgrades[upgrade].Levels[completedLevels] : null;
    }

    public static Building TrainingBuilding(Unit unit) => unit switch
    {
        Unit.Dot => Building.Plane,
        Unit.Circle => Building.Dome,
        Unit.Triangle => Building.Pyramid,
        Unit.Square => Building.Cube,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };
}
