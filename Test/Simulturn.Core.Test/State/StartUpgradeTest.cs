using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The upgrades a player starts the game with (<see cref="GameSettings.StartUpgrades"/>) and the
/// start matter upgrade, whose effect is applied to the start state only.
/// </summary>
public sealed class StartUpgradeTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. Player01 can research the start matter upgrade at home. The start
    /// hexagons hold no matter, so the players' matter never changes through income.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_start, Maps.Start("Player01", matter: 0).WithResearch(Upgrade.StartMatter))
        .SetItem(_opponentStart, Maps.Start("Player02", matter: 0)));

    private static GameSettings WithStartUpgrades(string playerId, params Upgrade[] upgrades)
    {
        return _gameSettings with
        {
            StartUpgrades = new Dictionary<string, ImmutableArray<Upgrade>>() { [playerId] = [.. upgrades] }.ToFrozenDictionary()
        };
    }

    [Test]
    public void StartUpgrades_GiveThePlayerOneLevelPerEntry()
    {
        var state = new GameState(WithStartUpgrades("Player01", Upgrade.TriangleExponent, Upgrade.CircleExponent, Upgrade.TriangleExponent));

        state.PlayerStates["Player01"].UpgradeLevels.ShouldBe(
            new Dictionary<Upgrade, byte>() { [Upgrade.TriangleExponent] = 2, [Upgrade.CircleExponent] = 1 }, ignoreOrder: true);
        state.PlayerStates["Player02"].UpgradeLevels.ShouldBeEmpty();
        StateValidator.Validate(state).ShouldBeEmpty();
    }

    [Test]
    public void StartMatterUpgrade_AsStartUpgrade_RaisesTheStartMatterOfThatPlayerOnly()
    {
        var state = new GameState(WithStartUpgrades("Player01", Upgrade.StartMatter));

        state.PlayerStates["Player01"].Matter.ShouldBe(TestSettings.DefaultStartMatter + 250);
        state.PlayerStates["Player02"].Matter.ShouldBe(TestSettings.DefaultStartMatter);
        StateValidator.Validate(state).ShouldBeEmpty();
    }

    [Test]
    public void StartMatterUpgrade_ResearchedDuringTheGame_CostsItsLevelAndGivesNoMatter()
    {
        // Arrange
        var state = new GameState(_gameSettings);
        var research = DictionaryExtensions.CommandsToDictionary([("Player01", _start, new Command() { Research = Upgrade.StartMatter })]);

        // Act
        var completed = state.PlayTurn(research).PlayUntilResearchesComplete("Player01");

        // Assert
        completed.PlayerStates["Player01"].UpgradeLevels[Upgrade.StartMatter].ShouldBe((byte)1);
        completed.PlayerStates["Player01"].Matter.ShouldBe(TestSettings.DefaultStartMatter - 100);
    }

    [Test]
    public void StartMatterUpgrade_ActsOnNoUnitAndStaysOutOfAFight()
    {
        var levels = ImmutableDictionary<Upgrade, byte>.Empty.Add(Upgrade.StartMatter, 1);
        Army army = new Army() { Triangle = 3, Circle = 3, Square = 3, Dot = 3 };

        UpgradeRules.ActingOn(_gameSettings, levels, army).ShouldBeEmpty();
        Combat.GetExponent(_gameSettings, levels).ShouldBe(_gameSettings.FightExponent);
    }
}
