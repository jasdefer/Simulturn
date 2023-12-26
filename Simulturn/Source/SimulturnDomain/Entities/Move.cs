using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Entities;
public record class Move(Coordinates Origin,
    Coordinates Destination,
    Army Army);