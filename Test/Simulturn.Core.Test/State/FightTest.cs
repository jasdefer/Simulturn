using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The fight rule: <see cref="Combat.Fight"/> on its own and as the fight phase of the turn resolution.
/// </summary>
public sealed class FightTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Hexagon _player3Start = new Hexagon(0, 1);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A disc of 7 hexagons. The players start on opposite sides of the center with 5 dots and a plane each.
    /// Only the start hexagons hold matter; the center offers the dot upgrade.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Disc(1)
        .SetItem(_player1Start, Maps.Start("Player01", matter: 10000))
        .SetItem(_player2Start, Maps.Start("Player02", matter: 10000))
        .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.DotUpgrade)));

    /// <summary>
    /// The standard map with different start armies and compounds.
    /// </summary>
    private static GameSettings WithStarts((Army Army, Compound Compound) player1, (Army Army, Compound Compound) player2)
    {
        return TestSettings.Create(_gameSettings.HexagonSettings.ToImmutableDictionary()
            .SetItem(_player1Start, Maps.Start("Player01", matter: 10000, army: player1.Army, compound: player1.Compound))
            .SetItem(_player2Start, Maps.Start("Player02", matter: 10000, army: player2.Army, compound: player2.Compound)));
    }

    private static Dictionary<string, Combatant> Combatants(params (string PlayerId, Army Army)[] armies)
    {
        return armies.ToDictionary(x => x.PlayerId, x => new Combatant() { Army = x.Army });
    }

    [Test]
    public void Fight_Dot_vs_Dot()
    {
        // Assign
        var gameState = new GameState(_gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Dot = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Dot = 3 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands);
        var turn1 = result1.State;

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Dot = 2 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));

        // Fighting reveals the pre-fight composition of the participating armies to each other.
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .Fights[new Hexagon(0, 0)].Armies["Player02"].ShouldBe(new Army() { Dot = 3 });
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .Fights[new Hexagon(0, 0)].Armies["Player01"].ShouldBe(new Army() { Dot = 5 });

        // The revelation only lasts for the turn of the fight.
        var result2 = turn1.ResolveTurn(_noCommands);
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player01").Fights.ShouldBeEmpty();
    }

    [Test]
    public void Fight_Circle_vs_Circle()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 5, Dot = 5 }, new Compound() { Plane = 1 }), (new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Circle = 3 })
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 2 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_Circle_vs_Square()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 3, Dot = 5 }, new Compound() { Plane = 1 }), (new Army() { Square = 5, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 3 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var turn1 = gameState.PlayTurn(commands);

        // Assert
        turn1.PlayerStates["Player01"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Circle = 1 });
        turn1.PlayerStates["Player02"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_Army_vs_Army()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }, new Compound() { Plane = 1 }), (new Army() { Square = 15, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 15, Dot = 5 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands, validateCommands: false);
        var turn1 = result1.State;

        // Assert
        turn1.PlayerStates["Player02"]
            .Armies[new Hexagon(0, 0)]
            .ShouldBe(new Army() { Square = 1 });
        turn1.PlayerStates["Player01"]
            .Armies.ShouldNotContainKey(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .Fights[new Hexagon(0, 0)].Losses["Player01"]
            .ShouldBe(new Army() { Circle = 5, Dot = 5, Square = 5, Triangle = 5 });
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .Fights[new Hexagon(0, 0)].Losses["Player02"]
            .ShouldBe(new Army() { Dot = 5, Square = 14 });
    }

    [Test]
    public void Fight_Army_vs_Army_InTwoTurns()
    {
        // Assign
        var gameSettings = WithStarts((new Army() { Square = 15 }, new Compound() { Plane = 1 }), (new Army() { Square = 15 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", new Hexagon(-1,0), new Hexagon(0,0), new Army() { Square = 15 }),
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result1 = gameState.ResolveTurn(commands, validateCommands: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result2 = result1.State.ResolveTurn(commands, validateCommands: false);

        commands = DictionaryExtensions.MovementsToDictionary([
            ("Player02", new Hexagon(1,0), new Hexagon(0,0), new Army() { Square = 5 })
        ]);

        // Act
        var result3 = result2.State.ResolveTurn(commands, validateCommands: false);

        // Assert
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player02")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result1.Report, "Player01")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player02")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result2.Report, "Player01")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result3.Report, "Player02")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
        FogOfWar.GetPlayerTurnReport(result3.Report, "Player01")
            .Fights.Keys.ShouldContain(new Hexagon(0, 0));
    }

    [Test]
    public void Fight_EqualStrength_DestroysBothArmies()
    {
        // Act
        FightReport fight = Combat.Fight(_gameSettings, Combatants(
            ("Player01", new Army() { Dot = 4 }),
            ("Player02", new Army() { Dot = 4 })));

        // Assert
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 4 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 4 });
    }

    [Test]
    public void Fight_ThreeArmies_TheStrongestLosesTheSumOfItsFights()
    {
        // Act
        FightReport fight = Combat.Fight(_gameSettings, Combatants(
            ("Player01", new Army() { Dot = 20 }),
            ("Player02", new Army() { Dot = 6 }),
            ("Player03", new Army() { Dot = 6 })));

        // Assert: 6 dots lost against each of the two weaker armies.
        fight.Armies["Player01"].ShouldBe(new Army() { Dot = 20 });
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 12 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 6 });
        fight.Losses["Player03"].ShouldBe(new Army() { Dot = 6 });
    }

    [Test]
    public void Fight_ThreeArmies_LossesEndAtTheArmy()
    {
        // Act: the strongest army beats each opponent alone, but not both together.
        FightReport fight = Combat.Fight(_gameSettings, Combatants(
            ("Player01", new Army() { Dot = 10 }),
            ("Player02", new Army() { Dot = 6 }),
            ("Player03", new Army() { Dot = 6 })));

        // Assert: 6 + 6 lost dots end at the 10 the army has.
        fight.Losses["Player01"].ShouldBe(new Army() { Dot = 10 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 6 });
        fight.Losses["Player03"].ShouldBe(new Army() { Dot = 6 });
    }

    [Test]
    public void Fight_ThreeArmies_DoesNotDependOnTheOrderOfTheCombatants()
    {
        // Arrange
        (string, Army)[] armies = [
            ("Player01", new Army() { Triangle = 7, Dot = 2 }),
            ("Player02", new Army() { Square = 5, Circle = 1 }),
            ("Player03", new Army() { Circle = 6, Dot = 4 })
        ];

        // Act
        FightReport fight = Combat.Fight(_gameSettings, Combatants(armies));
        FightReport reversed = Combat.Fight(_gameSettings, Combatants([.. armies.Reverse()]));

        // Assert
        foreach (string playerId in fight.Armies.Keys)
        {
            reversed.Losses[playerId].ShouldBe(fight.Losses[playerId]);
        }
    }

    [Test]
    public void Fight_ThreeArmiesOnOneHexagon_LeavesNoArmyBelowZero()
    {
        // Arrange
        var gameSettings = TestSettings.Create(_gameSettings.HexagonSettings.ToImmutableDictionary()
            .SetItem(_player1Start, Maps.Start("Player01", matter: 10000, army: new Army() { Dot = 10 }))
            .SetItem(_player2Start, Maps.Start("Player02", matter: 10000, army: new Army() { Dot = 6 }))
            .SetItem(_player3Start, Maps.Start("Player03", matter: 10000, army: new Army() { Dot = 6 })));
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 10 }),
            ("Player02", _player2Start, _center, new Army() { Dot = 6 }),
            ("Player03", _player3Start, _center, new Army() { Dot = 6 })
        ]);

        // Act
        var result = gameState.ResolveTurn(commands);

        // Assert
        result.Report.Fights[_center].Losses["Player01"].ShouldBe(new Army() { Dot = 10 });
        foreach (PlayerState player in result.State.PlayerStates.Values)
        {
            player.Armies.ShouldNotContainKey(_center);
            player.UsedSpace.ShouldBe(0);
        }
    }

    [Test]
    public void Forecast_FromThePlayerView_MatchesTheResolvedFight()
    {
        // Arrange
        var gameSettings = WithStarts((new Army() { Circle = 5, Triangle = 2, Dot = 5 }, new Compound() { Plane = 1 }), (new Army() { Square = 4, Circle = 3, Dot = 5 }, new Compound() { Plane = 1 }));
        var gameState = new GameState(gameSettings);
        Army attackers = new Army() { Circle = 5, Triangle = 2 };
        Army defenders = new Army() { Square = 4, Circle = 3 };
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, attackers),
            ("Player02", _player2Start, _center, defenders)
        ]);

        // Act: Player01 forecasts with what the own view holds and an assumption about the opponent.
        PlayerGameState view = FogOfWar.GetPlayerGameState(gameState, "Player01");
        FightReport forecast = Combat.Fight(view.GameSettings, new Dictionary<string, Combatant>()
        {
            ["Player01"] = new Combatant() { Army = attackers, UpgradeLevels = view.PlayerState.UpgradeLevels },
            ["Player02"] = new Combatant() { Army = defenders }
        });
        var result = gameState.ResolveTurn(commands, validateCommands: false);

        // Assert
        FightReport fight = result.Report.Fights[_center];
        forecast.Armies["Player01"].ShouldBe(fight.Armies["Player01"]);
        forecast.Armies["Player02"].ShouldBe(fight.Armies["Player02"]);
        forecast.Losses["Player01"].ShouldBe(fight.Losses["Player01"]);
        forecast.Losses["Player02"].ShouldBe(fight.Losses["Player02"]);
    }

    [Test]
    public void GetExponent_AddsTheBonusOfTheResearchedLevels()
    {
        Combat.GetExponent(_gameSettings, ImmutableDictionary<Upgrade, byte>.Empty)
            .ShouldBe(_gameSettings.FightExponent);

        var levelOne = ImmutableDictionary<Upgrade, byte>.Empty.Add(Upgrade.DotUpgrade, 1);
        Combat.GetExponent(_gameSettings, levelOne)
            .ShouldBe(_gameSettings.FightExponent + new Army() { Dot = 20 });

        // The maximum level must use the last defined upgrade instead of indexing past the array.
        var maxLevel = ImmutableDictionary<Upgrade, byte>.Empty.Add(Upgrade.DotUpgrade, 2);
        Combat.GetExponent(_gameSettings, maxLevel)
            .ShouldBe(_gameSettings.FightExponent + new Army() { Dot = 20 });
    }
}
