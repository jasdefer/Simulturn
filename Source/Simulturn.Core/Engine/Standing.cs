using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

/// <summary>
/// The rule of defeat and of the end of the game. The turn resolution, the command validation,
/// the player view and a client that runs a game all ask here.
/// </summary>
public static class Standing
{
    /// <summary>
    /// A player without a building and without a pending construction is defeated. A defeated
    /// player takes no more orders (<see cref="CommandValidator"/>), so nothing can be built
    /// again and the defeat is final without being stored.
    /// </summary>
    public static bool IsDefeated(PlayerState playerState)
    {
        return IsDefeated(playerState.Compounds, playerState.Constructions);
    }

    /// <summary>
    /// The same rule for the buildings and the pending constructions of a player.
    /// </summary>
    public static bool IsDefeated(IReadOnlyDictionary<Hexagon, Compound> compounds, IReadOnlyDictionary<TurnHexagon, Compound> constructions)
    {
        return constructions.Values.All(x => x.IsEmpty) && compounds.Values.All(x => x.IsEmpty);
    }

    /// <summary>
    /// The defeated players and the outcome of the game. The game runs while at least two
    /// players are not defeated.
    /// </summary>
    public static GameStanding Of(GameState state)
    {
        ImmutableHashSet<string> defeated = [.. state.PlayerStates.Where(x => IsDefeated(x.Value)).Select(x => x.Key)];
        List<string> remaining = [.. state.PlayerStates.Keys.Where(x => !defeated.Contains(x))];
        GameOutcome outcome = remaining.Count switch
        {
            0 => new MutualDefeat(),
            1 => new Victory(remaining[0]),
            _ => new GameRunning()
        };
        return new GameStanding() { DefeatedPlayerIds = defeated, Outcome = outcome };
    }
}
