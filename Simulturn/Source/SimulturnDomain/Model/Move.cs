using SimulturnDomain.Enums;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;
public record class Move(HexDirection Direction,
    Army Army);