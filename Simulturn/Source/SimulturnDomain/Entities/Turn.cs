using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Entities;
public record Turn(string PlayerId,
    ushort TurnNumber,
    ImmutableDictionary<Coordinates, Army> Trainings,
    ImmutableDictionary<Coordinates, Structure> Constructions,
    IImmutableSet<Move> Moves);