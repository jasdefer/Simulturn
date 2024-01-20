using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
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
}