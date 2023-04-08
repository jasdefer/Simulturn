using MediatR;

namespace SimulturnApplication.Queries.GetPlayerData;
public record GetPlayerDataCommand(string GameId) : IRequest<PlayerDataViewModel>;