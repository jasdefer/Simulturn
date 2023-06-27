using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddMovement;
public record AddMovementRequest(string GameId,
    Coordinates Origin,
    Coordinates Destination,
    Army Army) : IRequest
{
}
