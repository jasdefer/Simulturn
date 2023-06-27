using MediatR;

namespace SimulturnApplication.Commands.Order.AddMovement;
public class AddMovementRequestHandler : IRequestHandler<AddMovementRequest>
{
    public Task Handle(AddMovementRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
