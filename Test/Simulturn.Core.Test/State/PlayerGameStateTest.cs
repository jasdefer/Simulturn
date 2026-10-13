using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public class PlayerGameStateTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-3, 0);
    private static readonly Hexagon _player2Start = new Hexagon(3, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A map of 7 hexagons in a line from (-3,0) to (3,0).
    /// The players start on the opposite ends, 6 hexagons apart, with 5 dots and a plane each.
    /// With a visibility range of 1 and a partial visibility range of 2, the players cannot see each other initially.
    /// </summary>
    private static readonly GameSettings _gameSettings = new()
    {
        Armor = new Compound() { Axis = 10, Dome = 20, Cube = 20, Plane = 30, Pyramid = 20 },
        ArmyCost = new Army() { Triangle = 200, Circle = 200, Square = 200, Dot = 75 },
        CompoundCost = new Compound() { Axis = 150, Dome = 100, Cube = 150, Plane = 350, Pyramid = 150 },
        ConstructionDuration = new Compound() { Axis = 2, Dome = 3, Cube = 3, Plane = 4, Pyramid = 3 },
        FightExponent = new Army() { Triangle = 120, Circle = 120, Square = 120, Dot = 1 },
        Income = new Army() { Triangle = 0, Circle = 0, Square = 0, Dot = 10 },
        // Generous ranges: these tests exercise fog of war; range rules are tested separately.
        MovementRange = new Army() { Triangle = 10, Circle = 10, Square = 10, Dot = 10 },
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
        HexagonSettings = Enumerable.Range(-3, 7)
            .ToImmutableDictionary(
                x => new Hexagon((short)x, 0),
                x => new HexagonSettings()
                {
                    IsBuildable = true,
                    Matter = 1000,
                    MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 12 },
                    PlayerInitialization = x switch
                    {
                        -3 => ("Player01", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        3 => ("Player02", new Army() { Dot = 5 }, new Compound() { Plane = 1 }),
                        _ => ((string, Army, Compound)?)null
                    }
                })
    };

    private static GameState GetNextTurnAndValidate(GameState gameState, Dictionary<string, Dictionary<Hexagon, Command>> commands)
    {
        foreach ((string playerId, Dictionary<Hexagon, Command> playerCommands) in commands)
        {
            gameState.Validate(playerId, playerCommands).ShouldBeEmpty();
        }
        var newState = gameState.NextTurn(commands);
        newState.IsValid().ShouldBeEmpty();
        return newState;
    }

    private static Dictionary<string, Dictionary<Hexagon, Command>> Move(string playerId, Hexagon from, Hexagon to, Army army)
    {
        return DictionaryExtensions.MovementsToDictionary([(playerId, from, to, army)]);
    }

    /// <summary>
    /// Places the second player on the given hexagon instead of its default start.
    /// </summary>
    private static GameSettings WithPlayer2At(Hexagon hexagon)
    {
        var builder = _gameSettings.HexagonSettings.ToBuilder();
        builder[_player2Start] = builder[_player2Start] with { PlayerInitialization = null };
        builder[hexagon] = builder[hexagon] with
        {
            PlayerInitialization = ("Player02", new Army() { Dot = 5 }, new Compound() { Plane = 1 })
        };
        return _gameSettings with { HexagonSettings = builder.ToImmutableDictionary() };
    }

    [Test]
    public void InitialView_ContainsOwnSurroundingsAndHidesDistantHexagons()
    {
        var gameState = new GameState(_gameSettings);
        var view = gameState.GetPlayerGameState("Player01");

        view.PlayerId.ShouldBe("Player01");
        view.Turn.ShouldBe((ushort)0);
        view.IsGameOver.ShouldBeFalse();
        view.PlayerIds.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        view.PlayerState.Matter.ShouldBe(500);
        view.Observations.Count.ShouldBe(7);

        // Own start hexagon is occupied and fully known.
        view.Observations[_player1Start].Visibility.ShouldBe(Visibility.Occupied);
        view.Observations[_player1Start].LastSeenTurn.ShouldBe((ushort)0);
        view.Observations[_player1Start].RemainingMatter.ShouldBe(1000);

        // The adjacent hexagon is visible, the one behind partially visible.
        view.Observations[new Hexagon(-2, 0)].Visibility.ShouldBe(Visibility.Visible);
        view.Observations[new Hexagon(-1, 0)].Visibility.ShouldBe(Visibility.PartiallyVisible);
        view.Observations[new Hexagon(-1, 0)].RemainingMatter.ShouldBe(1000);

        // The opponent start hexagon is out of sight: no matter, no buildings, no units.
        var enemyStart = view.Observations[_player2Start];
        enemyStart.Visibility.ShouldBe(Visibility.Mapped);
        enemyStart.LastSeenTurn.ShouldBeNull();
        enemyStart.RemainingMatter.ShouldBeNull();
        enemyStart.OpponentCompounds.ShouldBeEmpty();
        enemyStart.OpponentUnitCounts.ShouldBeEmpty();

        // No opponent information leaks anywhere on the map.
        view.Observations.Values.ShouldAllBe(x => x.OpponentCompounds.IsEmpty && x.OpponentUnitCounts.IsEmpty);
    }

    [Test]
    public void PartialVisibility_RevealsBuildingsAndMatterButNoUnits()
    {
        var gameSettings = WithPlayer2At(new Hexagon(-1, 0));
        var gameState = new GameState(gameSettings);
        var view = gameState.GetPlayerGameState("Player01");

        var observation = view.Observations[new Hexagon(-1, 0)];
        observation.Visibility.ShouldBe(Visibility.PartiallyVisible);
        observation.RemainingMatter.ShouldBe(1000);
        observation.OpponentCompounds.ShouldHaveSingleItem();
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });
        observation.OpponentUnitCounts.ShouldBeEmpty();
    }

    [Test]
    public void FullVisibility_RevealsUnitCount()
    {
        var gameSettings = WithPlayer2At(new Hexagon(-2, 0));
        var gameState = new GameState(gameSettings);
        var view = gameState.GetPlayerGameState("Player01");

        var observation = view.Observations[new Hexagon(-2, 0)];
        observation.Visibility.ShouldBe(Visibility.Visible);
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });
        observation.OpponentUnitCounts.ShouldHaveSingleItem();
        observation.OpponentUnitCounts["Player02"].ShouldBe(5);
    }

    [Test]
    public void Memory_UpdatesWhileVisible()
    {
        var gameState = new GameState(_gameSettings);
        var turn1 = GetNextTurnAndValidate(gameState, _noCommands);
        var view = turn1.GetPlayerGameState("Player01");

        // 5 dots harvested 50 matter on the own hexagon.
        view.Observations[_player1Start].RemainingMatter.ShouldBe(1000 - 50);
        view.Observations[_player1Start].LastSeenTurn.ShouldBe((ushort)1);
    }

    [Test]
    public void Scouting_MemoryPersistsAndGetsStaleAfterLosingVision()
    {
        var gameState = new GameState(_gameSettings);
        var scout = new Army() { Dot = 1 };

        // Move a single dot from (-3,0) towards the opponent, one hexagon per turn.
        var turn1 = GetNextTurnAndValidate(gameState, Move("Player01", new Hexagon(-3, 0), new Hexagon(-2, 0), scout));
        var turn2 = GetNextTurnAndValidate(turn1, Move("Player01", new Hexagon(-2, 0), new Hexagon(-1, 0), scout));
        var turn3 = GetNextTurnAndValidate(turn2, Move("Player01", new Hexagon(-1, 0), new Hexagon(0, 0), scout));
        var turn4 = GetNextTurnAndValidate(turn3, Move("Player01", new Hexagon(0, 0), new Hexagon(1, 0), scout));

        // From (1,0) the opponent start hexagon is partially visible: buildings and matter, but no units.
        var view = turn4.GetPlayerGameState("Player01");
        var observation = view.Observations[_player2Start];
        observation.Visibility.ShouldBe(Visibility.PartiallyVisible);
        observation.LastSeenTurn.ShouldBe((ushort)4);
        observation.RemainingMatter.ShouldBe(1000 - 4 * 50); // 5 dots harvested 50 matter per turn
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });
        observation.OpponentUnitCounts.ShouldBeEmpty();

        // Retreat: the opponent start hexagon is out of sight again, the memory keeps the stale snapshot.
        var turn5 = GetNextTurnAndValidate(turn4, Move("Player01", new Hexagon(1, 0), new Hexagon(0, 0), scout));
        view = turn5.GetPlayerGameState("Player01");
        observation = view.Observations[_player2Start];
        observation.Visibility.ShouldBe(Visibility.Mapped);
        observation.LastSeenTurn.ShouldBe((ushort)4);
        observation.RemainingMatter.ShouldBe(800); // stale: the actual value is 750
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });

        // The opponent constructs an axis while unseen; the memory must not change.
        var constructAxis = DictionaryExtensions.CommandsToDictionary([
            ("Player02", _player2Start, new Command() { Construction = new Compound() { Axis = 1 } })
        ]);
        var turn6 = GetNextTurnAndValidate(turn5, constructAxis);
        var turn7 = GetNextTurnAndValidate(turn6, _noCommands); // axis completes
        turn7.PlayerStates["Player02"].Compounds[_player2Start].ShouldBe(new Compound() { Plane = 1, Axis = 1 });
        view = turn7.GetPlayerGameState("Player01");
        observation = view.Observations[_player2Start];
        observation.LastSeenTurn.ShouldBe((ushort)4);
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1 });

        // Scout again: the memory updates to the new buildings.
        var turn8 = GetNextTurnAndValidate(turn7, Move("Player01", new Hexagon(0, 0), new Hexagon(1, 0), scout));
        view = turn8.GetPlayerGameState("Player01");
        observation = view.Observations[_player2Start];
        observation.Visibility.ShouldBe(Visibility.PartiallyVisible);
        observation.LastSeenTurn.ShouldBe((ushort)8);
        observation.OpponentCompounds["Player02"].ShouldBe(new Compound() { Plane = 1, Axis = 1 });
        observation.OpponentUnitCounts.ShouldBeEmpty();

        // Move next to the opponent: full visibility reveals the unit count.
        var turn9 = GetNextTurnAndValidate(turn8, Move("Player01", new Hexagon(1, 0), new Hexagon(2, 0), scout));
        view = turn9.GetPlayerGameState("Player01");
        observation = view.Observations[_player2Start];
        observation.Visibility.ShouldBe(Visibility.Visible);
        observation.OpponentUnitCounts["Player02"].ShouldBe(5);
    }

    [Test]
    public void GetPlayerGameState_ThrowsForUnknownPlayer()
    {
        var gameState = new GameState(_gameSettings);

        Should.Throw<ArgumentException>(() => gameState.GetPlayerGameState("UnknownPlayer"));
    }
}
