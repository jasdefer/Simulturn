using SimulturnDomain.Enums;
using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class ValidationHelperTests
{
    [Fact]
    public void GetRequiredSpace_ForArmy()
    {
        var requiredSpace = new Army(1, 2, 3, 4, 5);
        var armyMap = new Dictionary<Coordinates, Army>();
        var trainingMap = new Dictionary<Coordinates, Army>();
        var totalRequiredSpace = ValidationHelper.GetRequiredSpace(requiredSpace, armyMap.ToHexMap(), trainingMap.ToHexMap());
        totalRequiredSpace.Should().Be(0);
    }

    [Fact]
    public void GetRequiredSpace_ForArmyAndTraining()
    {
        var requiredSpace = new Army(1, 2, 3, 4, 5);
        var armyMap = new Dictionary<Coordinates, Army>()
        {
            { new Coordinates(0, 0), new Army(5) },
        };
        var trainingMap = new Dictionary<Coordinates, Army>()
        {
             { new Coordinates(0, 1), new Army(7) },
        };
        var totalRequiredSpace = ValidationHelper.GetRequiredSpace(requiredSpace, armyMap.ToHexMap(), trainingMap.ToHexMap());
        totalRequiredSpace.Should().Be(12);
    }

    [Fact]
    public void GetConstructionCount_ForEmptyTrainings()
    {
        var coordinates = new Coordinates(1, 2);
        var order = new Structure();
        var turnMap = new Dictionary<ushort, IDictionary<Coordinates, Structure>>();
        var count = ValidationHelper.GetConstructionCount(3, coordinates, order, turnMap.ToTurnHexMap());
        count.Should().Be(0);
    }

    [Fact]
    public void GetConstructionCount_ForGivenTrainings()
    {
        var coordinates = new Coordinates(1, 1);
        var order = new Structure(3, 7);
        var turnMap = new Dictionary<ushort, IDictionary<Coordinates, Structure>>()
        {
            {0, new Dictionary<Coordinates, Structure>(){ { coordinates, new Structure(2) } } },
            {2, new Dictionary<Coordinates, Structure>(){ { coordinates, new Structure(3) } } },
            {3, new Dictionary<Coordinates, Structure>(){ { coordinates, new Structure(4) } } },
            {4, new Dictionary<Coordinates, Structure>(){ { coordinates, new Structure(5) }, { new Coordinates(1, 2), new Structure(6) } } },
        };
        var count = ValidationHelper.GetConstructionCount(3, coordinates, order, turnMap.ToTurnHexMap());
        count.Should().Be(19);
    }

    [Fact]
    public void TestGetRequiredStructures()
    {
        Army army = new Army()
        {
            Triangle = 1,
            Square = 2,
            Circle = 3,
            Line = 4,
            Point = 5
        };
        var buildable = new Dictionary<Unit, Building>()
        {
            { Unit.Triangle, Building.Pyramid },
            { Unit.Square, Building.Cube },
            { Unit.Circle, Building.Sphere },
            { Unit.Line, Building.Plane },
            { Unit.Point, Building.Root }
        };
        var requiredStructures = ValidationHelper.GetRequiredStructures(buildable, army);
        requiredStructures.Should().Be(new Structure()
        {
            Pyramid = 1,
            Cube = 2,
            Sphere = 3,
            Plane = 4,
            Root = 5
        });
    }

    [Fact]
    public void GetAvailableSpace()
    {
        var space = new Structure(1, 1, 1, 1, 1, 1);
        var structureMap = new Dictionary<Coordinates, Structure>()
        {
            { new Coordinates(0, 0), new Structure(1, 2, 3, 4, 5, 6) },
            { new Coordinates(1, 0), new Structure(7, 8,9,10, 11, 12) }
        };
        var availableSpace = ValidationHelper.GetAvailableSpace(space, structureMap.ToHexMap());
        availableSpace.Should().Be(78);
    }

    [Fact]
    public void GetRequiredSpace()
    {
        var requiredSpace = new Army(1, 2, 3, 4, 5);
        var armyMap = new Dictionary<Coordinates, Army>()
        {
            {  new Coordinates(0, 0), new Army(1,1,1,1,1) }
        };
        var trainingMap = new Dictionary<Coordinates, Army>()
        {
            {  new Coordinates(0, 0), new Army(1,1,1,1,1) }
        };
        var space = ValidationHelper.GetRequiredSpace(requiredSpace, armyMap.ToHexMap(), trainingMap.ToHexMap());
        space.Should().Be(30);
    }

    [Fact]
    public void GetRequiredMatter()
    {
        var trainingMap = new Dictionary<Coordinates, Army>()
        {
            { new Coordinates(0, 0), new Army(1, 1, 1, 1, 1) }
        };
        var constructionMap = new Dictionary<Coordinates, Structure>()
        {
            {new Coordinates(0,0), new Structure(1,1,1,1,1,1) }
        };
        var armyCost = new Army(1, 2, 3, 4, 5);
        var structureCost = new Structure(1, 2, 3, 4, 5, 6);
        var matter = ValidationHelper.GetRequiredMatter(trainingMap.ToHexMap(), constructionMap.ToHexMap(), armyCost, structureCost);
        matter.Should().Be(36);
    }
}