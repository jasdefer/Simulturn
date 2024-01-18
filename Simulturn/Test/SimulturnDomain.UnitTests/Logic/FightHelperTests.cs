using SimulturnDomain.Logic;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class FightHelperTests
{
    [Fact]
    public void Test()
    {
        Army army1 = new Army();
        Army army2 = new Army();
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(1.02, army1, army2);
        army1Losses.Any().Should().BeFalse();
        army2Losses.Any().Should().BeFalse();
    }

    [Fact]
    public void Test1()
    {
        Army army1 = new Army(1);
        Army army2 = new Army();
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(1.02, army1, army2);
        army1Losses.Any().Should().BeFalse();
        army2Losses.Any().Should().BeFalse();
    }

    [Fact]
    public void Test2()
    {
        Army army1 = new Army(2);
        Army army2 = new Army(1);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(20, army1, army2);
        army1Losses.Should().Be(army2);
        army2Losses.Should().Be(army2);
    }
}
