using MediatR;
using SimulturnDomain.Entities;

namespace SimulturnApplication.Commands.Order.AddConstruction;
public record AddConstructionRequest(string GameId, Structure Structure) : IRequest;