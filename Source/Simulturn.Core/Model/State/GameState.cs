using Simulturn.Core.Engine;

namespace Simulturn.Core.Model.State;

public sealed record GameState
{
    public GameSettings GameSettings { get; init; }
    public ImmutableHashSet<Hexagon> Hexagons { get; init; }
    public ImmutableHashSet<string> PlayerIds { get; init; }
    public ImmutableDictionary<string, PlayerState> PlayerStates { get; init; }
    public ImmutableDictionary<Hexagon, int> RemainingMatter { get; init; }

    /// <summary>
    /// The neutral armies that are still standing, keyed by hexagon. Holds no empty entries.
    /// See <see cref="Neutral"/>.
    /// </summary>
    public ImmutableDictionary<Hexagon, Army> NeutralArmies { get; init; }

    public ushort Turn { get; init; }

    public GameState(GameSettings gameSettings)
    {
        GameSettings = gameSettings;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        RemainingMatter = GameSettings.HexagonSettings.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.Matter);
        PlayerStates = GameSettings.HexagonSettings.Where(x => x.Value.PlayerInitialization is not null)
            .GroupBy(x => x.Value.PlayerInitialization!.Value.StartingPlayerId)
            .ToImmutableDictionary(x => x.Key, x =>
            {
                ImmutableDictionary<Upgrade, byte> upgradeLevels = UpgradeRules.StartLevels(GameSettings, x.Key);
                return new PlayerState()
                {
                    Armies = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialArmy),
                    Compounds = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialCompound),
                    AvailableSpace = x.Sum(y => Occupancy.ProvidedSpace(GameSettings, y.Value.PlayerInitialization!.Value.InitialCompound)),
                    UsedSpace = x.Sum(y => Costs.Training(GameSettings, y.Value.PlayerInitialization!.Value.InitialArmy).Space),
                    Constructions = ImmutableDictionary<TurnHexagon, Compound>.Empty,
                    Trainings = ImmutableDictionary<TurnHexagon, Army>.Empty,
                    Matter = UpgradeRules.StartMatter(GameSettings, upgradeLevels),
                    Researches = ImmutableDictionary<Hexagon, Research>.Empty,
                    UpgradeLevels = upgradeLevels
                };
            });
        PlayerIds = PlayerStates.Keys.ToImmutableHashSet();
        if (PlayerIds.Contains(Neutral.Id))
        {
            throw new ArgumentException($"The player id '{Neutral.Id}' is reserved for the neutral armies.", nameof(gameSettings));
        }
        NeutralArmies = GameSettings.HexagonSettings
            .Where(x => !x.Value.NeutralArmy.IsEmpty)
            .ToImmutableDictionary(x => x.Key, x => x.Value.NeutralArmy);
        foreach (Hexagon hexagon in NeutralArmies.Keys)
        {
            if (GameSettings.HexagonSettings[hexagon].PlayerInitialization is not null)
            {
                throw new ArgumentException($"{hexagon} is a start hexagon and holds a neutral army. A player cannot start on a hexagon with guards.", nameof(gameSettings));
            }
        }
    }

    /// <summary>
    /// Creates a game state from explicit parts. Intended for client side what-if simulation
    /// of believed states (e.g. by bots); real games only ever evolve through
    /// <see cref="TurnResolution.Resolve"/>. The caller is responsible for the consistency of the parts.
    /// </summary>
    public static GameState FromParts(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter,
        ImmutableDictionary<Hexagon, Army> neutralArmies)
    {
        return new GameState(gameSettings, turn, playerStates, remainingMatter, neutralArmies);
    }

    private GameState(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter,
        ImmutableDictionary<Hexagon, Army> neutralArmies)
    {
        GameSettings = gameSettings;
        PlayerIds = playerStates.Keys.ToImmutableHashSet();
        Turn = turn;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        RemainingMatter = remainingMatter;
        NeutralArmies = neutralArmies;
        PlayerStates = playerStates;
    }
}