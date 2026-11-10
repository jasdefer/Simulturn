using Simulturn.AI.Players;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.AI.Test;

/// <summary>
/// Layer 3 of the test strategy: random games must satisfy the engine invariants on every turn.
/// The random players only produce valid commands, so every failure here is an engine bug.
/// </summary>
public class EngineInvariantsTest
{
    private const int Seeds = 8;
    private const ushort MaxTurns = 100;

    private static readonly RandomPlayerOptions[] _presets =
    [
        new RandomPlayerOptions(),
        RandomPlayerOptions.Uniform,
        RandomPlayerOptions.Aggressive,
        RandomPlayerOptions.Defensive,
        RandomPlayerOptions.Expansive
    ];

    /// <summary>
    /// A small disc with matter everywhere and upgrades in the center, plus the map types of the generator.
    /// </summary>
    private static IEnumerable<(string Name, GameSettings Settings)> MapPresets()
    {
        yield return ("SmallDisc", TestSettings.Create(Maps.Disc(2, matter: 1000)
            .SetItem(new Hexagon(-2, 0), Maps.Start("Player01"))
            .SetItem(new Hexagon(2, 0), Maps.Start("Player02"))
            .SetItem(new Hexagon(0, 0), Maps.Hexagon(1000).WithResearch(Upgrade.DotUpgrade, Upgrade.DotUpgrade))));
        yield return ("Open", DefaultSettings.Create(MapGeneratorOptions.Open));
        yield return ("HomeOnly", DefaultSettings.Create(MapGeneratorOptions.HomeOnly));
        yield return ("Expansions", DefaultSettings.Create(MapGeneratorOptions.Expansions));
    }

    private static Dictionary<string, IArtificialPlayer> RandomPlayers(int seed, Func<RandomPlayerOptions, RandomPlayerOptions>? adjust = null)
    {
        adjust ??= options => options;
        return new Dictionary<string, IArtificialPlayer>()
        {
            { "Player01", new RandomPlayer(seed, adjust(_presets[seed % _presets.Length])) },
            { "Player02", new RandomPlayer(seed + 1000, adjust(_presets[(seed + 1) % _presets.Length])) }
        };
    }

    /// <summary>
    /// Plays a game like <see cref="GameRunner"/>, but hands every resolved turn together with
    /// its commands and its predecessor to the callback.
    /// </summary>
    private static GameState PlayGame(GameSettings gameSettings,
        IReadOnlyDictionary<string, IArtificialPlayer> players,
        Action<GameState, Dictionary<string, IReadOnlyDictionary<Hexagon, Command>>, GameState> onTurnResolved)
    {
        GameState state = new(gameSettings);
        while (!state.IsGameOver && state.Turn < MaxTurns)
        {
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in state.PlayerIds)
            {
                commands[playerId] = players[playerId].GetCommands(state.GetPlayerGameState(playerId));
                state.Validate(playerId, commands[playerId]).ShouldBeEmpty($"{playerId} issued invalid commands on turn {state.Turn}.");
            }
            GameState next = state.NextTurn(commands);
            onTurnResolved(state, commands, next);
            state = next;
        }
        return state;
    }

    [Test]
    public void RandomGames_SatisfyTheStateInvariantsOnEveryTurn()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                GameState final = PlayGame(gameSettings, RandomPlayers(seed), (_, _, next) =>
                    StateInvariants.CheckState(next).ShouldBeEmpty($"Map {map}, seed {seed}, turn {next.Turn}."));

                final.Turn.ShouldBeGreaterThan((ushort)0);
            }
        }
    }

    [Test]
    public void RandomGamesWithoutResearch_ConserveMatter()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var players = RandomPlayers(seed, options => options with { ResearchProbability = 0 });
                PlayGame(gameSettings, players, (before, commands, after) =>
                    StateInvariants.CheckTransition(before, commands, after).ShouldBeEmpty($"Map {map}, seed {seed}, turn {after.Turn}."));
            }
        }
    }

    [Test]
    public void RandomGamesWithResearch_ConserveMatter()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var players = RandomPlayers(seed, options => options with { ResearchProbability = 0.5 });
                PlayGame(gameSettings, players, (before, commands, after) =>
                    StateInvariants.CheckTransition(before, commands, after).ShouldBeEmpty($"Map {map}, seed {seed}, turn {after.Turn}."));
            }
        }
    }

    [Test]
    public void SameSettingsAndSeeds_ProduceTheSameGame()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            List<string> firstRun = [];
            List<string> secondRun = [];
            PlayGame(gameSettings, RandomPlayers(seed: 3), (_, _, next) => firstRun.Add(GameStateSnapshot.Of(next)));
            PlayGame(gameSettings, RandomPlayers(seed: 3), (_, _, next) => secondRun.Add(GameStateSnapshot.Of(next)));

            secondRun.ShouldBe(firstRun, $"Map {map}.");
        }
    }

    /// <summary>
    /// The engine must not depend on the enumeration order of the command dictionaries.
    /// The reversed players issue the same commands as the plain ones, only in reverse order.
    /// </summary>
    [Test]
    public void CommandOrder_DoesNotChangeTheGame()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            List<string> plain = [];
            List<string> reversed = [];
            PlayGame(gameSettings, RandomPlayers(seed: 5), (_, _, next) => plain.Add(GameStateSnapshot.Of(next)));
            var reversedPlayers = RandomPlayers(seed: 5)
                .ToDictionary(x => x.Key, x => (IArtificialPlayer)new ReversedCommandsPlayer(x.Value));
            PlayGame(gameSettings, reversedPlayers, (_, _, next) => reversed.Add(GameStateSnapshot.Of(next)));

            reversed.ShouldBe(plain, $"Map {map}.");
        }
    }

    private sealed class ReversedCommandsPlayer(IArtificialPlayer inner) : IArtificialPlayer
    {
        public string Name => inner.Name;

        public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
        {
            Dictionary<Hexagon, Command> reversed = [];
            foreach ((Hexagon hexagon, Command command) in inner.GetCommands(playerGameState).Reverse())
            {
                reversed[hexagon] = command;
            }
            return reversed;
        }
    }
}
