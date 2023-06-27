using MediatR;
using SimulturnApplication.Common.Interfaces;

namespace SimulturnApplication.Commands.Game.EndTurn;
public class EndTurnCommandHandler : IRequestHandler<EndTurnCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPlayerRepository _playerRepository;

    public EndTurnCommandHandler(ICurrentUserService currentUserService,
        IPlayerRepository playerRepository)
    {
        _currentUserService = currentUserService;
        _playerRepository = playerRepository;
    }
    public async Task Handle(EndTurnCommand request, CancellationToken cancellationToken)
    {
        var player = _currentUserService.UserId;
        await _playerRepository.EndTurn(request.GameId, player);
    }
}