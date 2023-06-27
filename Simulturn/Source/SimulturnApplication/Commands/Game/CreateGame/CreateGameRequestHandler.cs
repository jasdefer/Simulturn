using MediatR;
using SimulturnApplication.Common.Interfaces;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Game.CreateGame;
public class CreateGameRequestHandler : IRequestHandler<CreateGameRequest, string>
{
    private readonly IGameRepository _gameRepository;

    public CreateGameRequestHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }
    public async Task<string> Handle(CreateGameRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString();
        var players = request
            .PlayerNames
            .Select(name => new Player(name));
        var game = new SimulturnDomain.Entities.Game(id, players, request.GameSettings);
        await _gameRepository.Add(game);
        return game.Id;
    }
}
