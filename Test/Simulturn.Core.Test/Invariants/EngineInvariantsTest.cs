using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.Invariants;

/// <summary>
/// Layer 3 of the test strategy: random games must satisfy the engine invariants on every turn.
/// <see cref="RandomCommands"/> only produces valid commands, so every failure here is an engine bug.
/// </summary>
public sealed class EngineInvariantsTest
{
    private const int Seeds = 8;
    private const ushort MaxTurns = 100;

    /// <summary>
    /// The order invariants validate dozens of commands per player and turn, so they play fewer games.
    /// </summary>
    private const int OrderSeeds = 3;
    private static readonly Dictionary<Hexagon, Command> _noCommands = [];

    /// <summary>
    /// What gives the commands of one player for a turn, from the view of that player.
    /// </summary>
    private delegate IReadOnlyDictionary<Hexagon, Command> CommandSource(PlayerGameState view);

    private static readonly RandomCommandsOptions[] _presets =
    [
        new RandomCommandsOptions(),
        RandomCommandsOptions.Uniform,
        RandomCommandsOptions.Aggressive,
        RandomCommandsOptions.Defensive,
        RandomCommandsOptions.Expansive
    ];

    /// <summary>
    /// A small disc with matter everywhere and upgrades in the center, plus the map types of the
    /// generator and a map with neutral armies in the center and on the expansions.
    /// </summary>
    private static IEnumerable<(string Name, GameSettings Settings)> MapPresets()
    {
        yield return ("SmallDisc", TestSettings.Create(Maps.Disc(2, matter: 1000)
            .SetItem(new Hexagon(-2, 0), Maps.Start("Player01"))
            .SetItem(new Hexagon(2, 0), Maps.Start("Player02"))
            .SetItem(new Hexagon(0, 0), Maps.Hexagon(1000).WithResearch(Upgrade.TriangleExponent, Upgrade.CircleExponent, Upgrade.SquareExponent))));
        yield return ("Open", DefaultSettings.Create(MapGeneratorOptions.Open));
        yield return ("HomeOnly", DefaultSettings.Create(MapGeneratorOptions.HomeOnly));
        yield return ("Expansions", DefaultSettings.Create(MapGeneratorOptions.Expansions));
        yield return ("Guarded", DefaultSettings.Create(MapGeneratorOptions.Expansions with
        {
            CenterGuard = new Army() { Triangle = 2, Circle = 2, Square = 2 },
            ExpansionGuard = new Army() { Dot = 3 }
        }));
    }

    /// <summary>
    /// Fresh command sources for one game: the same seed only gives the same game with new instances.
    /// </summary>
    private static Dictionary<string, CommandSource> RandomPlayers(int seed, Func<RandomCommandsOptions, RandomCommandsOptions>? adjust = null)
    {
        adjust ??= options => options;
        return new Dictionary<string, CommandSource>()
        {
            { "Player01", new RandomCommands(seed, adjust(_presets[seed % _presets.Length])).GetCommands },
            { "Player02", new RandomCommands(seed + 1000, adjust(_presets[(seed + 1) % _presets.Length])).GetCommands }
        };
    }

    /// <summary>
    /// Plays a game up to the turn limit and hands every resolved turn together with
    /// its commands and its predecessor to the callback.
    /// </summary>
    private static GameState PlayGame(GameSettings gameSettings,
        IReadOnlyDictionary<string, CommandSource> players,
        Action<GameState, Dictionary<string, IReadOnlyDictionary<Hexagon, Command>>, TurnResult> onTurnResolved)
    {
        TurnResult turnResult = new(new GameState(gameSettings), TurnReport.Empty);
        while (!Standing.Of(turnResult.State).IsGameOver && turnResult.State.Turn < MaxTurns)
        {
            GameState state = turnResult.State;
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach (string playerId in state.PlayerIds)
            {
                commands[playerId] = players[playerId](FogOfWar.GetPlayerGameState(turnResult, playerId));
                CommandValidator.Validate(state, playerId, commands[playerId]).ShouldBeEmpty($"{playerId} issued invalid commands on turn {state.Turn}.");
            }
            turnResult = TurnResolution.Resolve(state, commands);
            onTurnResolved(state, commands, turnResult);
        }
        return turnResult.State;
    }

