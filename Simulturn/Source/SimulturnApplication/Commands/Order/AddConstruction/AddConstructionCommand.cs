using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddConstruction;
public record AddConstructionCommand(string GameId, Structure Structure) : IRequest;