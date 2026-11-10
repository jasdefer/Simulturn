using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The research phase of <see cref="GameState.NextTurn"/> (work item research-cost-and-duration):
/// a research costs the matter of its level when it starts, completes after the duration of that
/// level, and is canceled without refund once its hexagon has no dot left at the end of a turn.
/// </summary>
public class ResearchResolutionTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. The dot upgrade (level 1: 150 matter, 3 turns; level 2: 175 matter, 4 turns)
    /// is researchable on Player01's start and on the center. No hexagon holds matter, so the players'
    /// matter only changes through research.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_start, Maps.Start("Player01", matter: 0).WithResearch(Upgrade.DotUpgrade))
            .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.DotUpgrade))
            .SetItem(_opponentStart, Maps.Start("Player02", matter: 0)),
        startMatter: 1000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Research(Hexagon hexagon)
    {
        return DictionaryExtensions.CommandsToDictionary([("Player01", hexagon, new Command() { Upgrade = Upgrade.DotUpgrade })]);
    }

    private static Dictionary<string, Dictionary<Hexagon, Command>> MoveOneDotToCenter()
    {
        return DictionaryExtensions.MovementsToDictionary([("Player01", _start, _center, new Army() { Dot = 1 })]);
    }

    /// <summary>
    /// Player02 attacks the center with all five dots. A single dot of Player01 there dies.
    /// </summary>
    private static Dictionary<string, Dictionary<Hexagon, Command>> AttackCenter()
    {
        return DictionaryExtensions.MovementsToDictionary([("Player02", _opponentStart, _center, new Army() { Dot = 5 })]);
    }

    [Test]
    public void Research_CostsTheLevelInTheTurnItStarts()
    {
        var state = new GameState(_gameSettings);

        var turn1 = state.PlayTurn(Research(_start));

        turn1.PlayerStates["Player01"].Matter.ShouldBe(850);
        turn1.PlayerStates["Player01"].Researches[_start].ShouldBe(new Research(Upgrade.DotUpgrade, 2));
        turn1.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
    }

    [Test]
    public void Research_CompletesAfterTheDurationOfItsLevel()
    {
        // Arrange: level 1 takes three turns (started in turn 0, due in turn 2), level 2 four turns.
        var state = new GameState(_gameSettings);

        // Act
        var turn3 = state.PlayTurn(Research(_start)).PlayEmptyTurn().PlayEmptyTurn();
        var turn7 = turn3.PlayTurn(Research(_start)).PlayEmptyTurn().PlayEmptyTurn().PlayEmptyTurn();

        // Assert
        turn3.Turn.ShouldBe((ushort)3);
        turn3.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        turn3.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)1);
        turn3.PlayerStates["Player01"].Matter.ShouldBe(850);
        turn7.Turn.ShouldBe((ushort)7);
        turn7.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        turn7.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)2);
        turn7.PlayerStates["Player01"].Matter.ShouldBe(675);
    }

    [Test]
    public void LastDotDiesInAFight_CancelsTheResearchWithoutRefund()
    {
        // Arrange: one dot researches on the center.
        var state = new GameState(_gameSettings)
            .PlayTurn(MoveOneDotToCenter())
            .PlayTurn(Research(_center));
        state.PlayerStates["Player01"].Researches.ShouldContainKey(_center);

        // Act
        var afterFight = state.PlayTurn(AttackCenter());

        // Assert
        afterFight.PlayerStates["Player01"].Armies.ShouldNotContainKey(_center);
        afterFight.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        afterFight.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        afterFight.PlayerStates["Player01"].Matter.ShouldBe(850);
    }

    [Test]
    public void ResearchDueInTheTurnTheLastDotDies_CompletesBeforeTheFight()
    {
        // Arrange: the research on the center is due in the turn of the attack.
        var state = new GameState(_gameSettings)
            .PlayTurn(MoveOneDotToCenter())
            .PlayTurn(Research(_center))
            .PlayEmptyTurn();
        state.PlayerStates["Player01"].Researches[_center].CompletionTurn.ShouldBe(state.Turn);

        // Act
        var afterFight = state.PlayTurn(AttackCenter());

        // Assert
        afterFight.PlayerStates["Player01"].Armies.ShouldNotContainKey(_center);
        afterFight.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        afterFight.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)1);
        afterFight.PlayerStates["Player01"].Matter.ShouldBe(850);
    }

    [Test]
    public void ResearchStartedInTheTurnTheLastDotDies_IsPaidAndCanceled()
    {
        // Arrange: one dot on the center.
        var state = new GameState(_gameSettings)
            .PlayTurn(MoveOneDotToCenter());

        // Act: the research starts in the turn of the attack.
        var commands = Research(_center);
        commands["Player02"] = AttackCenter()["Player02"];
        var afterFight = state.PlayTurn(commands);

        // Assert
        afterFight.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        afterFight.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        afterFight.PlayerStates["Player01"].Matter.ShouldBe(850);
    }

    [Test]
    public void DotArrivingInTheTurnTheLastDotLeaves_KeepsTheResearch()
    {
        // Arrange: one dot researches on the center, four dots wait at the start.
        var state = new GameState(_gameSettings)
            .PlayTurn(MoveOneDotToCenter())
            .PlayTurn(Research(_center));

        // Act: the researching dot goes home while another one takes its place.
        var swap = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _center, _start, new Army() { Dot = 1 }),
            ("Player01", _start, _center, new Army() { Dot = 1 })
        ]);
        var afterSwap = state.PlayTurn(swap);

        // Assert
        afterSwap.PlayerStates["Player01"].Armies[_center].ShouldBe(new Army() { Dot = 1 });
        afterSwap.PlayerStates["Player01"].Researches[_center].ShouldBe(new Research(Upgrade.DotUpgrade, 3));
        afterSwap.PlayerStates["Player01"].Matter.ShouldBe(850);
    }
}
