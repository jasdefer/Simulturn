using MediatR;

namespace SimulturnApplication.Commands.EndTurn;
public record EndTurnCommand(string GameId) : IRequest;