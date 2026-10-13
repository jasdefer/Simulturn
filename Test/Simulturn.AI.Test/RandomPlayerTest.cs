using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.AI.Test;

public class RandomPlayerTest
{
    /// <summary>
    /// A hexagon disc with radius 2 (19 hexagons). The players start on opposite edges,
    /// upgrades can be researched in the center.
    /// </summary>
    private static GameSettings CreateSettings()
    {
        Dictionary<Hexagon, HexagonSettings> hexagonSettings = [];
        for (short x = -2; x <= 2; x++)
        {
            for (short y = -2; y <= 2; y++)
            {
                if (Math.Abs(x + y) > 2)
                {
                    continue;
                }
                Hexagon hexagon = new(x, y);
                hexagonSettings[hexagon] = new HexagonSettings()
                {
                    Matter = 1000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                    ResearchableUpgrades = hexagon == new Hexagon(0, 0) ? [Upgrade.DotUpgrade, Upgrade.DotUpgrade] : [],
                    PlayerInitialization = hexagon switch
                    {
                        { X: -2, Y: 0 } => ("Player01", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        { X: 2, Y: 0 } => ("Player02", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        _ => ((string, Army, Compound)?)null
                    }
                };
            }
        }
        return new GameSettings()
        {
            Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
            ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
            CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
            ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
            FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
            Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
            MovementRange = new Army() { Triangle = 100, Circle = 100, Square = 100, Dot = 100 },
            ProvidedSpace = new Compound() { Axis = 10, Dome = 0, Cube = 0, Plane = 10, Pyramid = 0 },
            RequiredSpace = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 1 },
            Seed = 1,
            StartMatter = 500,
            PartialVisibilityRange = 2,
            VisibilityRange = 1,
            StructureDamage = new Army() { Triangle = 5, Circle = 5, Square = 5, Dot = 1 },
            TrainingDuration = new Army() { Triangle = 2, Circle = 2, Square = 2, Dot = 1 },
            StartUpgrades = ImmutableDictionary<string, ImmutableArray<Upgrade>>.Empty,
            Upgrades = new IUpgrade[]
                {
                    new DotUpgrade() { Cost = 150, Duration = 3, ExponentBonus = 20 },
                    new DotUpgrade() { Cost = 175, Duration = 4, ExponentBonus = 20 }
                }.ToDictionary(),
            HexagonSettings = hexagonSettings.ToImmutableDictionary()
        };
    }

    private static readonly Func<int, RandomPlayerOptions>[] _optionPresets =
    [
        _ => new RandomPlayerOptions(),
        _ => RandomPlayerOptions.Uniform,
        _ => RandomPlayerOptions.Aggressive,
        _ => RandomPlayerOptions.Defensive,
        _ => RandomPlayerOptions.Expansive
    ];

    /// <summary>
    /// The central contract of every artificial player: the produced commands always pass validation.
    /// Doubles as a fuzz test for the engine, because every resolved turn must produce a valid state.
    /// </summary>
    [Test]
    public void RandomGames_NeverProduceInvalidCommandsOrStates()
    {
        var gameSettings = CreateSettings();
        for (int seed = 0; seed < 15; seed++)
        {
            var players = new Dictionary<string, IArtificialPlayer>()
            {
                { "Player01", new RandomPlayer(seed, _optionPresets[seed % _optionPresets.Length](seed)) },
                { "Player02", new RandomPlayer(seed + 1000, _optionPresets[(seed + 1) % _optionPresets.Length](seed)) }
            };

            // GameRunner throws InvalidCommandsException on any invalid command.
            var result = GameRunner.Run(gameSettings,
                players,
                maxTurns: 100,
                onTurnCompleted: state => state.IsValid().ShouldBeEmpty($"Invalid state in game with seed {seed} on turn {state.Turn}"));

            result.Turns.ShouldBeGreaterThan((ushort)0);
        }
    }

    /// <summary>
    /// The validity contract must hold on every map preset, including maps without
    /// expansions and maps with contested rich expansions.
    /// </summary>
    [Test]
    public void RandomGames_AreValidOnEveryMapPreset()
    {
        GameSettings[] maps =
        [
            GameSettingsFactory.HexDisc(),
            GameSettingsFactory.NoExpansion(),
            GameSettingsFactory.RichExpansions()
        ];
        foreach (GameSettings gameSettings in maps)
        {
            for (int seed = 0; seed < 4; seed++)
            {
                var players = new Dictionary<string, IArtificialPlayer>()
                {
                    { "Player01", new RandomPlayer(seed) },
                    { "Player02", new RandomPlayer(seed + 1000, RandomPlayerOptions.Aggressive) }
                };
                GameRunner.Run(gameSettings,
                    players,
                    maxTurns: 100,
                    onTurnCompleted: state => state.IsValid().ShouldBeEmpty($"Invalid state with seed {seed} on turn {state.Turn}"));
            }
        }
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
