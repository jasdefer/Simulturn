using SimulturnDomain.DataStructures;
using SimulturnDomain.Logic;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class ValidationHelperTests
{
    [Fact]
    public void Test()
    {
        var requiredSpace = new Army(1, 2, 3, 4, 5);
        var armyMap = new Dictionary<Coordinates, Army>();
        var trainingMap = new Dictionary<Coordinates, Army>();
        var totalRequiredSpace = ValidationHelper.GetRequiredSpace(requiredSpace, new HexMap<Army>(armyMap), new HexMap<Army>(trainingMap));
        totalRequiredSpace.Should().Be(0);
    }
}