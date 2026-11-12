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
    /// Hexagons out of range are left out. The visibility is never stored: it follows from
    /// the armies and the settings alone and is computed where it is needed.
    /// </summary>
    public static ImmutableDictionary<Hexagon, Visibility> GetVisibility(IReadOnlyDictionary<Hexagon, Army> armies,
        GameSettings gameSettings)
    {
        byte partialVisibilityRange = gameSettings.PartialVisibilityRange;
        byte visibilityRange = gameSettings.VisibilityRange;
        ImmutableDictionary<Hexagon, Visibility>.Builder visibilities = ImmutableDictionary.CreateBuilder<Hexagon, Visibility>();
        foreach (Hexagon hexagon in gameSettings.HexagonSettings.Keys)
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
    /// Carries the memories of a player into the next turn: what the view shows is taken over
    /// with the turn of the view, everything else stays as it was last seen.
    /// </summary>
    /// <remarks>
    /// The remaining matter and the buildings are seen on every hexagon that is at least
    /// partially visible, the units on every hexagon that is fully visible, so the two
    /// sightings of a hexagon can stem from different turns. A sighting with the turn of the
    /// view is what the player sees right now. A hexagon that was never seen has no entry.
    /// </remarks>
    /// <param name="memories">The memories up to the turn before the view. Empty at the start of the game.</param>
    /// <param name="view">The view of the current turn.</param>
    public static ImmutableDictionary<Hexagon, HexagonMemory> UpdateMemories(ImmutableDictionary<Hexagon, HexagonMemory> memories,
        PlayerGameState view)
    {
        var builder = memories.ToBuilder();
        Remember(builder, view);
        return builder.ToImmutable();
    }

    /// <summary>
    /// The memories of a player after the last of the given views, for a client that kept its
    /// views but not its memories. The same as applying <see cref="UpdateMemories"/> view by view.
    /// </summary>
    /// <param name="views">
    /// The views of one player in ascending turn order. A view that is missing is a turn
    /// without knowledge.
    /// </param>
    public static ImmutableDictionary<Hexagon, HexagonMemory> GetMemories(IReadOnlyList<PlayerGameState> views)
    {
        var memories = ImmutableDictionary.CreateBuilder<Hexagon, HexagonMemory>();
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i].PlayerId != views[0].PlayerId)
            {
                throw new ArgumentException($"The views belong to different players: '{views[0].PlayerId}' and '{views[i].PlayerId}'.", nameof(views));
            }
            if (i > 0 && views[i].Turn <= views[i - 1].Turn)
            {
                throw new ArgumentException($"The views are not in ascending turn order: turn {views[i - 1].Turn} is followed by turn {views[i].Turn}.", nameof(views));
            }
            Remember(memories, views[i]);
        }
        return memories.ToImmutable();
    }

    private static void Remember(ImmutableDictionary<Hexagon, HexagonMemory>.Builder memories, PlayerGameState view)
    {
        foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
        {
            if (observation.Visibility < StaticObservationThreshold)
            {
                // Nothing of the hexagon is in sight; the units need at least as much visibility.
                continue;
            }
            StaticSighting staticSighting = new()
            {
                Turn = view.Turn,
                RemainingMatter = observation.RemainingMatter
                    ?? throw new InvalidOperationException($"The view of turn {view.Turn} shows {hexagon} as {observation.Visibility} but holds no remaining matter."),
                OpponentCompounds = observation.OpponentCompounds
            };
            UnitSighting? unitSighting = observation.Visibility >= UnitObservationThreshold
                ? new UnitSighting() { Turn = view.Turn, OpponentUnitCounts = observation.OpponentUnitCounts }
                : memories.GetValueOrDefault(hexagon)?.Units;
            memories[hexagon] = new HexagonMemory() { Static = staticSighting, Units = unitSighting };
        }
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
    /// The view holds this turn only; <see cref="UpdateMemories"/> carries what was seen before.
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
        var visibilities = GetVisibility(playerState.Armies, gameState.GameSettings);
        var observations = ImmutableDictionary.CreateBuilder<Hexagon, HexagonObservation>();
        foreach (Hexagon hexagon in gameState.Hexagons)
        {
            Visibility visibility = visibilities.GetValueOrDefault(hexagon, Visibility.Unknown);
            int? remainingMatter = null;
            var opponentCompounds = ImmutableDictionary<string, Compound>.Empty;
            if (visibility >= StaticObservationThreshold)
            {
                remainingMatter = gameState.RemainingMatter.GetValueOrDefault(hexagon);
                opponentCompounds = gameState.PlayerStates
                    .Where(x => x.Key != playerId &&
                        x.Value.Compounds.TryGetValue(hexagon, out var compound) &&
                        !compound.IsEmpty)
                    .ToImmutableDictionary(x => x.Key, x => x.Value.Compounds[hexagon]);
            }
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
                RemainingMatter = remainingMatter,
                OpponentCompounds = opponentCompounds,
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
