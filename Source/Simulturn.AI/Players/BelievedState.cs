using Simulturn.AI.Analysis;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

namespace Simulturn.AI.Players;

/// <summary>
/// Constructs a full game state from a partially informed player view by filling the
/// unknowns with estimates: enemy buildings from the fog of war memories, enemy armies
/// from the estimated strength, remaining matter from the last observations.
/// The result is a guess to simulate on, not the truth.
/// </summary>
internal static class BelievedState
{
    public static GameState Build(PlayerGameState view, Army estimatedEnemyArmy, SettingsAnalysis analysis)
    {
        GameSettings gameSettings = view.GameSettings;
        var playerStates = ImmutableDictionary.CreateBuilder<string, PlayerState>();
        playerStates[view.PlayerId] = view.PlayerState;

        foreach (string opponentId in view.PlayerIds.Where(x => x != view.PlayerId).OrderBy(x => x))
        {
            var compounds = ImmutableDictionary.CreateBuilder<Hexagon, Compound>();
            foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
            {
                Compound compound = observation.OpponentCompounds.GetValueOrDefault(opponentId);
                if (!compound.IsEmpty)
                {
                    compounds[hexagon] = compound;
                }
            }

            // Place currently visible stacks where they are seen and the unseen remainder
            // at the opponent's home (their biggest known base or their starting hexagon).
            var armies = ImmutableDictionary.CreateBuilder<Hexagon, Army>();
            int visibleTotal = 0;
            foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
            {
                int count = observation.OpponentUnitCounts.GetValueOrDefault(opponentId);
                if (count > 0)
                {
                    armies[hexagon] = analysis.UniformFighterMix(count);
                    visibleTotal += count;
                }
            }
            int hidden = Math.Max(0, estimatedEnemyArmy.Total - visibleTotal);
            if (hidden > 0)
            {
                Hexagon home = compounds.Count > 0
                    ? compounds.OrderByDescending(x => x.Value.Sum()).ThenBy(x => x.Key.X).ThenBy(x => x.Key.Y).First().Key
                    : gameSettings.HexagonSettings
                        .Where(x => x.Value.PlayerInitialization?.StartingPlayerId == opponentId)
                        .Select(x => x.Key)
                        .OrderBy(x => x.X).ThenBy(x => x.Y)
                        .First();
                Army hiddenArmy = estimatedEnemyArmy.MultiplyAndRoundUp(hidden / (double)Math.Max(1, estimatedEnemyArmy.Total));
                armies[home] = armies.GetValueOrDefault(home) + hiddenArmy;
            }

            playerStates[opponentId] = new PlayerState()
            {
                Matter = gameSettings.StartMatter,
                UsedSpace = 0,
                AvailableSpace = 0,
                Armies = armies.ToImmutableDictionary(),
                Compounds = compounds.ToImmutableDictionary(),
                Trainings = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>>.Empty,
                Constructions = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>>.Empty,
                Researches = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>>.Empty,
                Visibilities = PlayerStateBuilder.GetVisibility(armies,
                    gameSettings.HexagonSettings.Keys,
                    gameSettings.PartialVisibilityRange,
                    gameSettings.VisibilityRange),
                Losses = ImmutableDictionary<Hexagon, Army>.Empty
            };
        }

        var remainingMatter = view.Observations.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.RemainingMatter ?? gameSettings.HexagonSettings[x.Key].Matter);
        return GameState.FromParts(gameSettings, view.Turn, playerStates.ToImmutableDictionary(), remainingMatter);
    }
}
