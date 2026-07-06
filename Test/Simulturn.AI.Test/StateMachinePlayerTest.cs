using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Test;

public class StateMachinePlayerTest
{
    [Test]
    public void ProducesValidCommandsFromTheStart()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var gameState = new GameState(gameSettings);
        var player = new StateMachinePlayer();

        var commands = player.GetCommands(gameState.GetPlayerGameState("Player01"));

        gameState.Validate("Player01", commands).ShouldBeEmpty();
        commands.ShouldNotBeEmpty(); // it should at least start training workers
    }

    [Test]
    public void DefeatsTheIdlePlayer()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var players = new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new StateMachinePlayer() },
            { "Player02", new IdlePlayer() }
        };

        // GameRunner validates every command, so this also proves a full game of valid commands.
        var result = GameRunner.Run(gameSettings, players, maxTurns: 200);

        result.EndReason.ShouldBe(GameEndReason.Victory);
        result.WinnerId.ShouldBe("Player01");
    }

    [Test]
    public void WinsMoreThan60PercentAgainstRandomPlayers()
    {
        var gameSettings = GameSettingsFactory.HexDisc();
        var stateMachine = new ArenaPlayer()
        {
            Name = "StateMachine",
            CreatePlayer = _ => new StateMachinePlayer()
        };
        RandomPlayerOptions[] presets =
        [
            new RandomPlayerOptions(),
            RandomPlayerOptions.Aggressive,
            RandomPlayerOptions.Defensive,
            RandomPlayerOptions.Expansive
        ];

        int wins = 0;
        int games = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            var random = new ArenaPlayer()
            {
                Name = "Random",
                CreatePlayer = seed => new RandomPlayer(seed, presets[i])
            };
            var results = Arena.Run(gameSettings,
                [stateMachine, random],
                new ArenaOptions() { GamesPerSeatPairing = 2, MaxTurns = 200, Seed = i * 100 });
            wins += results.Sum(x => x.WinsA);
            games += results.Sum(x => x.Games);
        }

        games.ShouldBe(16);
        (wins / (double)games).ShouldBeGreaterThan(0.6, $"StateMachine won only {wins}/{games} games");
    }
}
