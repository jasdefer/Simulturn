using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

/// <summary>
/// A player never occupies more levels of an upgrade, completed plus pending plus requested this turn,
/// than <see cref="GameSettings.Upgrades"/> defines. A hexagon either offers an upgrade or it does not;
/// the number of levels is defined once in the table (work item research-level-validation).
/// </summary>
public class ResearchLevelTest
{
    private static readonly Hexagon _start = new(-1, 0);
    private static readonly Hexagon _center = new(0, 0);
    private static readonly Hexagon _opponentStart = new(1, 0);
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A line of three hexagons. The dot upgrade (two levels in <see cref="TestSettings"/>) is researchable
    /// on Player01's start and on the center, not on Player02's start. The start hexagons hold no matter,
    /// so the players' matter never changes through income.
    /// </summary>
    private static readonly ImmutableDictionary<Hexagon, HexagonSettings> _map = Maps.Line(-1, 1)
        .SetItem(_start, Maps.Start("Player01", matter: 0).WithResearch(Upgrade.DotUpgrade))
        .SetItem(_center, Maps.Hexagon().WithResearch(Upgrade.DotUpgrade))
        .SetItem(_opponentStart, Maps.Start("Player02", matter: 0));

    private static readonly GameSettings _richSettings = TestSettings.Create(_map, startMatter: 5000);

    private static Dictionary<string, Dictionary<Hexagon, Command>> Research(string playerId, params Hexagon[] hexagons)
    {
        return hexagons
            .Select(hexagon => (playerId, hexagon, new Command() { Upgrade = Upgrade.DotUpgrade }))
            .CommandsToDictionary();
    }

    private static Dictionary<string, Dictionary<Hexagon, Command>> MoveOneDotToCenter()
    {
        return DictionaryExtensions.MovementsToDictionary([("Player01", _start, _center, new Army() { Dot = 1 })]);
    }

