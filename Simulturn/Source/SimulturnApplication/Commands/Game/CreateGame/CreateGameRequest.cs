using MediatR;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Commands.Game.CreateGame;
public record CreateGameRequest(IEnumerable<string> PlayerNames,
    GameSettings GameSettings) : IRequest<string>;