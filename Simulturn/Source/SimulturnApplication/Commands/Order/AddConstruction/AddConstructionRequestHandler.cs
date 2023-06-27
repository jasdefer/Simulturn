using MediatR;

namespace SimulturnApplication.Commands.Order.AddConstruction;
public class AddConstructionRequestHandler : IRequestHandler<AddConstructionRequest>
{
    public Task Handle(AddConstructionRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
