using MediatR;
using SimulturnApplication.Common.Interfaces;

namespace SimulturnApplication.Commands.Game.EndTurn;
public class EndTurnCommandHandler : IRequestHandler<EndTurnCommand>
{
    private readonly ICurrentUserService currentUserService;
    private readonly IPlayerRepository playerRepository;
    private readonly IGameRepository gameRepository;

    public EndTurnCommandHandler(ICurrentUserService currentUserService,
        IPlayerRepository playerRepository,
        IGameRepository gameRepository)
    {
        this.currentUserService = currentUserService;
        this.playerRepository = playerRepository;
        this.gameRepository = gameRepository;
    }
    public async Task Handle(EndTurnCommand request, CancellationToken cancellationToken)
    {
        var playerId = currentUserService.UserId;
        await playerRepository.EndTurn(request.GameId, playerId);

        var allPlayersEndedTheirTurn = await gameRepository.AllPlayersEndedTheirTurn(request.GameId);
        if (allPlayersEndedTheirTurn)
        {
            // End turn
        }
    }
}
