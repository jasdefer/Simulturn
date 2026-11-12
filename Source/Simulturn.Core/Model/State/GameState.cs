using Simulturn.Core.Engine;

namespace Simulturn.Core.Model.State;

public sealed record GameState
{
    public GameSettings GameSettings { get; init; }
    public ImmutableHashSet<Hexagon> Hexagons { get; init; }
    public ImmutableHashSet<string> PlayerIds { get; init; }
    public ImmutableDictionary<string, PlayerState> PlayerStates { get; init; }
    public ImmutableDictionary<Hexagon, int> RemainingMatter { get; init; }
    public bool IsGameOver => PlayerStates.Where(x => x.Value.Compounds.Any(compound => !compound.Value.IsEmpty)).Count() <= 1;

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
                return new PlayerState()
                {
                    Armies = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialArmy),
                    Compounds = x.ToImmutableDictionary(y => y.Key, y => y.Value.PlayerInitialization!.Value.InitialCompound),
                    AvailableSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialCompound * GameSettings.ProvidedSpace),
                    UsedSpace = x.Sum(y => y.Value.PlayerInitialization!.Value.InitialArmy * GameSettings.RequiredSpace),
                    Constructions = Constructions.Empty,
                    Trainings = Trainings.Empty,
                    Matter = GameSettings.StartMatter,
                    Researches = ImmutableDictionary<Hexagon, Research>.Empty,
                    UpgradeLevels = GameSettings.StartUpgrades.ContainsKey(x.Key)
                        ? GameSettings.StartUpgrades[x.Key].GroupBy(x => x).ToImmutableDictionary(x => x.Key, x => Convert.ToByte(x.Count()))
                        : ImmutableDictionary<Upgrade, byte>.Empty
                };
            });
        PlayerIds = PlayerStates.Keys.ToImmutableHashSet();
    }

    /// <summary>
    /// Creates a game state from explicit parts. Intended for client side what-if simulation
    /// of believed states (e.g. by AI players); real games only ever evolve through
    /// <see cref="TurnResolution.Resolve"/>. The caller is responsible for the consistency of the parts.
    /// </summary>
    public static GameState FromParts(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter)
    {
        return new GameState(gameSettings, turn, playerStates, remainingMatter);
    }

    private GameState(GameSettings gameSettings,
        ushort turn,
        ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter)
    {
        GameSettings = gameSettings;
        PlayerIds = playerStates.Keys.ToImmutableHashSet();
        Turn = turn;
        Hexagons = GameSettings.HexagonSettings.Keys.ToImmutableHashSet();
        RemainingMatter = remainingMatter;
        PlayerStates = playerStates;
    }
}