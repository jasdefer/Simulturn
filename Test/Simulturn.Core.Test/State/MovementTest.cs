using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// Every unit type moves a limited number of hexagons per turn (work item movement-range).
/// <see cref="Movement"/> holds the rule, <see cref="CommandValidator"/> and a client that offers
/// destinations both ask there.
/// </summary>
public sealed class MovementTest
{
    private static readonly Hexagon _start = new(0, 0);
    private static readonly Hexagon _opponentStart = new(3, 0);
    private static readonly Army _army = new() { Dot = 5, Circle = 2, Square = 4 };

    /// <summary>
    /// A disc of radius 3 with Player01 in the middle and Player02 on the edge. Dots and triangles
    /// move one hexagon, circles two and squares three.
    /// </summary>
    private static readonly GameSettings _gameSettings = DefaultRulesOnTheDisc() with { MovementRange = new Army() { Dot = 1, Triangle = 1, Circle = 2, Square = 3 } };

    private static GameSettings DefaultRulesOnTheDisc()
    {
        return TestSettings.Create(Maps.Disc(3)
            .SetItem(_start, Maps.Start("Player01", army: _army))
            .SetItem(_opponentStart, Maps.Start("Player02")));
    }

    private static Dictionary<Hexagon, Command> Move(Hexagon origin, Hexagon destination, Army army)
    {
        return new() { [origin] = new Command() { MovementCommands = [new MovementCommand() { Army = army, Destination = destination }] } };
    }

    [Test]
    public void ReachableArmy_KeepsTheUnitTypesThatGetAsFarAsTheDistance()
    {
        Movement.ReachableArmy(_gameSettings, _army, 0).ShouldBe(_army);
        Movement.ReachableArmy(_gameSettings, _army, 1).ShouldBe(_army);
        Movement.ReachableArmy(_gameSettings, _army, 2).ShouldBe(new Army() { Circle = 2, Square = 4 });
        Movement.ReachableArmy(_gameSettings, _army, 3).ShouldBe(new Army() { Square = 4 });
        Movement.ReachableArmy(_gameSettings, _army, 4).ShouldBe(Army.Empty);
    }

    [Test]
    public void Destinations_GiveThePartOfTheArmyThatReachesEveryHexagon()
    {
        var destinations = Movement.Destinations(_gameSettings, _start, _army);

        destinations.Count.ShouldBe(36); // the disc of radius 3 without the origin
        destinations.ShouldNotContainKey(_start);
        foreach (Hexagon hexagon in Hexagon.Ring(1))
        {
            destinations[hexagon].ShouldBe(_army);
        }
        foreach (Hexagon hexagon in Hexagon.Ring(2))
        {
            destinations[hexagon].ShouldBe(new Army() { Circle = 2, Square = 4 });
        }
        foreach (Hexagon hexagon in Hexagon.Ring(3))
        {
            destinations[hexagon].ShouldBe(new Army() { Square = 4 });
        }
    }

    [Test]
    public void Destinations_LeaveOutHexagonsNoUnitReaches()
    {
        var destinations = Movement.Destinations(_gameSettings, _start, new Army() { Dot = 5 });

        destinations.Keys.ShouldBe(Hexagon.Ring(1), ignoreOrder: true);
    }

    [Test]
    public void Destinations_EndAtTheEdgeOfTheMap()
    {
        var destinations = Movement.Destinations(_gameSettings, _opponentStart, new Army() { Dot = 5 });

        // Three of the six neighbors of a hexagon on the edge of the disc lie outside the map.
        destinations.Keys.ShouldBe([new Hexagon(2, 0), new Hexagon(2, 1), new Hexagon(3, -1)], ignoreOrder: true);
    }

    [Test]
    public void Destinations_OfAnEmptyArmy_AreEmpty()
    {
        Movement.Destinations(_gameSettings, _start, Army.Empty).ShouldBeEmpty();
    }

    [Test]
    public void DefaultSettings_MoveEveryUnitTypeOneHexagon()
    {
        GameSettings defaultSettings = TestSettings.Create(Maps.Disc(1));

        Movement.Range(defaultSettings).ShouldBe(new Army() { Dot = 1, Triangle = 1, Circle = 1, Square = 1 });
    }

    [Test]
    public void MovementWithinTheRangeOfEveryUnitType_IsValid()
    {
        var state = new GameState(_gameSettings);

        CommandValidator.Validate(state, "Player01", Move(_start, new Hexagon(1, 0), _army)).ShouldBeEmpty();
        CommandValidator.Validate(state, "Player01", Move(_start, new Hexagon(2, 0), new Army() { Circle = 2, Square = 4 })).ShouldBeEmpty();
        CommandValidator.Validate(state, "Player01", Move(_start, new Hexagon(3, -3), new Army() { Square = 4 })).ShouldBeEmpty();
    }

    [Test]
    public void MovementBeyondTheRange_YieldsMovementExceedsRangeForEveryUnitTypeThatIsTooSlow()
    {
        var state = new GameState(_gameSettings);
        Hexagon destination = new(0, 3);

        var validations = CommandValidator.Validate(state, "Player01", Move(_start, destination, new Army() { Dot = 1, Circle = 1, Square = 1 })).ToList();

        validations.Count.ShouldBe(2);
        var tooSlow = validations.Select(x => x.Value.ShouldBeOfType<MovementExceedsRange>()).OrderBy(x => x.Range).ToList();
        tooSlow[0].ShouldBe(new MovementExceedsRange("Player01", _start, destination, Unit.Dot, 3, 1));
        tooSlow[1].ShouldBe(new MovementExceedsRange("Player01", _start, destination, Unit.Circle, 3, 2));
    }

    [Test]
    public void MovementToAHexagonOutsideTheMap_YieldsMovementDestinationOffMap()
    {
        var state = new GameState(_gameSettings);
        Hexagon outside = new(4, 0);

        var validations = CommandValidator.Validate(state, "Player02", Move(_opponentStart, outside, new Army() { Dot = 1 })).ToList();

        var validation = validations.ShouldHaveSingleItem().Value.ShouldBeOfType<MovementDestinationOffMap>();
        validation.ShouldBe(new MovementDestinationOffMap("Player02", _opponentStart, outside));
    }

    [Test]
    public void MovementOfTheWholeArmy_IsValidExactlyOnTheHexagonsDestinationsGivesTheWholeArmy()
    {
        var state = new GameState(_gameSettings);
        Army army = new() { Circle = 2, Square = 4 };
        var destinations = Movement.Destinations(_gameSettings, _start, army);

        // One ring beyond the map, so hexagons outside are covered too.
        foreach (Hexagon hexagon in Hexagon.Disc(4).Where(x => x != _start))
        {
            bool offered = destinations.TryGetValue(hexagon, out Army reachable) && reachable == army;
            bool valid = !CommandValidator.Validate(state, "Player01", Move(_start, hexagon, army)).Any();

            valid.ShouldBe(offered, $"Destinations and Validate disagree on {hexagon}.");
        }
    }
}
