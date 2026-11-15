using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Analysis;

/// <summary>
/// Scores a game state for one player as the difference between the own assets and the
/// strongest opponent's assets. All weights are derived from the game settings costs,
/// so the evaluation adapts to the settings instead of hardcoding unit values.
/// </summary>
internal static class StateEvaluator
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
        double armyValue = playerState.Armies.Values.Sum(x => (double)Costs.Training(gameSettings, x).Matter);
        double buildingValue = playerState.Compounds.Values.Sum(x => (double)Costs.Construction(gameSettings, x).Matter);

        // Expected income: what the armies harvest in a turn without new constructions.
        double incomePerTurn = 0;
        foreach ((Hexagon hexagon, Army army) in playerState.Armies)
        {
            Compound compound = playerState.Compounds.GetValueOrDefault(hexagon);
            incomePerTurn += Harvest.Of(gameSettings, hexagon, army, compound, Compound.Empty, gameState.RemainingMatter.GetValueOrDefault(hexagon)).Matter;
        }

        double value = playerState.Matter + armyValue + buildingValue + IncomeHorizon * incomePerTurn;
        if (playerState.Compounds.All(x => x.Value.IsEmpty))
        {
            value -= 1_000_000; // no buildings means the player is eliminated
        }
        return value;
    }
}
