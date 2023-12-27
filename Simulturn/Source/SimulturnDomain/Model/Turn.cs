using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Model;
public record Turn(
    ImmutableDictionary<Coordinates, Army> Trainings,
    ImmutableDictionary<Coordinates, Structure> Constructions,
    IImmutableSet<Move> Moves);