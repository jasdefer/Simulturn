using SimulturnDomain.Logic;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class FightHelperTests
{
    [Fact]
    public void EmptyArmies_NoEngagement_NoLosses()
    {
        Army army1 = new Army();
        Army army2 = new Army();
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Any().Should().BeFalse();
        army2Losses.Any().Should().BeFalse();
    }

    [Fact]
    public void SingleTriangleVsEmpty_NoEngagement_NoLosses()
    {
        Army army1 = new Army(1);
        Army army2 = new Army();
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Any().Should().BeFalse();
        army2Losses.Any().Should().BeFalse();
    }

    [Fact]
    public void TwoTrianglesVsOneTriangle_Overpower_SurvivorVictory()
    {
        Army army1 = new Army(2);
        Army army2 = new Army(1);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(army2);
        army2Losses.Should().Be(army2);
    }

    [Fact]
    public void TenTrianglesVsFiveTriangles_DoubleStrength_VictoryWithLosses()
    {
        Army army1 = new Army(10);
        Army army2 = new Army(5);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(new Army(3));
        army2Losses.Should().Be(army2);
    }

    [Fact]
    public void TenCirclesVsTenTriangles_CompleteDominance_TotalVictory()
    {
        Army army1 = new Army(10,0,0);
        Army army2 = new Army(0,0,10);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(Army.Empty);
        army2Losses.Should().Be(army2);
    }

    [Fact]
    public void TenTrianglesVsTenCircles_CompleteLoss_NoSurvivors()
    {
        Army army1 = new Army(0, 0, 10);
        Army army2 = new Army(10, 0, 0);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(army1);
        army2Losses.Should().Be(Army.Empty);
    }

    [Fact]
    public void TenSquaresVsTenCircles_UnderdogVictory_NoLosses()
    {
        Army army1 = new Army(0, 10, 0);
        Army army2 = new Army(10, 0, 0);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(Army.Empty);
        army2Losses.Should().Be(army2);
    }

    [Fact]
    public void TenCirclesVsTenSquares_CompleteLoss_NoSurvivors()
    {
        Army army1 = new Army(10, 0, 0);
        Army army2 = new Army(0, 10, 0);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(army1);
        army2Losses.Should().Be(Army.Empty);
    }

    [Fact]
    public void TenTrianglesVsTenSquares_UnderdogVictory_NoLosses()
    {
        Army army1 = new Army(0, 0, 10);
        Army army2 = new Army(0, 10, 0);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(Army.Empty);
        army2Losses.Should().Be(army2);
    }

    [Fact]
    public void TenSquaresVsTenTriangles_CompleteLoss_NoSurvivors()
    {
        Army army1 = new Army(0, 10, 0);
        Army army2 = new Army(0, 0, 10);
        (Army army1Losses, Army army2Losses) = FightHelper.Fight(2, army1, army2);
        army1Losses.Should().Be(army1);
        army2Losses.Should().Be(Army.Empty);
    }
}
