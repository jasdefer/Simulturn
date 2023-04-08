using MediatR;

namespace SimulturnApplication.Queries.GetPlayerData;
public class GetPlayerDataCommandHandler :
    IRequestHandler<GetPlayerDataCommand, PlayerDataViewModel>
{
    public Task<PlayerDataViewModel> Handle(GetPlayerDataCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
