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
    /// What a player last saw on the hexagons the current view does not show, taken from the
    /// earlier views of that player. Goes back turn by turn until every hexagon is settled.
    /// </summary>
    /// <remarks>
    /// The remaining matter and the buildings are remembered for every hexagon that is not at
    /// least partially visible now, the units for every hexagon that is not fully visible now.
    /// The two sightings of a hexagon can stem from different turns. A hexagon without any
    /// sighting has no entry. A view that is missing from the list is a turn without knowledge.
    /// </remarks>
    /// <param name="views">
    /// The views of one player in ascending turn order. The last one is the current view.
    /// </param>
    public static ImmutableDictionary<Hexagon, HexagonMemory> GetMemories(IReadOnlyList<PlayerGameState> views)
    {
        if (views.Count == 0)
        {
            throw new ArgumentException("At least the current view is required.", nameof(views));
        }
        for (int i = 1; i < views.Count; i++)
        {
            if (views[i].PlayerId != views[0].PlayerId)
            {
                throw new ArgumentException($"The views belong to different players: '{views[0].PlayerId}' and '{views[i].PlayerId}'.", nameof(views));
            }
            if (views[i].Turn <= views[i - 1].Turn)
            {
                throw new ArgumentException($"The views are not in ascending turn order: turn {views[i - 1].Turn} is followed by turn {views[i].Turn}.", nameof(views));
            }
        }

        HashSet<Hexagon> withoutStatic = [];
        HashSet<Hexagon> withoutUnits = [];
        foreach ((Hexagon hexagon, HexagonObservation observation) in views[^1].Observations)
        {
            if (observation.Visibility < StaticObservationThreshold)
            {
                withoutStatic.Add(hexagon);
            }
            if (observation.Visibility < UnitObservationThreshold)
            {
                withoutUnits.Add(hexagon);
            }
        }

        Dictionary<Hexagon, StaticSighting> staticSightings = [];
        Dictionary<Hexagon, UnitSighting> unitSightings = [];
        for (int i = views.Count - 2; i >= 0 && (withoutStatic.Count > 0 || withoutUnits.Count > 0); i--)
        {
            PlayerGameState view = views[i];
            foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
            {
                if (observation.Visibility >= StaticObservationThreshold && withoutStatic.Remove(hexagon))
                {
                    staticSightings[hexagon] = new StaticSighting()
                    {
                        Turn = view.Turn,
                        RemainingMatter = observation.RemainingMatter
                            ?? throw new InvalidOperationException($"The view of turn {view.Turn} shows {hexagon} as {observation.Visibility} but holds no remaining matter."),
                        OpponentCompounds = observation.OpponentCompounds
                    };
                }
                if (observation.Visibility >= UnitObservationThreshold && withoutUnits.Remove(hexagon))
                {
                    unitSightings[hexagon] = new UnitSighting()
                    {
                        Turn = view.Turn,
                        OpponentUnitCounts = observation.OpponentUnitCounts
                    };
                }
            }
        }

        var memories = ImmutableDictionary.CreateBuilder<Hexagon, HexagonMemory>();
        foreach (Hexagon hexagon in staticSightings.Keys.Union(unitSightings.Keys))
        {
            memories[hexagon] = new HexagonMemory()
            {
                Static = staticSightings.GetValueOrDefault(hexagon),
                Units = unitSightings.GetValueOrDefault(hexagon)
            };
        }
        return memories.ToImmutableDictionary();
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
    /// The view holds this turn only; <see cref="GetMemories"/> adds what was seen before.
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