    [Test]
    public void RandomGames_SatisfyTheStateInvariantsOnEveryTurn()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                GameState final = PlayGame(gameSettings, RandomPlayers(seed), (_, _, next) =>
                    StateInvariants.CheckState(next.State).ShouldBeEmpty($"Map {map}, seed {seed}, turn {next.State.Turn}."));

                final.Turn.ShouldBeGreaterThan((ushort)0);
            }
        }
    }

    /// <summary>
    /// The commands of a random player are a turn half entered: what <see cref="Orders.Possible"/>
    /// offers on top of them is exactly what the validation accepts.
    /// </summary>
    [Test]
    public void RandomGames_OfferExactlyTheOrdersTheValidationAccepts()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < OrderSeeds; seed++)
            {
                var players = RandomPlayers(seed, options => options with { ResearchProbability = 0.5 });
                PlayGame(gameSettings, players, (before, commands, _) =>
                {
                    foreach ((string playerId, IReadOnlyDictionary<Hexagon, Command> playerCommands) in commands)
                    {
                        PlayerGameState view = FogOfWar.GetPlayerGameState(before, playerId);
                        OrderInvariants.Check(view, playerCommands).ShouldBeEmpty($"Map {map}, seed {seed}, turn {before.Turn}.");
                        OrderInvariants.Check(view, _noCommands).ShouldBeEmpty($"Map {map}, seed {seed}, turn {before.Turn}, without commands.");
                    }
                });
            }
        }
    }

    [Test]
    public void RandomGamesWithoutResearch_ConserveMatterAndMatchTheReport()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var players = RandomPlayers(seed, options => options with { ResearchProbability = 0 });
                PlayGame(gameSettings, players, (before, commands, after) =>
                    StateInvariants.CheckTransition(before, commands, after).ShouldBeEmpty($"Map {map}, seed {seed}, turn {after.State.Turn}."));
            }
        }
    }

    [Test]
    public void RandomGamesWithResearch_ConserveMatterAndMatchTheReport()
    {
        foreach ((string map, GameSettings gameSettings) in MapPresets())
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var players = RandomPlayers(seed, options => options with { ResearchProbability = 0.5 });
                PlayGame(gameSettings, players, (before, commands, after) =>
                    StateInvariants.CheckTransition(before, commands, after).ShouldBeEmpty($"Map {map}, seed {seed}, turn {after.State.Turn}."));
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
            PlayGame(gameSettings, RandomPlayers(seed: 3), (_, _, next) => firstRun.Add(GameStateSnapshot.Of(next.State)));
            PlayGame(gameSettings, RandomPlayers(seed: 3), (_, _, next) => secondRun.Add(GameStateSnapshot.Of(next.State)));

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
            PlayGame(gameSettings, RandomPlayers(seed: 5), (_, _, next) => plain.Add(GameStateSnapshot.Of(next.State)));
            var reversedPlayers = RandomPlayers(seed: 5)
                .ToDictionary(x => x.Key, x => Reversed(x.Value));
            PlayGame(gameSettings, reversedPlayers, (_, _, next) => reversed.Add(GameStateSnapshot.Of(next.State)));

            reversed.ShouldBe(plain, $"Map {map}.");
        }
    }

    private static CommandSource Reversed(CommandSource inner)
    {
        return view =>
        {
            Dictionary<Hexagon, Command> reversed = [];
            foreach ((Hexagon hexagon, Command command) in inner(view).Reverse())
            {
                reversed[hexagon] = command;
            }
            return reversed;
        };
    }
}
