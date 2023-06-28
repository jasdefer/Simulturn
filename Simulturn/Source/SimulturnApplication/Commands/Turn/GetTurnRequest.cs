using MediatR;

namespace SimulturnApplication.Commands.Turn;
public record GetTurnRequest(string GameId) : IRequest<TurnVm>;
