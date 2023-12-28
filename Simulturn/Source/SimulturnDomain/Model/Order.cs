using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Model;
public record Order(
    ImmutableDictionary<Coordinates, Army> Trainings,
    ImmutableDictionary<Coordinates, Structure> Constructions,
    IImmutableSet<Move> Moves);