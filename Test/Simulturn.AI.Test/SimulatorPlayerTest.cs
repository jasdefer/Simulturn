using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;
using Simulturn.Core.Engine;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;

namespace Simulturn.AI.Test;

public sealed class SimulatorPlayerTest
{
    [Test]
    public void ProducesValidCommandsFromTheStart()
    {
        var gameSettings = DefaultSettings.Create(MapGeneratorOptions.Open);
        var gameState = new GameState(gameSettings);
        var player = new SimulatorPlayer();

        var commands = player.GetCommands(FogOfWar.GetPlayerGameState(gameState, "Player01"));

        CommandValidator.Validate(gameState, "Player01", commands).ShouldBeEmpty();
        commands.ShouldNotBeEmpty();
    }

    [Test]
    public void DefeatsTheIdlePlayer()
    {
        var gameSettings = DefaultSettings.Create(MapGeneratorOptions.Open);
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new SimulatorPlayer() },
            { "Player02", new IdlePlayer() }
        };

        var result = GameRunner.Run(gameSettings, players, maxTurns: 200);

        result.EndReason.ShouldBe(GameEndReason.Victory);
        result.WinnerId.ShouldBe("Player01");
    }

    [Test]
    public void NeverLosesToTheCommander()
    {
        var gameSettings = DefaultSettings.Create(MapGeneratorOptions.Open);
        var simulator = new ArenaPlayer()
        {
            Name = "Simulator",
            CreatePlayer = _ => new SimulatorPlayer()
        };
        var commander = new ArenaPlayer()
        {
            Name = "Commander",
            CreatePlayer = _ => new CommanderPlayer()
        };

        // Both players are deterministic, so one game per seat covers all distinct outcomes.
        var results = Arena.Run(gameSettings,
            [simulator, commander],
            new ArenaOptions() { GamesPerSeatPairing = 1, MaxTurns = 300 });

        results.Sum(x => x.WinsB).ShouldBe(0, "the Simulator must never lose to its predecessor");
    }
}
