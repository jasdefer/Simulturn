using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddTraining;
public record AddTrainingCommand(string GameId, Army Army) : IRequest;
