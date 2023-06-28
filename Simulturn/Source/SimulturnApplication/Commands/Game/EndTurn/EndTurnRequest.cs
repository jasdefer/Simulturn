using MediatR;

namespace SimulturnApplication.Commands.Game.EndTurn;
public record EndTurnRequest(string GameId) : IRequest;