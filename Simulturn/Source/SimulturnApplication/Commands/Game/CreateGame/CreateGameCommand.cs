using MediatR;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Commands.Game.CreateGame;
public record CreateGameCommand(IEnumerable<string> PlayerIds,
    GameSettings GameSettings) : IRequest<string>;