    [Test]
    public void SecondResearchWhileFirstIsPending_IsAccepted()
    {
        // Arrange: one pending research occupies level 1 of 2.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start));

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start)["Player01"]).ToList();

        // Assert
        validations.ShouldBeEmpty();
    }

    [Test]
    public void ResearchWhilePendingResearchesOccupyAllLevels_YieldsUpgradeExceedsAvailableLevel()
    {
        // Arrange: two pending researches occupy both levels; the first completes at the end of turn 2.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start))
            .PlayTurn(Research("Player01", _start));
        state.PlayerStates["Player01"].UpgradeLevels.ShouldBeEmpty();
        state.PlayerStates["Player01"].PendingResearchLevels()[Upgrade.DotUpgrade].ShouldBe((byte)2);

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start)["Player01"]).ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeExceedsAvailableLevel>();
        validation.Hexagon.ShouldBe(_start);
        validation.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        validation.CompletedLevels.ShouldBe((byte)0);
        validation.PendingLevels.ShouldBe((byte)2);
        validation.RequestedLevels.ShouldBe((byte)1);
        validation.MaxLevel.ShouldBe(2);
    }

    [Test]
    public void ResearchAfterLastLevelIsCompleted_YieldsUpgradeExceedsAvailableLevel()
    {
        // Arrange: both levels researched and completed.
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start))
            .PlayTurn(Research("Player01", _start));
        state = PlayUntilResearchesComplete(state);
        state.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)2);

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start)["Player01"]).ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeExceedsAvailableLevel>();
        validation.CompletedLevels.ShouldBe((byte)2);
        validation.PendingLevels.ShouldBe((byte)0);
        validation.RequestedLevels.ShouldBe((byte)1);
        validation.MaxLevel.ShouldBe(2);
    }

    [Test]
    public void ResearchOnTwoHexagonsWithinTheTable_IsAccepted()
    {
        // Arrange: a dot on the center, so both research hexagons are usable.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveOneDotToCenter());

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start, _center)["Player01"]).ToList();

        // Assert
        validations.ShouldBeEmpty();
    }

    [Test]
    public void ResearchOnTwoHexagonsBeyondTheTable_YieldsAValidationForEveryCommandInAnyOrder()
    {
        // Arrange: one pending research, two more requested, two levels defined.
        var state = new GameState(_richSettings)
            .PlayTurn(MoveOneDotToCenter())
            .PlayTurn(Research("Player01", _start));

        // Act: the same two commands in both enumeration orders.
        var startFirst = Research("Player01", _start, _center)["Player01"];
        var centerFirst = Research("Player01", _center, _start)["Player01"];
        var validationsStartFirst = state.Validate("Player01", startFirst).ToList();
        var validationsCenterFirst = state.Validate("Player01", centerFirst).ToList();

        // Assert: both commands are reported, and the order does not change the result.
        validationsStartFirst.Count.ShouldBe(2);
        validationsStartFirst.ShouldAllBe(x => x is UpgradeExceedsAvailableLevel);
        validationsStartFirst.Cast<UpgradeExceedsAvailableLevel>().Select(x => x.Hexagon).ShouldBe([_start, _center], ignoreOrder: true);
        validationsStartFirst.Cast<UpgradeExceedsAvailableLevel>().ShouldAllBe(x =>
            x.CompletedLevels == 0 && x.PendingLevels == 1 && x.RequestedLevels == 2 && x.MaxLevel == 2);
        validationsCenterFirst.ShouldBe(validationsStartFirst, ignoreOrder: true);
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
    public void ResearchWhileFirstLevelIsPending_CostsTheSecondLevel()
    {
        // Arrange: exactly enough matter for level 1 (150), not for level 2 (175). NextTurn does not
        // deduct research cost yet (research-cost-and-duration), so the matter stays at 150.
        var state = new GameState(TestSettings.Create(_map, startMatter: 150))
            .PlayTurn(Research("Player01", _start));
        state.PlayerStates["Player01"].Matter.ShouldBe(150);

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start)["Player01"]).ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<InsufficientMatter>();
        validation.Required.ShouldBe(175);
        validation.Available.ShouldBe(150);
    }

    [Test]
    public void ResearchOnTwoHexagonsInOneTurn_CostsBothLevels()
    {
        // Arrange: enough matter for one level, not for both (150 + 175).
        var state = new GameState(TestSettings.Create(_map, startMatter: 300))
            .PlayTurn(MoveOneDotToCenter());

        // Act
        var validations = state.Validate("Player01", Research("Player01", _start, _center)["Player01"]).ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<InsufficientMatter>();
        validation.Required.ShouldBe(325);
        validation.Available.ShouldBe(300);
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
    public void IsValid_PendingResearchOnHexagonWithoutTheUpgrade_YieldsUpgradeNotResearchableOnHexagon()
    {
        // Arrange: Player02 holds dots on its start, which offers no upgrade.
        var state = new GameState(_richSettings);
        var player = state.PlayerStates["Player02"] with
        {
            Researches = ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>>.Empty
                .Add(2, ImmutableDictionary<Hexagon, Upgrade>.Empty.Add(_opponentStart, Upgrade.DotUpgrade))
        };
        state = state with { PlayerStates = state.PlayerStates.SetItem("Player02", player) };

        // Act
        var validations = state.IsValid().ToList();

        // Assert
        var validation = validations.ShouldHaveSingleItem().ShouldBeOfType<UpgradeNotResearchableOnHexagon>();
        validation.PlayerId.ShouldBe("Player02");
        validation.Hexagon.ShouldBe(_opponentStart);
    }

    /// <summary>
    /// Scenario: both levels are started in consecutive turns, complete, and the fully researched
    /// upgrade is applied in a fight without indexing past the table.
    /// </summary>
    [Test]
    public void StackedResearchesCompleteBothLevels_AndTheFightAppliesTheLastLevel()
    {
        // Arrange
        var state = new GameState(_richSettings)
            .PlayTurn(Research("Player01", _start))
            .PlayTurn(Research("Player01", _start));
        state = PlayUntilResearchesComplete(state);
        state.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)2);
        state.PlayerStates["Player01"].ExponentBonusFromUpgrades(_richSettings).ShouldBe(new Army() { Dot = 20 });

        // Act: both players move their dots to the center and fight there.
        var fight = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _start, _center, new Army() { Dot = 5 }),
            ("Player02", _opponentStart, _center, new Army() { Dot = 5 })
        ]);
        var afterFight = Should.NotThrow(() => state.PlayTurn(fight));

        // Assert
        afterFight.PlayerStates["Player01"].Losses.Keys.ShouldContain(_center);
        afterFight.PlayerStates["Player02"].Losses.Keys.ShouldContain(_center);
        afterFight.PlayerStates["Player01"].UpgradeLevels[Upgrade.DotUpgrade].ShouldBe((byte)2);
    }

    /// <summary>
    /// Plays empty turns until no research is pending. The duration of a stacked research is the
    /// subject of research-cost-and-duration, so the scenario does not pin the exact turn.
    /// </summary>
    private static GameState PlayUntilResearchesComplete(GameState state)
    {
        for (int turn = 0; turn < 10 && !state.PlayerStates["Player01"].Researches.IsEmpty; turn++)
        {
            state = state.PlayTurn(_noCommands);
        }
        state.PlayerStates["Player01"].Researches.ShouldBeEmpty();
        return state;
    }
}
