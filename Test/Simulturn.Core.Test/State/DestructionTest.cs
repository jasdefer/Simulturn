using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

public sealed class DestructionTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. Player01 starts without an army on a plane and a cube,
    /// Player02 with 10 squares that cross the whole line in one turn.
    /// A square deals 5 structure damage, a dot 1. An axis has 10 armor, a cube 20, a plane 30.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01", army: Army.Empty, compound: new Compound() { Plane = 1, Cube = 1 }))
        .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 }))) with
    {
        MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 }
    };

    [Test]
    public void ArmyWithTheDamageOfTheArmor_DestroysTheBuilding()
    {
        Destruction.Of(_gameSettings, new Army() { Square = 6 }, new Compound() { Plane = 1 }).ShouldBe(new Compound() { Plane = 1 });
    }

    [Test]
    public void ArmyWithLessDamageThanTheArmor_DestroysNothing()
    {
        Destruction.Of(_gameSettings, new Army() { Square = 5 }, new Compound() { Plane = 1 }).ShouldBe(Compound.Empty);
        Destruction.Of(_gameSettings, Army.Empty, new Compound() { Axis = 1 }).ShouldBe(Compound.Empty);
    }

    [Test]
    public void Damage_IsSpentOnTheLowestArmorFirst()
    {
        Compound compound = new() { Axis = 1, Cube = 1, Plane = 1 };

        Destruction.Of(_gameSettings, new Army() { Square = 6 }, compound).ShouldBe(new Compound() { Axis = 1, Cube = 1 });
    }

    [Test]
    public void DamageLeftOver_IsLost()
    {
        // 35 damage: the axis takes 10, the remaining 25 are not enough for the plane.
        Compound compound = new() { Axis = 1, Plane = 1 };

        Destruction.Of(_gameSettings, new Army() { Square = 7 }, compound).ShouldBe(new Compound() { Axis = 1 });
    }

    [Test]
    public void EveryUnitType_AddsItsStructureDamage()
    {
        // 5 dots and a square deal 10 damage together.
        Destruction.Of(_gameSettings, new Army() { Dot = 5, Square = 1 }, new Compound() { Axis = 2 }).ShouldBe(new Compound() { Axis = 1 });
    }

    [Test]
    public void Army_DestroysNoMoreBuildingsThanThereAre()
    {
        Destruction.Of(_gameSettings, new Army() { Square = 10 }, new Compound() { Axis = 2 }).ShouldBe(new Compound() { Axis = 2 });
    }

    [Test]
    public void TurnResolution_DestroysWhatTheRuleForecasts()
    {
        // Arrange
        GameState state = new(_gameSettings);
        Army squares = new() { Square = 10 };
        Compound forecast = Destruction.Of(_gameSettings, squares, state.PlayerStates["Player01"].Compounds[_player1Start]);

        // Act
        TurnResult result = state.ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player2Start, _player1Start, squares)
        ]));

        // Assert
        forecast.ShouldBe(new Compound() { Plane = 1, Cube = 1 });
        result.Report.Destructions.ShouldHaveSingleItem()
            .ShouldBe(new DestructionReport(_player1Start, "Player02", "Player01", forecast));
    }
}
