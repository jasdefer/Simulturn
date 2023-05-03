using MediatR;
using SimulturnApplication.Common.Interfaces;

namespace SimulturnApplication.Commands.Game.EndTurn;
public class EndTurnCommandHandler : IRequestHandler<EndTurnCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IGameRepository _gameRepository;

    public EndTurnCommandHandler(ICurrentUserService currentUserService,
        IPlayerRepository playerRepository,
        IGameRepository gameRepository)
    {
        _currentUserService = currentUserService;
        _playerRepository = playerRepository;
        _gameRepository = gameRepository;
    }
    public async Task Handle(EndTurnCommand request, CancellationToken cancellationToken)
    {
        var playerId = _currentUserService.UserId;
        await _playerRepository.EndTurn(request.GameId, playerId);

        var endTurn = await _gameRepository.AllPlayersEndedTheirTurn(request.GameId);
        if (!endTurn)
        {
            return;
        }

        EndTurn(request.GameId);
    }

    private async Task EndTurn(string gameId)
    {
        var settings = await _gameRepository.GetSettings(gameId);
        var players = await _gameRepository.GetPlayers(gameId);
        foreach (var player in players)
        {
            var income = GetIncome(settings.ArmySettings.Income, player.);
            _playerRepository.AddIncome(gameId, player, income);
            ResolveChanges();
        }
    }
}
