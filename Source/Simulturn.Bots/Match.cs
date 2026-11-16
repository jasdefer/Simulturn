using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

namespace Simulturn.Bots;

/// <summary>
/// Plays one game between bots to its end. The engine and the maps do not roll dice, so a match
/// follows from the settings and the bots alone: the same bots on the same map play the same match.
/// </summary>
public static class Match
{
    /// <summary>
    /// Plays the game of the settings with one bot per player, keyed by player id.
    /// Every bot gets the view of its player and nothing else.
    /// </summary>
    /// <exception cref="ArgumentException">The bots are not exactly the players of the settings.</exception>
    /// <exception cref="InvalidBotCommandsException">A bot issued invalid commands.</exception>
    public static MatchResult Play(GameSettings settings, IReadOnlyDictionary<string, IBot> bots, MatchLimits limits)
    {
        TurnResult turn = Game.Start(settings);
        if (!turn.State.PlayerIds.SetEquals(bots.Keys))
        {
            throw new ArgumentException(
                $"The bots ({string.Join(", ", bots.Keys.Order(StringComparer.Ordinal))}) are not the players of the settings ({string.Join(", ", turn.State.PlayerIds.Order(StringComparer.Ordinal))}).",
                nameof(bots));
        }
        ImmutableArray<string> playerIds = [.. turn.State.PlayerIds.Order(StringComparer.Ordinal)];

        int stillTurns = 0;
        while (true)
        {
            GameStanding standing = Standing.Of(turn.State);
            switch (standing.Outcome.Value)
            {
                case Victory victory:
                    return new MatchResult(MatchEnd.Victory, victory.WinnerId, turn.State.Turn);
                case MutualDefeat:
                    return new MatchResult(MatchEnd.MutualDefeat, null, turn.State.Turn);
            }
            if (turn.State.Turn >= limits.MaxTurns)
            {
                return new MatchResult(MatchEnd.TurnLimit, null, turn.State.Turn);
            }
            if (stillTurns >= limits.StallTurns)
            {
                return new MatchResult(MatchEnd.Stall, null, turn.State.Turn);
            }

            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in playerIds)
            {
                if (!standing.DefeatedPlayerIds.Contains(playerId))
                {
                    commands[playerId] = bots[playerId].GetCommands(FogOfWar.GetPlayerGameState(turn, playerId));
                }
            }

            switch (Game.PlayTurn(turn.State, commands).Value)
            {
                case TurnResult next:
                    turn = next;
                    break;
                case RefusedCommands refused:
                    string culprit = refused.Validations.Keys.Order(StringComparer.Ordinal).First();
                    throw new InvalidBotCommandsException(culprit, turn.State.Turn, refused.Validations[culprit]);
                default:
                    throw new InvalidOperationException("The turn attempt holds no value.");
            }
            stillTurns = IsStill(turn.Report) ? stillTurns + 1 : 0;
        }
    }

    /// <summary>
    /// Whether nothing happened in a turn: no fight, no destruction, no defeat, and for every
    /// player no movement, no income, no completed or canceled order and no change of matter.
    /// A new order costs matter, so a turn with one is never still.
    /// </summary>
    public static bool IsStill(TurnReport report)
    {
        return report.Fights.IsEmpty
            && report.Destructions.IsEmpty
            && report.Defeats.IsEmpty
            && report.Activities.Values.All(IsStill);
    }

    private static bool IsStill(PlayerActivityReport activity)
    {
        return activity.MatterBefore == activity.MatterAfter
            && activity.Incomes.IsEmpty
            && activity.Movements.IsEmpty
            && activity.CompletedTrainings.IsEmpty
            && activity.CompletedConstructions.IsEmpty
            && activity.CompletedResearches.IsEmpty
            && activity.CanceledTrainings.IsEmpty
            && activity.CanceledConstructions.IsEmpty
            && activity.CanceledResearches.IsEmpty;
    }
}
