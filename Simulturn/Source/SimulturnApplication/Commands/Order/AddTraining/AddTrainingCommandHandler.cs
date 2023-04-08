using MediatR;

namespace SimulturnApplication.Commands.Order.AddTraining;
public class AddTrainingCommandHandler : IRequestHandler<AddTrainingCommand>
{
    public Task Handle(AddTrainingCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
