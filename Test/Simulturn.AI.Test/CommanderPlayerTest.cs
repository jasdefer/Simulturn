using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;
using Simulturn.Core.Model;
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
    public void DefeatsRandomPlayersOnEveryMapPreset()
    {
        (string Name, GameSettings Settings)[] maps =
        [
            ("disc", GameSettingsFactory.HexDisc()),
            ("noexpansion", GameSettingsFactory.NoExpansion()),
            ("rich", GameSettingsFactory.RichExpansions())
        ];
        var commander = new ArenaPlayer()
        {
            Name = "Commander",
            CreatePlayer = _ => new CommanderPlayer()
        };
        var random = new ArenaPlayer()
        {
            Name = "Random",
            CreatePlayer = seed => new RandomPlayer(seed)
        };
        foreach ((string name, GameSettings gameSettings) in maps)
        {
            var results = Arena.Run(gameSettings,
                [commander, random],
                new ArenaOptions() { GamesPerSeatPairing = 2, MaxTurns = 300 });
            results.Sum(x => x.WinsB).ShouldBe(0, $"Commander lost to Random on map '{name}'");
            results.Sum(x => x.WinsA).ShouldBeGreaterThanOrEqualTo(3, $"Commander won too few games on map '{name}'");
        }
    }

    [Test]
    public void ResearchesUpgradesWithBankedMatter()
    {
        // Upgrades are researchable on the starting hexagon of this map.
        var gameSettings = GameSettingsFactory.NoExpansion();
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new CommanderPlayer() },
            { "Player02", new IdlePlayer() }
        };

        bool researched = false;
        GameRunner.Run(gameSettings, players, maxTurns: 150, onTurnCompleted: state =>
            researched |= state.PlayerStates["Player01"].UpgradeLevels.Any(x => x.Value > 0));

        researched.ShouldBeTrue("the Commander never researched an upgrade despite banked matter");
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
