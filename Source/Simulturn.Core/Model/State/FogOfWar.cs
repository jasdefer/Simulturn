namespace Simulturn.Core.Model.State;

/// <summary>
/// Computes what each player is allowed to know about the game.
/// </summary>
public static class FogOfWar
{
    /// <summary>
    /// The minimum visibility required to observe static information (remaining matter, buildings) on a hexagon.
    /// </summary>
    public const Visibility StaticObservationThreshold = Visibility.PartiallyVisible;

    /// <summary>
    /// The minimum visibility required to observe opponent units on a hexagon.
    /// </summary>
    public const Visibility UnitObservationThreshold = Visibility.Visible;

    /// <summary>
    /// Updates the <see cref="PlayerState.Memories"/> of all players with a snapshot of every hexagon
    /// that is at least partially visible to them on the given turn.
    /// </summary>
    public static ImmutableDictionary<string, PlayerState> UpdateMemories(ImmutableDictionary<string, PlayerState> playerStates,
        ImmutableDictionary<Hexagon, int> remainingMatter,
        ushort turn)
    {
        return playerStates.ToImmutableDictionary(x => x.Key, x =>
        {
            var memories = x.Value.Memories.ToBuilder();
            foreach ((Hexagon hexagon, Visibility visibility) in x.Value.Visibilities)
            {
                if (visibility < StaticObservationThreshold)
                {
                    continue;
                }
                var opponentCompounds = playerStates
                    .Where(y => y.Key != x.Key &&
                        y.Value.Compounds.TryGetValue(hexagon, out var compound) &&
                        !compound.IsEmpty)
                    .ToImmutableDictionary(y => y.Key, y => y.Value.Compounds[hexagon]);
                memories[hexagon] = new HexagonMemory()
                {
                    LastSeenTurn = turn,
                    RemainingMatter = remainingMatter.GetValueOrDefault(hexagon),
                    OpponentCompounds = opponentCompounds
                };
            }
            return x.Value with { Memories = memories.ToImmutableDictionary() };
        });
    }

    /// <summary>
    /// Builds the partially informed view of the game for a single player.
    /// </summary>
    public static PlayerGameState GetPlayerGameState(GameState gameState, string playerId)
    {
        if (!gameState.PlayerStates.TryGetValue(playerId, out var playerState))
        {
            throw new ArgumentException($"Unknown player id '{playerId}'.", nameof(playerId));
        }
        var observations = ImmutableDictionary.CreateBuilder<Hexagon, HexagonObservation>();
        foreach (Hexagon hexagon in gameState.Hexagons)
        {
            Visibility visibility = playerState.Visibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
            HexagonMemory? memory = playerState.Memories.GetValueOrDefault(hexagon);
            var opponentUnitCounts = ImmutableDictionary<string, int>.Empty;
            if (visibility >= UnitObservationThreshold)
            {
                opponentUnitCounts = gameState.PlayerStates
                    .Where(x => x.Key != playerId &&
                        x.Value.Armies.TryGetValue(hexagon, out var army) &&
                        !army.IsEmpty)
                    .ToImmutableDictionary(x => x.Key, x => x.Value.Armies[hexagon].Total);
            }
            observations[hexagon] = new HexagonObservation()
            {
                Visibility = visibility,
                LastSeenTurn = memory?.LastSeenTurn,
                RemainingMatter = memory?.RemainingMatter,
                OpponentCompounds = memory?.OpponentCompounds ?? ImmutableDictionary<string, Compound>.Empty,
                OpponentUnitCounts = opponentUnitCounts
            };
        }
        return new PlayerGameState()
        {
            PlayerId = playerId,
            Turn = gameState.Turn,
            GameSettings = gameState.GameSettings,
            PlayerIds = gameState.PlayerIds.ToImmutableHashSet(),
            PlayerState = playerState,
            Observations = observations.ToImmutableDictionary(),
            IsGameOver = gameState.IsGameOver
        };
    }
}
