using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulturn.Web.Services;

/// <summary>
/// Debug JSON dumps via flat DTO projection. A DTO layer sidesteps everything System.Text.Json
/// chokes on in the engine model: struct dictionary keys (Hexagon), the PlayerInitialization
/// tuple, and IUpgrade polymorphism. This is also the designated slot-in point for save/load.
/// </summary>
public static class GameStateSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ToJson(GameState state) => JsonSerializer.Serialize(ToDto(state), _options);

    public static string ToJson(GameSettings settings) => JsonSerializer.Serialize(ToDto(settings), _options);

    private static object ToDto(GameState state) => new
    {
        state.Turn,
        state.IsGameOver,
        Hexagons = state.Hexagons
            .OrderBy(hexagon => hexagon.Z).ThenBy(hexagon => hexagon.X)
            .Select(hexagon => new
            {
                hexagon.X,
                hexagon.Y,
                RemainingMatter = state.RemainingMatter.GetValueOrDefault(hexagon),
            }),
        Players = state.PlayerStates.OrderBy(pair => pair.Key).Select(pair => new
        {
            Id = pair.Key,
            pair.Value.Matter,
            pair.Value.UsedSpace,
            pair.Value.AvailableSpace,
            Armies = PerHex(pair.Value.Armies),
            Compounds = PerHex(pair.Value.Compounds),
            Trainings = PerTurnPerHex(pair.Value.Trainings),
            Constructions = PerTurnPerHex(pair.Value.Constructions),
            Researches = PerTurnPerHex(pair.Value.Researches),
            Losses = PerHex(pair.Value.Losses),
            UpgradeLevels = pair.Value.UpgradeLevels.OrderBy(level => level.Key)
                .ToDictionary(level => level.Key.ToString(), level => level.Value),
            Visibilities = PerHex(pair.Value.Visibilities),
        }),
    };

    private static object ToDto(GameSettings settings) => new
    {
        settings.Seed,
        settings.StartMatter,
        settings.ArmyCost,
        settings.RequiredSpace,
        settings.TrainingDuration,
        settings.Income,
        settings.MovementRange,
        settings.StructureDamage,
        settings.FightExponent,
        settings.CompoundCost,
        settings.ProvidedSpace,
        settings.ConstructionDuration,
        settings.Armor,
        settings.PartialVisibilityRange,
        settings.VisibilityRange,
        Upgrades = settings.Upgrades.OrderBy(pair => pair.Key).ToDictionary(
            pair => pair.Key.ToString(),
            pair => pair.Value.Select(ToDto)),
        StartUpgrades = settings.StartUpgrades.OrderBy(pair => pair.Key).ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(upgrade => upgrade.ToString())),
        Hexagons = settings.HexagonSettings
            .OrderBy(pair => pair.Key.Z).ThenBy(pair => pair.Key.X)
            .Select(pair => new
            {
                pair.Key.X,
                pair.Key.Y,
                pair.Value.Matter,
                pair.Value.IsBuildable,
                MaxHarvesters = pair.Value.MaxNumberOfUnitsGeneratingMatter,
                ResearchableUpgrades = pair.Value.ResearchableUpgrades.Select(upgrade => upgrade.ToString()),
                Start = pair.Value.PlayerInitialization is { } init
                    ? new { init.StartingPlayerId, init.InitialArmy, init.InitialCompound }
                    : null,
            }),
    };

    private static object ToDto(IUpgrade upgrade) => upgrade switch
    {
        DotUpgrade dot => new { dot.Cost, dot.Duration, dot.ExponentBonus },
        UpgradeStartMatter matter => new { matter.Cost, matter.Duration, matter.Matter },
        _ => new { upgrade.Cost, upgrade.Duration },
    };

    private static IEnumerable<object> PerHex<T>(IReadOnlyDictionary<Hexagon, T> values) =>
        values.OrderBy(pair => pair.Key.Z).ThenBy(pair => pair.Key.X)
            .Select(pair => (object)new { pair.Key.X, pair.Key.Y, Value = pair.Value });

    private static IEnumerable<object> PerTurnPerHex<T>(
        IReadOnlyDictionary<ushort, System.Collections.Immutable.ImmutableDictionary<Hexagon, T>> queues) =>
        queues.OrderBy(pair => pair.Key)
            .SelectMany(pair => pair.Value
                .OrderBy(entry => entry.Key.Z).ThenBy(entry => entry.Key.X)
                .Select(entry => (object)new { CompletionTurn = pair.Key, entry.Key.X, entry.Key.Y, Value = entry.Value }));
}
