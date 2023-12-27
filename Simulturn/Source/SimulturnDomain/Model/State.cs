using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;

public record State(ushort Matter,
    HexMap<Army> ArmyMap,
    HexMap<Structure> Structures,
    TurnMap<HexMap<Army>> TrainingMap,
    TurnMap<HexMap<Structure>> ConstructionMap);