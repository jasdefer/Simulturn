using MediatR;

namespace SimulturnApplication.Commands.Order.AddTraining;
public class AddTrainingRequestHandler : IRequestHandler<AddTrainingRequest>
{
    public Task Handle(AddTrainingRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
