using SimulturnDomain.DataStructures;
using SimulturnDomain.Logic;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;

namespace SimulturnDomain.UnitTests.Logic;
public class StateHelperTests
{
    private static readonly string[] _playerIds = new[] { "Player01", "Player02" };

    [Fact]
    public void Test()
    {
        // Assign
        GameSettings gameSettings = GameSettings.Default(1);
        TurnDictionary turnDictionary = new TurnDictionary(new[] { _playerIds[0] });
        Game game = new Game("Game01", turnDictionary, gameSettings);

        // Act
        var state = StateHelper.GetStatesPerPlayer(game);

        // Assert
    }
}
