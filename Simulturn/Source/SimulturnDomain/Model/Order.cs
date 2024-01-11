using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Model;
public record Order(
    HexMap<Army> Trainings,
    HexMap<Structure> Constructions,
    IImmutableSet<Move> Moves);