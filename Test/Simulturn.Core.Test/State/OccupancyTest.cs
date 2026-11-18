using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

/// <summary>
/// The functions of <see cref="Occupancy"/> on their own. That the validators and the turn
/// resolution follow them is shown by the tests of the rules that ask there.
/// </summary>
public sealed class OccupancyTest
{
    [Test]
    public void TrainingSlots_AreOnePerBuildingOfTheTrainingType()
    {
        var compound = new Compound() { Plane = 1, Dome = 2, Pyramid = 3, Cube = 4, Axis = 5 };

        Occupancy.TrainingSlots(compound).ShouldBe(new Army() { Dot = 1, Circle = 2, Triangle = 3, Square = 4 });
    }

    [Test]
    public void FreeTrainingSlots_AreTheSlotsWithoutTheTrainings()
    {
        var compound = new Compound() { Plane = 1, Pyramid = 3 };
        var trainings = new Army() { Dot = 1, Triangle = 1 };

        Occupancy.FreeTrainingSlots(compound, trainings).ShouldBe(new Army() { Dot = 0, Triangle = 2 });
    }

    [Test]
    public void FreeTrainingSlots_AreNegativeWhereATrainingHasNoBuilding()
    {
        var compound = new Compound() { Plane = 1 };
        var trainings = new Army() { Dot = 1, Circle = 2 };

        Occupancy.FreeTrainingSlots(compound, trainings).ShouldBe(new Army() { Circle = -2 });
    }

    [Test]
    public void FreeDots_AreTheDotsWithoutOnePerConstruction()
    {
        var army = new Army() { Dot = 5, Triangle = 3 };
        var constructions = new Compound() { Axis = 1, Dome = 2 };

        Occupancy.FreeDots(army, constructions).ShouldBe(2);
    }

    [Test]
    public void FreeDots_AreNegativeWhereAConstructionHasNoDot()
    {
        var army = new Army() { Dot = 1, Square = 4 };
        var constructions = new Compound() { Plane = 2 };

        Occupancy.FreeDots(army, constructions).ShouldBe(-1);
    }

    [Test]
    public void HoldsResearch_NeedsADot()
    {
        Occupancy.HoldsResearch(new Army() { Dot = 1 }).ShouldBeTrue();
        Occupancy.HoldsResearch(new Army() { Triangle = 5, Circle = 5, Square = 5 }).ShouldBeFalse();
        Occupancy.HoldsResearch(Army.Empty).ShouldBeFalse();
    }

    [Test]
    public void ProvidedSpace_IsTheSpaceOfEveryBuilding()
    {
        GameSettings settings = TestSettings.Create(Maps.Line(-1, 1));
        var compound = new Compound() { Plane = 1, Axis = 2, Dome = 3 };

        Occupancy.ProvidedSpace(settings, compound).ShouldBe(settings.ProvidedSpace.Plane + 2 * settings.ProvidedSpace.Axis + 3 * settings.ProvidedSpace.Dome);
    }
}
