using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Model;

public record PlayerState(short Matter,
    HexMap<Army> ArmyMap,
    HexMap<Structure> Structures,
    TurnMap<HexMap<Army>> TrainingMap,
    TurnMap<HexMap<Structure>> ConstructionMap,
    HexMap<ImmutableDictionary<string, Army>> Fights);

public record State(ImmutableDictionary<string, PlayerState> PlayerStates,
    HexMap<ushort> RemainingMatter,
    HexMap<ImmutableDictionary<string, Army>> Fights);