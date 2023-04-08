using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddMovement;
public record AddMovementCommand(string GameId,
    Coordinates Origin,
    Coordinates Destination,
    Army Army) : IRequest
{
}
