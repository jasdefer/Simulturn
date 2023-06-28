using MediatR;
using SimulturnApplication.Common.Interfaces;

namespace SimulturnApplication.Commands.Game.EndTurn;
public class EndTurnRequestHandler : IRequestHandler<EndTurnRequest>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameRepository _gameRepository;

    public EndTurnRequestHandler(ICurrentUserService currentUserService,
        IGameRepository gameRepository)
    {
        _currentUserService = currentUserService;
        _gameRepository = gameRepository;
    }
    public async Task Handle(EndTurnRequest request, CancellationToken cancellationToken)
    {
        var player = _currentUserService.UserId;
        await _gameRepository.EndTurn(request.GameId, player);
        var allPlayersEndedTheirTurn = await _gameRepository.HaveAllPlayersEndedTheirTurn(request.GameId);
        if (allPlayersEndedTheirTurn)
        {
            await _gameRepository.NewTurn(request.GameId);
        }
    }
}