using MediatR;

namespace SimulturnApplication.Commands.Turn;
public class GetTurnRequestHandler : IRequestHandler<GetTurnRequest, TurnVm>
{
    public Task<TurnVm> Handle(GetTurnRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
