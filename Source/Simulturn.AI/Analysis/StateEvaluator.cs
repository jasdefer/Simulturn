using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Analysis;

/// <summary>
/// Scores a game state for one player as the difference between the own assets and the
/// strongest opponent's assets. All weights are derived from the game settings costs,
/// so the evaluation adapts to the settings instead of hardcoding unit values.
/// </summary>
public static class StateEvaluator
{
    /// <summary>
    /// How many turns of income are counted as an asset.
    /// </summary>
    public const int IncomeHorizon = 20;

    public static double Evaluate(GameState gameState, string playerId)
    {
        double own = PlayerValue(gameState, playerId);
        double strongestOpponent = gameState.PlayerIds
            .Where(x => x != playerId)
            .Select(x => PlayerValue(gameState, x))
            .DefaultIfEmpty(0)
            .Max();
        return own - strongestOpponent;
    }

    private static double PlayerValue(GameState gameState, string playerId)
    {
        GameSettings gameSettings = gameState.GameSettings;
        PlayerState playerState = gameState.PlayerStates[playerId];
        double armyValue = playerState.Armies.Values.Sum(x => (double)(x * gameSettings.ArmyCost));
        double buildingValue = playerState.Compounds.Values.Sum(x => (double)(x * gameSettings.CompoundCost));

        // Expected income: workers on own income hexagons with matter left, up to the hexagon cap.
        double incomePerTurn = 0;
        foreach ((Hexagon hexagon, Compound compound) in playerState.Compounds)
        {
            if (compound.Plane <= 0 || gameState.RemainingMatter.GetValueOrDefault(hexagon) <= 0)
            {
                continue;
            }
            int workers = playerState.Armies.GetValueOrDefault(hexagon).Dot;
            int cap = gameSettings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter.Dot;
            incomePerTurn += Math.Min(workers, cap) * gameSettings.Income.Dot;
        }

        double value = playerState.Matter + armyValue + buildingValue + IncomeHorizon * incomePerTurn;
        if (playerState.Compounds.All(x => x.Value.IsEmpty))
        {
            value -= 1_000_000; // no buildings means the player is eliminated
        }
        return value;
    }
}
