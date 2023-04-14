using MediatR;
using SimulturnApplication.Common.GameLogik;
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
        this._currentUserService = currentUserService;
        this._playerRepository = playerRepository;
        this._gameRepository = gameRepository;
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

        // End turn
        var game = await _gameRepository.Get(request.GameId);
        Turn.EndTurn(game);
        await _gameRepository.Update(game);
    }
}
