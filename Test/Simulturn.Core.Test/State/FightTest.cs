using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Frozen;
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
    /// Only the start hexagons hold matter; the center offers the triangle upgrade.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Disc(1)
        .SetItem(_player1Start, Maps.Start("Player01", matter: 10000))
        .SetItem(_player2Start, Maps.Start("Player02", matter: 10000))
        .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.TriangleExponent)));

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
    public void Fight_DotsAgainstFightingUnitsOfTheSamePrice_LoseWithTheStandardDotStrength()
    {
        // 8 dots cost as much as 3 triangles. A dot counts a quarter: 3^1.2 = 3.74 against 2.
        FightReport fight = Combat.Fight(_gameSettings, Combatants(
            ("Player01", new Army() { Triangle = 3 }),
            ("Player02", new Army() { Dot = 8 })));

        fight.Losses["Player01"].ShouldBe(new Army() { Triangle = 2 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 8 });
    }

    [Test]
    public void Fight_DotStrengthOfTheSettings_DecidesWhatADotCounts()
    {
        // With a full strength per dot the same dots win: 8 against 3^1.2 = 3.74.
        GameSettings settings = _gameSettings with { DotFightStrength = 100 };

        FightReport fight = Combat.Fight(settings, Combatants(
            ("Player01", new Army() { Triangle = 3 }),
            ("Player02", new Army() { Dot = 8 })));

        fight.Losses["Player01"].ShouldBe(new Army() { Triangle = 3 });
        fight.Losses["Player02"].ShouldBe(new Army() { Dot = 4 });
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
    public void GetExponent_AddsTheBonusOfTheLevelThePlayerHas()
    {
        Combat.GetExponent(_gameSettings, ImmutableDictionary<Upgrade, byte>.Empty)
            .ShouldBe(_gameSettings.FightExponent);

        Combat.GetExponent(_gameSettings, Levels((Upgrade.TriangleExponent, 1)))
            .ShouldBe(_gameSettings.FightExponent + new Army() { Triangle = 10 });

        // The bonus of a level is absolute: level 2 gives 20, not 10 + 20.
        Combat.GetExponent(_gameSettings, Levels((Upgrade.TriangleExponent, 2)))
            .ShouldBe(_gameSettings.FightExponent + new Army() { Triangle = 20 });

        Combat.GetExponent(_gameSettings, Levels((Upgrade.TriangleExponent, 2), (Upgrade.CircleExponent, 1), (Upgrade.SquareExponent, 0)))
            .ShouldBe(_gameSettings.FightExponent + new Army() { Triangle = 20, Circle = 10 });
    }

    [Test]
    [Arguments(Upgrade.TriangleExponent, Unit.Triangle)]
    [Arguments(Upgrade.CircleExponent, Unit.Circle)]
    [Arguments(Upgrade.SquareExponent, Unit.Square)]
    public void Fight_ExponentUpgrade_DecidesTheFightOfTwoEqualArmies(Upgrade upgrade, Unit unit)
    {
        // Arrange: ten units against ten units of the same type destroy each other without an upgrade.
        Army army = Army.Empty.AddUnit(unit, 10);
        Combat.Fight(_gameSettings, Combatants(("Player01", army), ("Player02", army))).Losses["Player01"].ShouldBe(army);

        // Act
        FightReport levelOne = Combat.Fight(_gameSettings, new Dictionary<string, Combatant>()
        {
            ["Player01"] = new Combatant() { Army = army, UpgradeLevels = Levels((upgrade, 1)) },
            ["Player02"] = new Combatant() { Army = army }
        });
        FightReport levelTwo = Combat.Fight(_gameSettings, new Dictionary<string, Combatant>()
        {
            ["Player01"] = new Combatant() { Army = army, UpgradeLevels = Levels((upgrade, 2)) },
            ["Player02"] = new Combatant() { Army = army }
        });

        // Assert: 10^1.2 / 10^1.3 = 0.79 and 10^1.2 / 10^1.4 = 0.63 of the upgraded army die, rounded up.
        levelOne.Losses["Player01"].ShouldBe(Army.Empty.AddUnit(unit, 8));
        levelOne.Losses["Player02"].ShouldBe(army);
        levelTwo.Losses["Player01"].ShouldBe(Army.Empty.AddUnit(unit, 7));
        levelTwo.Losses["Player02"].ShouldBe(army);
    }

    [Test]
    public void Fight_ReportsTheUpgradesThatActOnTheArmies()
    {
        // Arrange: Player01 has the triangle and the circle upgrade but brings no circles.
        var combatants = new Dictionary<string, Combatant>()
        {
            ["Player01"] = new Combatant()
            {
                Army = new Army() { Triangle = 4, Dot = 2 },
                UpgradeLevels = Levels((Upgrade.TriangleExponent, 2), (Upgrade.CircleExponent, 1))
            },
            ["Player02"] = new Combatant() { Army = new Army() { Square = 3 }, UpgradeLevels = Levels((Upgrade.SquareExponent, 0)) }
        };

        // Act
        FightReport fight = Combat.Fight(_gameSettings, combatants);

        // Assert
        fight.UpgradeLevels["Player01"].ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Upgrade, byte>(Upgrade.TriangleExponent, 2));
        fight.UpgradeLevels["Player02"].ShouldBeEmpty();
    }

    [Test]
    public void Fight_FromTheArmiesAndUpgradesOfAReport_GivesTheLossesOfTheReport()
    {
        // Arrange
        var combatants = new Dictionary<string, Combatant>()
        {
            ["Player01"] = new Combatant()
            {
                Army = new Army() { Triangle = 6, Circle = 3 },
                UpgradeLevels = Levels((Upgrade.TriangleExponent, 2), (Upgrade.SquareExponent, 2))
            },
            ["Player02"] = new Combatant() { Army = new Army() { Circle = 7, Dot = 4 }, UpgradeLevels = Levels((Upgrade.CircleExponent, 1)) }
        };
        FightReport fight = Combat.Fight(_gameSettings, combatants);

        // Act
        FightReport recomputed = Combat.Fight(_gameSettings, fight.Armies.ToDictionary(
            x => x.Key,
            x => new Combatant() { Army = x.Value, UpgradeLevels = fight.UpgradeLevels[x.Key] }));

        // Assert
        recomputed.Losses["Player01"].ShouldBe(fight.Losses["Player01"]);
        recomputed.Losses["Player02"].ShouldBe(fight.Losses["Player02"]);
    }

    [Test]
    public void Fight_UpgradeWithoutItsUnitsInTheArmy_ChangesNothing()
    {
        // Every upgrade of the rules names the unit types it acts on. On an army without these
        // types it must not change a fight, whatever its effect is.
        Army[] armies =
        [
            new Army() { Triangle = 7, Circle = 5, Square = 6, Dot = 4 },
            new Army() { Triangle = 12, Circle = 1, Square = 3, Dot = 9 },
            new Army() { Triangle = 2, Circle = 9, Square = 9, Dot = 1 }
        ];
        Army opponent = new Army() { Triangle = 5, Circle = 6, Square = 7, Dot = 3 };

        foreach ((Upgrade upgrade, var definition) in _gameSettings.Upgrades)
        {
            for (byte level = 1; level <= definition.Levels.Length; level++)
            {
                foreach (Army army in armies)
                {
                    Army unaffected = army;
                    foreach (Unit unit in definition.AffectedUnits)
                    {
                        unaffected = unaffected.AddUnit(unit, (short)-army[unit]);
                    }

                    FightReport without = Combat.Fight(_gameSettings, Combatants(("Player01", unaffected), ("Player02", opponent)));
                    FightReport with = Combat.Fight(_gameSettings, new Dictionary<string, Combatant>()
                    {
                        ["Player01"] = new Combatant() { Army = unaffected, UpgradeLevels = Levels((upgrade, level)) },
                        ["Player02"] = new Combatant() { Army = opponent }
                    });

                    with.Losses["Player01"].ShouldBe(without.Losses["Player01"], $"{upgrade} level {level}, {unaffected}");
                    with.Losses["Player02"].ShouldBe(without.Losses["Player02"], $"{upgrade} level {level}, {unaffected}");
                    with.UpgradeLevels["Player01"].ShouldBeEmpty($"{upgrade} level {level}, {unaffected}");
                }
            }
        }
    }

    [Test]
    public void Fight_InATurn_UsesTheUpgradesAndShowsThemToTheOpponent()
    {
        // Arrange: both players bring ten triangles to the center, Player01 starts with the triangle upgrade.
        Army triangles = new Army() { Triangle = 10 };
        var gameSettings = WithStarts((triangles, new Compound() { Plane = 1 }), (triangles, new Compound() { Plane = 1 })) with
        {
            StartUpgrades = new Dictionary<string, ImmutableArray<Upgrade>>() { ["Player01"] = [Upgrade.TriangleExponent] }.ToFrozenDictionary()
        };
        var gameState = new GameState(gameSettings);
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, triangles),
            ("Player02", _player2Start, _center, triangles)
        ]);

        // Act
        TurnResult result = gameState.ResolveTurn(commands, validateCommands: false);
        PlayerGameState opponentView = FogOfWar.GetPlayerGameState(result, "Player02");

        // Assert
        result.State.PlayerStates["Player01"].Armies[_center].ShouldBe(new Army() { Triangle = 2 });
        result.State.PlayerStates["Player02"].Armies.ShouldNotContainKey(_center);
        FightReport seen = opponentView.Report.Fights[_center];
        seen.UpgradeLevels["Player01"].ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Upgrade, byte>(Upgrade.TriangleExponent, 1));
        seen.UpgradeLevels["Player02"].ShouldBeEmpty();
    }

    private static ImmutableDictionary<Upgrade, byte> Levels(params (Upgrade Upgrade, byte Level)[] levels)
    {
        return levels.ToImmutableDictionary(x => x.Upgrade, x => x.Level);
    }
}
