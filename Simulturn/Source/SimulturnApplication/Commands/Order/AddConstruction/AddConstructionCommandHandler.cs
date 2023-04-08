using MediatR;

namespace SimulturnApplication.Commands.Order.AddConstruction;
public class AddConstructionCommandHandler : IRequestHandler<AddConstructionCommand>
{
    public Task Handle(AddConstructionCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
