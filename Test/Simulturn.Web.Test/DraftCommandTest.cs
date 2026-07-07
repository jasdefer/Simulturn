using Simulturn.Core.Model;
using Simulturn.Web.Models;

namespace Simulturn.Web.Test;

public class DraftCommandTest
{
    [Test]
    public void ToCommand_MapsAllParts()
    {
        var draft = new DraftCommand();
        draft.Training.Dot = 2;
        draft.Construction.Dome = 1;
        draft.Upgrade = Upgrade.DotUpgrade;
        draft.UpsertMovement(new Hexagon(1, 0), new ArmyDraft { Triangle = 3 });

        var command = draft.ToCommand();
        command.Training.Dot.ShouldBe((short)2);
        command.Construction.Dome.ShouldBe((short)1);
        command.Upgrade.ShouldBe(Upgrade.DotUpgrade);
        var movement = command.MovementCommands.ShouldHaveSingleItem();
        movement.Destination.ShouldBe(new Hexagon(1, 0));
        movement.Army.Triangle.ShouldBe((short)3);
    }

    [Test]
    public void UpsertMovement_SameDestination_Overwrites()
    {
        var draft = new DraftCommand();
        var destination = new Hexagon(1, 0);
        draft.UpsertMovement(destination, new ArmyDraft { Dot = 2 });
        draft.UpsertMovement(destination, new ArmyDraft { Dot = 5 });

        var movement = draft.Movements.ShouldHaveSingleItem();
        movement.Army.Dot.ShouldBe(5);
    }

    [Test]
    public void UpsertMovement_EmptyArmy_RemovesOrder()
    {
        var draft = new DraftCommand();
        var destination = new Hexagon(1, 0);
        draft.UpsertMovement(destination, new ArmyDraft { Dot = 2 });
        draft.UpsertMovement(destination, new ArmyDraft());

        draft.Movements.ShouldBeEmpty();
        draft.IsEmpty.ShouldBeTrue();
    }

    [Test]
    public void ToCommand_ClampsToShortRange()
    {
        var draft = new DraftCommand();
        draft.Training.Dot = int.MaxValue;
        draft.ToCommand().Training.Dot.ShouldBe(short.MaxValue);
    }

    [Test]
    public void ToCommand_SkipsEmptyMovements()
    {
        var draft = new DraftCommand();
        draft.Movements.Add(new DraftMovement(new Hexagon(1, 0), new ArmyDraft()));
        draft.ToCommand().MovementCommands.ShouldBeEmpty();
    }
}
