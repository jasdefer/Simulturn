using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddTraining;
public record AddTrainingRequest(string GameId, Army Army) : IRequest;
