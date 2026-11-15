using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

public sealed class BuildableHexagonTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends with 5 dots and a
    /// plane each. The start hexagon of Player02 is not buildable.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02") with { IsBuildable = false }));

    private static readonly Dictionary<Hexagon, Command> _axisAtPlayer2Start = Command.Create(
        (_player2Start, new Command() { Construction = new Compound() { Axis = 1 } }));

    [Test]
    public void ConstructionOnHexagonThatIsNotBuildable_YieldsHexagonNotBuildable()
    {
        GameState state = new(_gameSettings);

        var validations = CommandValidator.Validate(state, "Player02", _axisAtPlayer2Start).ToList();

        validations.ShouldHaveSingleItem().Value.ShouldBe(new HexagonNotBuildable("Player02", _player2Start));
    }

    [Test]
    public void ConstructionOnBuildableHexagon_IsValid()
    {
        GameState state = new(_gameSettings);
        var commands = Command.Create((_player1Start, new Command() { Construction = new Compound() { Axis = 1 } }));

        CommandValidator.Validate(state, "Player01", commands).ShouldBeEmpty();
    }

    [Test]
    public void TrainingOnHexagonThatIsNotBuildable_IsValid()
    {
        GameState state = new(_gameSettings);
        var commands = Command.Create((_player2Start, new Command() { Training = new Army() { Dot = 1 } }));

        CommandValidator.Validate(state, "Player02", commands).ShouldBeEmpty();
    }
}
