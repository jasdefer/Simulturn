using SimulturnDomain.DataStructures;
using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class ValidationHelperTests
{
    [Fact]
    public void Test1()
    {
        var requiredSpace = new Army(1, 2, 3, 4, 5);
        var armyMap = new Dictionary<Coordinates, Army>();
        var trainingMap = new Dictionary<Coordinates, Army>();
        var totalRequiredSpace = ValidationHelper.GetRequiredSpace(requiredSpace, armyMap.ToHexMap(), trainingMap.ToHexMap());
        totalRequiredSpace.Should().Be(0);
    }

    [Fact]
    public void Test2()
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
    public void Test3()
    {
        var coordinates = new Coordinates(1, 2);
        var order = new Structure();
        var turnMap = new Dictionary<ushort, IDictionary<Coordinates, Structure>>();
        var count = ValidationHelper.GetConstructionCount(3, coordinates, order,  turnMap.ToTurnHexMap());
        count.Should().Be(0);
    }

    [Fact]
    public void Test4()
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
}