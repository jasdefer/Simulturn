using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The research rules of <see cref="GameState.Validate"/> and <see cref="GameState.IsValid"/>
/// (work item research-cost-and-duration): one pending research per upgrade and per hexagon, the
/// next level only after the previous one completed, never beyond the table, never on a hexagon
/// without the upgrade or without a dot, and the last dot stays until the research completes.
/// </summary>
public class ResearchValidationTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);

    /// <summary>
    /// A line of three hexagons. The dot upgrade (two levels in <see cref="TestSettings"/>) is researchable
    /// on Player01's start and on the center, the start matter upgrade only on Player01's start. Player02's
    /// start offers nothing. The start hexagons hold no matter, so the players' matter never changes through income.
    /// </summary>
    private static readonly ImmutableDictionary<Hexagon, HexagonSettings> _map = Maps.Line(-1, 1)
        .SetItem(_start, Maps.Start("Player01", matter: 0).WithResearch(Upgrade.DotUpgrade, Upgrade.UpgradeStartMatter))
        .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.DotUpgrade))
        .SetItem(_opponentStart, Maps.Start("Player02", matter: 0));

    private static readonly GameSettings _richSettings = TestSettings.Create(_map, startMatter: 5000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Research(string playerId, params Hexagon[] hexagons)
    {
        return Research(playerId, Upgrade.DotUpgrade, hexagons);
    }

    private static Dictionary<string, Dictionary<Hexagon, Command>> Research(string playerId, Upgrade upgrade, params Hexagon[] hexagons)
    {
        return hexagons
            .Select(hexagon => (playerId, hexagon, new Command() { Upgrade = upgrade }))
            .CommandsToDictionary();
    }

    private static Dictionary<string, Dictionary<Hexagon, Command>> MoveDotsToCenter(short dots)
    {
        return DictionaryExtensions.MovementsToDictionary([("Player01", _start, _center, new Army() { Dot = dots })]);
    }

    private static List<ICommandValidation> Validate(GameState state, Dictionary<string, Dictionary<Hexagon, Command>> commands)
    {
        return state.Validate("Player01", commands["Player01"]).ToList();
    }

    [Test]
    public void ResearchOnAnOfferingHexagonWithADot_IsAccepted()
    {
        var state = new GameState(_richSettings);

        Validate(state, Research("Player01", _start)).ShouldBeEmpty();
    }

    [Test]
    public void ResearchAfterLastLevelIsCompleted_YieldsUpgradeExceedsAvailableLevel()
    {
        // Arrange: both levels researched one after the other and completed.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start))
            .PlayUntilResearchesComplete("Player01")
            .PlayTurn(Research("Player01", _start))
            .PlayUntilResearchesComplete("Player01");
        state.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)2);

        // Act
        var validations = Validate(state, Research("Player01", _start));

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeExceedsAvailableLevel>();
        validation.Hexagon.ShouldBe(_start);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        validation.CompletedLevels.ShouldBe((byte)2);
        validation.MaxLevel.ShouldBe(2);
    }

    [Test]
    public void SameUpgradeOnAnotherHexagonWhileItIsPending_YieldsUpgradeAlreadyInResearch()
    {
        // Arrange: a dot on the center, the dot upgrade pending at the start.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveDotsToCenter(1))
            .PlayTurn(Research("Player01", _start));

        // Act
        var validations = Validate(state, Research("Player01", _center));

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeAlreadyInResearch>();
        validation.Hexagon.ShouldBe(_center);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void SameUpgradeOnTheSameHexagonWhileItIsPending_YieldsUpgradeAlreadyInResearchAndHexagonAlreadyResearching()
    {
        // Arrange: the dot upgrade pending at the start.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));

        // Act
        var validations = Validate(state, Research("Player01", _start));

        // Assert
        validations.Count.ShouldBe(2);
        validations.ShouldContain(x => x is UpgradeAlreadyInResearch);
        validations.ShouldContain(x => x is HexagonAlreadyResearching);
    }

    [Test]
    public void SameUpgradeOnTwoHexagonsInOneTurn_YieldsUpgradeRequestedOnMultipleHexagonsForEveryCommandInAnyOrder()
    {
        // Arrange: a dot on the center, so both hexagons could research.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveDotsToCenter(1));

        // Act: the same two commands in both enumeration orders.
        var validationsStartFirst = Validate(state, Research("Player01", _start, _center));
        var validationsCenterFirst = Validate(state, Research("Player01", _center, _start));

        // Assert: both commands are reported, and the order does not change the result.
        validationsStartFirst.Count.ShouldBe(2);
        validationsStartFirst.ShouldAllBe(x => x is UpgradeRequestedOnMultipleHexagons);
        validationsStartFirst.Cast<UpgradeRequestedOnMultipleHexagons>().Select(x => x.Hexagon).ShouldBe([_start, _center], ignoreOrder: true);
        validationsStartFirst.Cast<UpgradeRequestedOnMultipleHexagons>().ShouldAllBe(x => x.Upgrade == Upgrade.DotUpgrade && x.RequestedCount == 2);
        validationsCenterFirst.ShouldBe(validationsStartFirst, ignoreOrder: true);
    }

    [Test]
    public void OtherUpgradeOnAHexagonWithAPendingResearch_YieldsHexagonAlreadyResearching()
    {
        // Arrange: the dot upgrade pending at the start.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));

        // Act
        var validations = Validate(state, Research("Player01", Upgrade.UpgradeStartMatter, _start));

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<HexagonAlreadyResearching>();
        validation.Hexagon.ShouldBe(_start);
        validation.Upgrade.ShouldBe(Upgrade.UpgradeStartMatter);
        validation.PendingUpgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void DifferentUpgradesOnDifferentHexagons_AreAccepted()
    {
        // Arrange: a dot on the center, the dot upgrade pending there.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveDotsToCenter(1))
            .PlayTurn(Research("Player01", _center));

        // Act
        var validations = Validate(state, Research("Player01", Upgrade.UpgradeStartMatter, _start));

        // Assert
        validations.ShouldBeEmpty();
    }

    [Test]
    public void ResearchOnHexagonWithoutTheUpgrade_YieldsUpgradeNotResearchableOnHexagon()
    {
        var state = new GameState(_richSettings);

        var validations = state.Validate("Player02", Research("Player02", _opponentStart)["Player02"]).ToList();

        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeNotResearchableOnHexagon>();
        validation.PlayerId.ShouldBe("Player02");
        validation.Hexagon.ShouldBe(_opponentStart);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void ResearchOnHexagonWithoutADot_YieldsUpgradeRequiresDot()
    {
        var state = new GameState(_richSettings);

        var validations = Validate(state, Research("Player01", _center));

        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeRequiresDot>();
        validation.Hexagon.ShouldBe(_center);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void ResearchWithoutEnoughMatter_YieldsInsufficientMatter()
    {
        var state = new GameState(TestSettings.Create(_map, startMatter: 149));

        var validations = Validate(state, Research("Player01", _start));

        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<InsufficientMatter>();
        validation.Required.ShouldBe(150);
        validation.Available.ShouldBe(149);
    }

    [Test]
    public void SecondLevel_CostsTheSecondLevel()
    {
        // Arrange: level 1 paid (150) and completed, 174 matter left, level 2 costs 175.
        var state = new GameState(TestSettings.Create(_map, startMatter: 324))
            .PlayTurn(Research("Player01", _start))
            .PlayUntilResearchesComplete("Player01");
        state.PlayerStates["Player01"].Matter.ShouldBe(174);

        // Act
        var validations = Validate(state, Research("Player01", _start));

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<InsufficientMatter>();
        validation.Required.ShouldBe(175);
        validation.Available.ShouldBe(174);
    }

    [Test]
    public void MovingTheLastDotOffAPendingResearch_YieldsMovementLeavesResearchWithoutDot()
    {
        // Arrange: the dot upgrade pending at the start, five dots there.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));

        // Act
        var validations = Validate(state, MoveDotsToCenter(5));

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<MovementLeavesResearchWithoutDot>();
        validation.Hexagon.ShouldBe(_start);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void MovingSomeDotsOffAPendingResearch_IsAccepted()
    {
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));

        Validate(state, MoveDotsToCenter(4)).ShouldBeEmpty();
    }

    [Test]
    public void MovingTheLastDotOffARequestedResearch_YieldsMovementLeavesResearchWithoutDot()
    {
        // Arrange: a research and a movement of all dots in the same command.
        var state = new GameState(_richSettings);
        var commands = MoveDotsToCenter(5);
        commands["Player01"][_start] = commands["Player01"][_start] with { Upgrade = Upgrade.DotUpgrade };

        // Act
        var validations = Validate(state, commands);

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<MovementLeavesResearchWithoutDot>();
        validation.Hexagon.ShouldBe(_start);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
    }

    [Test]
    public void SwappingTheDotOfAPendingResearch_IsAccepted()
    {
        // Arrange: one dot on the center with a pending research, four at the start.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveDotsToCenter(1))
            .PlayTurn(Research("Player01", _center));

        // Act: the dot leaves the center while another one arrives.
        var swap = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _center, _start, new Army() { Dot = 1 }),
            ("Player01", _start, _center, new Army() { Dot = 1 })
        ]);

        // Assert
        Validate(state, swap).ShouldBeEmpty();
    }

    [Test]
    public void IsValid_CompletedAndPendingLevelsBeyondTheTable_YieldsUpgradeLevelsExceedTable()
    {
        // Arrange: one pending research, then both levels marked completed behind the engine's back.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));
        var player = state.PlayerStates["Player01"] with
        {
            UpgradeLevels = ImmutableDictionary<Upgrade, byte>.Empty.Add(Upgrade.DotUpgrade, 2)
        };
        state = state with { PlayerStates = state.PlayerStates.SetItem("Player01", player) };

        // Act
        var validations = state.IsValid().ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeLevelsExceedTable>();
        validation.PlayerId.ShouldBe("Player01");
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        validation.CompletedLevels.ShouldBe((byte)2);
        validation.PendingLevels.ShouldBe((byte)1);
        validation.MaxLevel.ShouldBe(2);
    }

    [Test]
    public void IsValid_UpgradePendingOnTwoHexagons_YieldsUpgradeResearchedOnMultipleHexagons()
    {
        // Arrange: dots on both research hexagons, the same upgrade pending on both behind the engine's back.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveDotsToCenter(1));
        var player = state.PlayerStates["Player01"] with
        {
            Researches = ImmutableDictionary<Hexagon, Research>.Empty
                .Add(_start, new Research(Upgrade.DotUpgrade, 5))
                .Add(_center, new Research(Upgrade.DotUpgrade, 5))
        };
        state = state with { PlayerStates = state.PlayerStates.SetItem("Player01", player) };

        // Act
        var validations = state.IsValid().ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeResearchedOnMultipleHexagons>();
        validation.PlayerId.ShouldBe("Player01");
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        validation.PendingCount.ShouldBe((byte)2);
    }

    [Test]
    public void IsValid_PendingResearchOnHexagonWithoutTheUpgrade_YieldsUpgradeNotResearchableOnHexagon()
    {
        // Arrange: Player02 holds dots on its start, which offers no upgrade.
        var state = new GameState(_richSettings);
        var player = state.PlayerStates["Player02"] with
        {
            Researches = ImmutableDictionary<Hexagon, Research>.Empty.Add(_opponentStart, new Research(Upgrade.DotUpgrade, 2))
        };
        state = state with { PlayerStates = state.PlayerStates.SetItem("Player02", player) };

        // Act
        var validations = state.IsValid().ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeNotResearchableOnHexagon>();
        validation.PlayerId.ShouldBe("Player02");
        validation.Hexagon.ShouldBe(_opponentStart);
    }

    [Test]
    public void IsValid_PendingResearchWithoutADot_YieldsUpgradeRequiresDot()
    {
        // Arrange: the center offers the upgrade, but no dot stands there.
        var state = new GameState(_richSettings);
        var player = state.PlayerStates["Player01"] with
        {
            Researches = ImmutableDictionary<Hexagon, Research>.Empty.Add(_center, new Research(Upgrade.DotUpgrade, 2))
        };
        state = state with { PlayerStates = state.PlayerStates.SetItem("Player01", player) };

        // Act
        var validations = state.IsValid().ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeRequiresDot>();
        validation.PlayerId.ShouldBe("Player01");
        validation.Hexagon.ShouldBe(_center);
    }
}
