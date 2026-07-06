using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Test;

public class CommanderPlayerTest
{
    [Test]
    public void ProducesValidCommandsFromTheStart()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var gameState = new GameState(gameSettings);
        var player = new CommanderPlayer();

        var commands = player.GetCommands(gameState.GetPlayerGameState("Player01"));

        gameState.Validate("Player01", commands).ShouldBeEmpty();
        commands.ShouldNotBeEmpty();
    }

    [Test]
    public void DefeatsTheIdlePlayer()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new CommanderPlayer() },
            { "Player02", new IdlePlayer() }
        };

        var result = GameRunner.Run(gameSettings, players, maxTurns: 200);

        result.EndReason.ShouldBe(GameEndReason.Victory);
        result.WinnerId.ShouldBe("Player01");
    }

    [Test]
    public void OutperformsTheStateMachinePlayer()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var commander = new ArenaPlayer()
        {
            Name = "Commander",
            CreatePlayer = _ => new CommanderPlayer()
        };
        var stateMachine = new ArenaPlayer()
        {
            Name = "StateMachine",
            CreatePlayer = _ => new StateMachinePlayer()
        };

        // Both players are deterministic, so one game per seat covers all distinct outcomes.
        var results = Arena.Run(gameSettings,
            [commander, stateMachine],
            new ArenaOptions() { GamesPerSeatPairing = 1, MaxTurns = 300 });

        int commanderWins = results.Sum(x => x.WinsA);
        int stateMachineWins = results.Sum(x => x.WinsB);
        stateMachineWins.ShouldBe(0, "the Commander must never lose to its predecessor");
        commanderWins.ShouldBeGreaterThan(stateMachineWins,
            $"Commander {commanderWins} vs StateMachine {stateMachineWins} ({results.Sum(x => x.Draws)} draws)");
    }
}
