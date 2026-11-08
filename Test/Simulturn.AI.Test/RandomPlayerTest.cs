using Simulturn.AI.Players;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Test.Common;

namespace Simulturn.AI.Test;

public class RandomPlayerTest
{
    /// <summary>
    /// A hexagon disc with radius 2 (19 hexagons). The players start on opposite edges,
    /// upgrades can be researched in the center.
    /// </summary>
    private static GameSettings CreateSettings()
    {
        return TestSettings.Create(Maps.Disc(2, matter: 1000)
            .SetItem(new Hexagon(-2, 0), Maps.Start("Player01"))
            .SetItem(new Hexagon(2, 0), Maps.Start("Player02"))
            .SetItem(new Hexagon(0, 0), Maps.Hexagon(1000).WithResearch(Upgrade.DotUpgrade, Upgrade.DotUpgrade)));
    }

    [Test]
    public void RandomPlayer_IsDeterministicForASeed()
    {
        var gameSettings = CreateSettings();
        GameResult Play()
        {
            var players = new Dictionary<string, IArtificialPlayer>()
            {
                { "Player01", new RandomPlayer(42) },
                { "Player02", new RandomPlayer(7, RandomPlayerOptions.Aggressive) }
            };
            return GameRunner.Run(gameSettings, players, maxTurns: 50);
        }

        var first = Play();
        var second = Play();

        second.Turns.ShouldBe(first.Turns);
        second.EndReason.ShouldBe(first.EndReason);
        second.WinnerId.ShouldBe(first.WinnerId);
        foreach (string playerId in first.FinalState.PlayerIds)
        {
            second.FinalState.PlayerStates[playerId].Matter.ShouldBe(first.FinalState.PlayerStates[playerId].Matter);
            second.FinalState.PlayerStates[playerId].UsedSpace.ShouldBe(first.FinalState.PlayerStates[playerId].UsedSpace);
        }
    }

    [Test]
    public void RandomPlayer_ActuallyIssuesCommands()
    {
        var gameSettings = CreateSettings();
        var gameState = new GameState(gameSettings);
        var view = gameState.GetPlayerGameState("Player01");

        int nonEmptyCommandSets = 0;
        for (int seed = 0; seed < 10; seed++)
        {
            var commands = new RandomPlayer(seed).GetCommands(view);
            gameState.Validate("Player01", commands).ShouldBeEmpty();
            if (commands.Count > 0)
            {
                nonEmptyCommandSets++;
            }
        }

        // A random player must do something at least occasionally, otherwise it is an idle player.
        nonEmptyCommandSets.ShouldBeGreaterThan(5);
    }
}
