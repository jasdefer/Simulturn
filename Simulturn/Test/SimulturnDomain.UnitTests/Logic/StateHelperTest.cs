using SimulturnDomain.Enums;
using SimulturnDomain.Helper;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

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

    [Fact]
    public void CompleteTrainings()
    {
        var armies = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(1) },
            { _coordinates[1], new Army(2) }
        };

        var trainings = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(5) },
            { _coordinates[2], new Army(9) }
        };
        var newArmies = StateHelper.CompleteTrainings(armies.ToHexMap(), trainings.ToHexMap());
        newArmies.Keys.Should().HaveCount(3);
        newArmies[_coordinates[0]].Should().Be(new Army(6));
        newArmies[_coordinates[1]].Should().Be(new Army(2));
        newArmies[_coordinates[2]].Should().Be(new Army(9));
    }

    [Fact]
    public void MoveArmies()
    {
        var armies = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(5) },
            { _coordinates[0].GetNeighbor(HexDirection.NorthEast), new Army(9) },
            { _coordinates[0].GetNeighbor(HexDirection.SouthWest), new Army(15) },
        };
        IImmutableSet<Move> moves =
        [
            new Move(_coordinates[0], HexDirection.NorthEast, new Army(1)),
            new Move(_coordinates[0].GetNeighbor(HexDirection.SouthWest), HexDirection.SouthWest, new Army(15)),
        ];
        var newArmies = StateHelper.MoveArmies(armies.ToHexMap(), moves);
        newArmies.Keys.Should().HaveCount(3);
        newArmies[_coordinates[0]].Should().Be(new Army(4));
        newArmies[_coordinates[0].GetNeighbor(HexDirection.NorthEast)].Should().Be(new Army(10));
        newArmies[_coordinates[0].GetNeighbor(HexDirection.SouthWest).GetNeighbor(HexDirection.SouthWest)].Should().Be(new Army(15));
    }

    [Fact]
    public void AddConstructions()
    {
        var duration = new Structure(1, 10, 1, 1, 1, 1);
        var order = new Dictionary<Coordinates, Structure>()
        {
            { _coordinates[0], new Structure(1, 1) },
            { _coordinates[2], new Structure(99) },
        };
        var constructions = new Dictionary<ushort, IDictionary<Coordinates, Structure>>()
        {
            { 1, new Dictionary<Coordinates, Structure>(){ { _coordinates[0], new Structure(3) } } },
            { 2, new Dictionary<Coordinates, Structure>(){ { _coordinates[0], new Structure(5) } } },
            { 3, new Dictionary<Coordinates, Structure>(){ { _coordinates[0], new Structure(3) }, { _coordinates[1], new Structure(11) } } },
        };
        var newConstructions = StateHelper.AddConstructions(2, constructions.ToTurnHexMap(), order.ToHexMap(), duration);
        newConstructions.Keys.Should().HaveCount(4);
        newConstructions[3][_coordinates[0]].Should().Be(new Structure(4));
        newConstructions[3][_coordinates[1]].Should().Be(new Structure(11));
        newConstructions[3][_coordinates[2]].Should().Be(new Structure(99));
        newConstructions[12][_coordinates[0]].Should().Be(new Structure(0, 1));
    }

    [Fact]
    public void AddTrainings()
    {
        var duration = new Army(1, 10, 1, 1, 1);
        var order = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(1, 1) },
            { _coordinates[2], new Army(99) },
        };
        var trainings = new Dictionary<ushort, IDictionary<Coordinates, Army>>()
        {
            { 1, new Dictionary<Coordinates, Army>(){ { _coordinates[0], new Army(3) } } },
            { 2, new Dictionary<Coordinates, Army>(){ { _coordinates[0], new Army(5) } } },
            { 3, new Dictionary<Coordinates, Army>(){ { _coordinates[0], new Army(3) }, { _coordinates[1], new Army(11) } } },
        };
        var newTrainings = StateHelper.AddTrainings(2, trainings.ToTurnHexMap(), order.ToHexMap(), duration);
        newTrainings.Keys.Should().HaveCount(4);
        newTrainings[3][_coordinates[0]].Should().Be(new Army(4));
        newTrainings[3][_coordinates[1]].Should().Be(new Army(11));
        newTrainings[3][_coordinates[2]].Should().Be(new Army(99));
        newTrainings[12][_coordinates[0]].Should().Be(new Army(0, 1));
    }

    [Fact]
    public void GetConstructionCost()
    {
        Structure structureCost = new Structure(1, 2);
        var constructions = new Dictionary<Coordinates, Structure>()
        {
            { _coordinates[0], new Structure(1,2 ) },
            { _coordinates[1], new Structure(5,6) }
        };
        var cost = StateHelper.GetConstructionCost(structureCost, constructions.ToHexMap());
        cost.Should().Be(22);
    }

    [Fact]
    public void GetTrainingCost()
    {
        Army armyCost = new Army(1, 2);
        var trainings = new Dictionary<Coordinates, Army>()
        {
            { _coordinates[0], new Army(1,2 ) },
            { _coordinates[1], new Army(5,6) }
        };
        var cost = StateHelper.GetTrainingCost(armyCost, trainings.ToHexMap());
        cost.Should().Be(22);
    }
}
