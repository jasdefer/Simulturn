using SimulturnDomain.Enums;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;
public record class Move(Coordinates Origin,
    HexDirection Direction,
    Army Army);