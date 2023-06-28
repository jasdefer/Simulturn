using SimulturnApplication.Commands.Game.CreateGame;
using SimulturnApplication.Commands.Game.EndTurn;
using SimulturnDomain.Settings;
using SimulturnInfrastructure.Persistance.InMemory;
using System.Collections.Immutable;

namespace SimulturnApplication.IntegrationTests.GameTests;
public class TurnTests
{
    private static readonly ImmutableArray<string> _playerNames = new[] { "Player01", "Player02", "Player03" }.ToImmutableArray();
    private static readonly CreateGameRequest _createGameRequest = 
        new CreateGameRequest(_playerNames, GameSettings.Default());

    [Fact]
    public async Task Test()
    {
        // Assign
        var userService = new InMemoryCurrentUserService();
        var sender = Testing.GetSender(userService);
        

        // Act
        string gameId = await sender.Send(_createGameRequest);
        var endTurnRequest = new EndTurnRequest(gameId);

        userService.UserId = _playerNames[0];
        await sender.Send(endTurnRequest);

        userService.UserId = _playerNames[1];
        await sender.Send(endTurnRequest);

        userService.UserId = _playerNames[2];
        await sender.Send(endTurnRequest);

        // Assert
    }
}
