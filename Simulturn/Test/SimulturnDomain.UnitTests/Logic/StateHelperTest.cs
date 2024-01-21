using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.UnitTests.Logic;
public class StateHelperTest
{
    private static readonly Coordinates[] _coordinates = new Coordinates[]
    {
        new Coordinates(0, 0),
        new Coordinates(0, 1),
        new Coordinates(0, 2)
    };
    private static readonly string[] _players = ["Player01", "Player02"];

    [Fact]
    public void RemoveMatter()
    {
        var remainingMatter = new Dictionary<Coordinates, ushort>()
        {
            { _coordinates[0], 5 },
            { _coordinates[1], 5 },
            { _coordinates[2], 5 }
        };
        var revenue = new Dictionary<Coordinates, ushort>()
        {
            { _coordinates[0], 5 },
            { _coordinates[2], 3 }
        };
        var result = StateHelper.RemoveMatter(remainingMatter.ToHexMap(), revenue.ToHexMap());
        result.Keys.Count().Should().Be(2);
        result.Keys.Should().Contain(_coordinates[1]);
        result[_coordinates[1]].Should().Be(5);
        result.Keys.Should().Contain(_coordinates[2]);
        result[_coordinates[2]].Should().Be(2);
    }

    [Fact]
    public void RemoveMatter_RemoveTooMuch()
    {
        var remainingMatter = new Dictionary<Coordinates, ushort>()
        {
            { _coordinates[0], 5 }
        };
        var revenue = new Dictionary<Coordinates, ushort>()
        {
            { _coordinates[0], 6 }
        };
        Action act = () => StateHelper.RemoveMatter(remainingMatter.ToHexMap(), revenue.ToHexMap());
        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void RemoveDestruction()
    {
        Army damage = new Army(1, 2, 3, 4, 5);
        Structure armor = new Structure(1, 1, 1, 1, 1);
        var structureMap = new Dictionary<Coordinates, Structure>()
        {
            { _coordinates[0], new Structure(1,1) }
        };
        var fights = new Dictionary<string, IDictionary<Coordinates, Fight>>()
        {
            { _players[0], new Dictionary<Coordinates, Fight>(){ { _coordinates[0], new Fight(new Army(2), new Army(1)) } } }
        };
        var remainingStructures = StateHelper.RemoveDestruction(_players[0], damage, armor, structureMap.ToHexMap(), fights.ToPlayerHexMap());
        remainingStructures.Keys.Should().HaveCount(1);
        remainingStructures.ContainsKey(_coordinates[0]).Should().BeTrue();
    }

    [Fact]
    public void RemoveLosses()
    {
        var armyMap = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(2) },
            { _coordinates[1], new Army(5) },
            { _coordinates[2], new Army(7) },
        };

        var fights = new Dictionary<Coordinates, Fight>()
        {
            { _coordinates[1], new Fight(new Army(5), new Army(5)) },
            { _coordinates[2], new Fight(new Army(7), new Army(3)) },
        };
        var newArmyMap = StateHelper.RemoveLosses(armyMap.ToHexMap(), fights.ToHexMap());
        newArmyMap.Keys.Should().HaveCount(2);
        newArmyMap.ContainsKey(_coordinates[0]).Should().BeTrue();
        newArmyMap[_coordinates[0]].Should().Be(new Army(2));
        newArmyMap[_coordinates[2]].Should().Be(new Army(4));
    }

    [Fact]
    public void GetFightingArmies()
    {
        var armiesPerPlayer = new Dictionary<string, IDictionary<Coordinates, Army>>()
        {
            {
                _players[0], new Dictionary<Coordinates, Army>()
                {
                    { _coordinates[0], new Army(1) },
                    { _coordinates[1], new Army(2) }
                }
            },
            {
                _players[1], new Dictionary<Coordinates, Army>()
                {
                    { _coordinates[0], new Army(3) },
                    { _coordinates[2], new Army(4) }
                }
            }
        };
        var fightingArmies = StateHelper.GetFightingArmies(armiesPerPlayer.ToPlayerHexMap());
        fightingArmies.ContainsKey(_coordinates[0]).Should().BeTrue();
        fightingArmies.Keys.Should().HaveCount(1);
        fightingArmies[_coordinates[0]].Keys.Should().HaveCount(2);
        fightingArmies[_coordinates[0]][_players[0]].Should().Be(new Army(1));
        fightingArmies[_coordinates[0]][_players[1]].Should().Be(new Army(3));
    }

    [Fact]
    public void CompleteConstructions()
    {
        var structures = new Dictionary<Coordinates, Structure>()
        {
            { _coordinates[0], new Structure(1) },
            { _coordinates[1], new Structure(2) }
        };

        var constructions = new Dictionary<Coordinates, Structure>()
        {
            { _coordinates[0], new Structure(5) },
            { _coordinates[2], new Structure(9) }
        };
        var newStructures = StateHelper.CompleteConstructions(structures.ToHexMap(), constructions.ToHexMap());

        newStructures.Keys.Should().HaveCount(3);
        newStructures[_coordinates[0]].Should().Be(new Structure(6));
        newStructures[_coordinates[1]].Should().Be(new Structure(2));
        newStructures[_coordinates[2]].Should().Be(new Structure(9));
    }
}
