using SimulturnApplication.Common.Interfaces;
using SimulturnDomain.Entities;
using SimulturnDomain.Settings;
using System.Collections.Immutable;

namespace SimulturnInfrastructure.UnitTests.PersistenceTests.GameRepositoryTests;
public abstract class GameRepositoryFixture
{
    protected abstract IGameRepository GetGameRepository();

    private static readonly ImmutableArray<Player> _players = new Player[]
    {
        new Player("Player01"),
        new Player("Player02"),
        new Player("Player03"),
    }.ToImmutableArray();

    [Fact]
    public async Task HaveAllPlayersEndedTheirTurn_Should_ReturnFalse_When_NotAllPlayersEndedTheirTurn()
    {
        // Assign
        var game = new Game(Guid.NewGuid().ToString(), _players, GameSettings.Default(1));
        var gameRepository = GetGameRepository();
        await gameRepository.Add(game);
        await gameRepository.EndTurn(game.Id, _players[0].Name);
        await gameRepository.EndTurn(game.Id, _players[2].Name);

        // Act
        var allPlayersEndedTheirTurn = await gameRepository.HaveAllPlayersEndedTheirTurn(game.Id);

        // Assert
        allPlayersEndedTheirTurn.Should().Be(false);
    }

    [Fact]
    public async Task HaveAllPlayersEndedTheirTurn_Should_ReturnTrue_When_AllPlayersEndedTheirTurn()
    {
        // Assign
        var game = new Game(Guid.NewGuid().ToString(), _players, GameSettings.Default(1));
        var gameRepository = GetGameRepository();
        await gameRepository.Add(game);
        await gameRepository.EndTurn(game.Id, _players[0].Name);
        await gameRepository.EndTurn(game.Id, _players[1].Name);
        await gameRepository.EndTurn(game.Id, _players[2].Name);

        // Act
        var allPlayersEndedTheirTurn = await gameRepository.HaveAllPlayersEndedTheirTurn(game.Id);

        // Assert
        allPlayersEndedTheirTurn.Should().Be(true);
    }

    [Fact]
    public async Task EndTurn_Should_IncrementTurnNumberAndResetEndTurnFlagOfAllPlayers()
    {
        // Assign
        var game = new Game(Guid.NewGuid().ToString(), _players, GameSettings.Default(1));
        var gameRepository = GetGameRepository();
        await gameRepository.Add(game);
        await gameRepository.EndTurn(game.Id, _players[0].Name);
        await gameRepository.EndTurn(game.Id, _players[1].Name);
        await gameRepository.EndTurn(game.Id, _players[2].Name);

        // Act
        await gameRepository.NewTurn(game.Id);

        // Assert
        (await gameRepository.HaveAllPlayersEndedTheirTurn(game.Id)).Should().Be(false);
        (await gameRepository.HasPlayerEndedHisTurn(game.Id, _players[0].Name)).Should().Be(false);
        (await gameRepository.HasPlayerEndedHisTurn(game.Id, _players[1].Name)).Should().Be(false);
        (await gameRepository.HasPlayerEndedHisTurn(game.Id, _players[2].Name)).Should().Be(false);
        (await gameRepository.GetTurn(game.Id)).Should().Be(1);
    }
}
