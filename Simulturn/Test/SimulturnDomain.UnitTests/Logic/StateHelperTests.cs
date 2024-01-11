using SimulturnDomain.DataStructures;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.UnitTests.Logic;
public class StateHelperTests
{
    private static readonly string[] _playerIds = new[] { "Player01", "Player02" };

    [Fact]
    public void StartGameTests()
    {
        // Assign
        GameSettings gameSettings = GameSettings.Default(1);
        TurnDictionary turnDictionary = new TurnDictionary(new[] { _playerIds[0] });
        Game game = new Game("Game01", turnDictionary, gameSettings);

        // Act
        var state = StateHelper.GetStatesPerPlayer(game);

        // Assert
        state.Keys.Should().BeEmpty();
    }

    [Fact]
    public void SingleTurnTest()
    {
        // Assign
        GameSettings gameSettings = GameSettings.Default(1);
        Coordinates player1Start = new Coordinates(-1, +2);
        TurnDictionary turnDictionary = new TurnDictionary(new[] { _playerIds[0] });
        var trainings = new Dictionary<Coordinates, Army>()
        {
            { player1Start, new Army(0,0,0,0,1) }
        };
        var constructions = new Dictionary<Coordinates, Structure>()
        {
            { player1Start, new Structure(0,0,0,0,0,1) }
        };
        Order order = new Order(trainings, constructions, ImmutableHashSet.Create<Move>());
        turnDictionary.AddTurn(_playerIds[0], order);
        Game game = new Game("Game01", turnDictionary, gameSettings);

        // Act
        var state = StateHelper.GetStatesPerPlayer(game);

        // Assert
        state.Keys.Should().ContainSingle().Which.Should().Be(0);
        state[0].PlayerStates.Should().ContainSingle().Which.Key.Should().Be(_playerIds[0]);
        var map = state[0].PlayerStates[_playerIds[0]].ArmyMap;
    }
}
