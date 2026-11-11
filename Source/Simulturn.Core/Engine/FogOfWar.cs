using Simulturn.Core.Model;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

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
    /// The visibility of every hexagon a player with the given armies can see.
    /// Hexagons out of range are left out.
    /// </summary>
    public static ImmutableDictionary<Hexagon, Visibility> GetVisibility(IDictionary<Hexagon, Army> armies,
        IEnumerable<Hexagon> hexagons,
        byte partialVisibilityRange,
        byte visibilityRange)
    {
        ImmutableDictionary<Hexagon, Visibility>.Builder visibilities = ImmutableDictionary.CreateBuilder<Hexagon, Visibility>();
        foreach (Hexagon hexagon in hexagons)
        {
            Visibility visibility = Visibility.Mapped;
            foreach ((Hexagon armyHexagon, Army army) in armies)
            {
                if (army.IsEmpty)
                {
                    continue;
                }
                var distance = hexagon.DistanceTo(armyHexagon);
                if (distance == 0)
                {
                    visibility = Visibility.Occupied;
                    break;
                }
                if (distance <= visibilityRange)
                {
                    visibility = Visibility.Visible;
                    continue;
                }
                if (visibility < Visibility.Visible && distance <= partialVisibilityRange)
                {
                    visibility = Visibility.PartiallyVisible;
                }
            }
            if (visibility > Visibility.Unknown)
            {
                visibilities[hexagon] = visibility;
            }
        }
        return visibilities.ToImmutableDictionary();
    }

    /// <summary>
    /// Updates the <see cref="PlayerState.Memories"/> of all players with a snapshot of every hexagon
    /// that is at least partially visible to them on the given turn.
    /// </summary>
    internal static ImmutableDictionary<string, PlayerState> UpdateMemories(ImmutableDictionary<string, PlayerState> playerStates,
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
    /// Builds the partially informed view of the game for a single player from a state without
    /// a preceding turn, such as the start of the game. The report of the view is empty.
    /// </summary>
    public static PlayerGameState GetPlayerGameState(GameState gameState, string playerId)
    {
        return GetPlayerGameState(gameState, TurnReport.Empty, playerId);
    }

    /// <summary>
    /// Builds the partially informed view of the game for a single player: the state and the
    /// report of the resolved turn, both cut down to what the player is allowed to know.
    /// </summary>
    public static PlayerGameState GetPlayerGameState(TurnResult turnResult, string playerId)
    {
        return GetPlayerGameState(turnResult.State, turnResult.Report, playerId);
    }

    /// <summary>
    /// The part of the report a single player is allowed to know: the own activity, the fights
    /// the player took part in and the destroyed buildings the player lost or destroyed.
    /// A fight is handed over complete, with the armies and the losses of every participant.
    /// </summary>
    public static PlayerTurnReport GetPlayerTurnReport(TurnReport report, string playerId)
    {
        return new PlayerTurnReport()
        {
            Activity = report.Activities.GetValueOrDefault(playerId, PlayerActivityReport.Empty),
            Fights = report.Fights.IsEmpty
                ? report.Fights
                : report.Fights.Where(x => x.Value.Armies.ContainsKey(playerId)).ToImmutableDictionary(),
            Destructions = report.Destructions.IsEmpty
                ? report.Destructions
                : [.. report.Destructions.Where(x => x.AttackerId == playerId || x.OwnerId == playerId)]
        };
    }

    private static PlayerGameState GetPlayerGameState(GameState gameState, TurnReport report, string playerId)
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
            PlayerIds = gameState.PlayerIds,
            PlayerState = playerState,
            Observations = observations.ToImmutableDictionary(),
            Report = GetPlayerTurnReport(report, playerId),
            IsGameOver = gameState.IsGameOver
        };
    }
}
