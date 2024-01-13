using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;

public record PlayerState(short Matter,
    HexMap<Army> ArmyMap,
    HexMap<Structure> StructureMap,
    TurnMap<HexMap<Army>> TrainingMap,
    TurnMap<HexMap<Structure>> ConstructionMap,
    HexMap<Fight> Fights);

public record State(PlayerMap<PlayerState> PlayerStates,
    HexMap<ushort> RemainingMatter);