using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class FightHelperTests
{
    #region Fight
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
        Army army1 = new Army(10, 0, 0);
        Army army2 = new Army(0, 0, 10);
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
    #endregion

    #region Destroy
    [Fact]
    public void EmptyArmy_FullyFortifiedStructure_MinimalDamage_NoDestruction()
    {
        Army army = new Army();
        Structure structure = new Structure(10, 10, 10, 10, 10, 10);
        Army damage = new Army(1, 1, 1);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Any().Should().BeFalse();
    }

    [Fact]
    public void SingleUnitArmy_FullyFortifiedStructure_LowDamageHighArmor_NoDestruction()
    {
        Army army = new Army(1);
        Structure structure = new Structure(10, 10, 10, 10, 10, 10);
        Army damage = new Army(1, 1, 1);
        Structure armor = new Structure(2, 3, 4, 5, 6, 7);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Any().Should().BeFalse();
    }

    [Fact]
    public void SingleUnitArmy_FullyFortifiedStructure_EqualDamageArmor_MinorDestruction()
    {
        Army army = new Army(1);
        Structure structure = new Structure(10, 10, 10, 10, 10, 10);
        Army damage = new Army(1, 1, 1);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Should().Be(new Structure(1));
    }

    [Fact]
    public void SingleUnitArmy_FullyFortifiedStructure_IncreasedDamageMinorArmor_SpecificDestruction()
    {
        Army army = new Army(1);
        Structure structure = new Structure(10, 10, 10, 10, 10, 10);
        Army damage = new Army(2, 1, 1);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Should().Be(new Structure(2));
    }

    [Fact]
    public void SingleUnitArmy_MinimalFortification_IncreasedDamageMinorArmor_MinorDestruction()
    {
        Army army = new Army(1);
        Structure structure = new Structure(1, 1, 1, 1, 1, 1);
        Army damage = new Army(2, 1, 1);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Should().Be(new Structure(1));
    }

    [Fact]
    public void SingleUnitArmy_MinimalFortification_HighDamageMinorArmor_ModerateDestruction()
    {
        Army army = new Army(1);
        Structure structure = new Structure(1, 1, 1, 1, 1, 1);
        Army damage = new Army(3, 1, 1);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Should().Be(new Structure(1, 1));
    }

    [Fact]
    public void BalancedArmy_ModeratelyFortifiedStructure_HighDamageLowArmor_SignificantDestruction()
    {
        Army army = new Army(1, 1, 1);
        Structure structure = new Structure(5, 5, 5, 5, 5, 5);
        Army damage = new Army(10, 10, 10);
        Structure armor = new Structure(1, 2, 3, 4, 5, 6);
        Structure destruction = FightHelper.Destroy(army, structure, damage, armor);
        destruction.Should().Be(new Structure(5, 5, 5));
    }
    #endregion

    [Fact]
    public void GetFights()
    {
        var coordinates = new Coordinates[]
        {
            new Coordinates(0,0),
            new Coordinates(0, 1),
            new Coordinates(0, 2)
        };
        var armyMap = new Dictionary<Coordinates, IDictionary<string, Army>>()
        {
            {
                coordinates[0], new Dictionary<string, Army>()
                {
                    { "Player01", new Army(1) },
                    { "Player02", new Army(2) },
                }
            },
            {
                coordinates[1], new Dictionary<string, Army>()
                {
                    { "Player01", new Army(4) },
                    { "Player02", new Army(2) },
                }
            },
            {
                coordinates[2], new Dictionary<string, Army>()
                {
                    { "Player01", new Army(4) },
                }
            },
        };
        var fights = FightHelper.GetFights(2, armyMap.ToHexPlayerMap());
        fights.ContainsKey("Player01");
        fights["Player01"].Keys.Count().Should().Be(2);
        fights["Player01"].Keys.Should().Contain(coordinates[0]);
        fights["Player01"].Keys.Should().Contain(coordinates[1]);
        fights.ContainsKey("Player02");
        fights["Player02"].Keys.Count().Should().Be(2);
        fights["Player02"].Keys.Should().Contain(coordinates[0]);
        fights["Player02"].Keys.Should().Contain(coordinates[1]);
    }
}