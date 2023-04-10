using MediatR;

namespace SimulturnApplication.Commands.Game.EndTurn;
public record EndTurnCommand(string GameId) : IRequest;