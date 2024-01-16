using SimulturnDomain.Logic;
using SimulturnDomain.Settings;

namespace SimulturnDomain.UnitTests.Logic;
public class IncomeHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(49)]
    public void GetIncome_NoUpkeep(ushort space)
    {
        var upkeepLevels = new UpkeepLevel[]
        {
            new UpkeepLevel(50, 0.2),
            new UpkeepLevel(30, 0.5)
        };

        // Assign
        var income = IncomeHelper.GetIncome(100, space, upkeepLevels);
        income.Should().Be(100);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(79)]
    public void GetIncome_LowUpkeep(ushort space)
    {
        var upkeepLevels = new UpkeepLevel[]
        {
            new UpkeepLevel(50, 0.2),
            new UpkeepLevel(30, 0.5)
        };

        // Assign
        var income = IncomeHelper.GetIncome(100, space, upkeepLevels);
        income.Should().Be(80);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(90)]
    [InlineData(1000)]
    public void GetIncome_HighUpkeep(ushort space)
    {
        var upkeepLevels = new UpkeepLevel[]
        {
            new UpkeepLevel(50, 0.2),
            new UpkeepLevel(30, 0.5)
        };

        // Assign
        var income = IncomeHelper.GetIncome(100, space, upkeepLevels);
        income.Should().Be(50);
    }
}
