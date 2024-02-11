using SimulturnDomain.DataStructures;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;

/// <summary>
/// Contains all information available for a given player and turn.
/// </summary>
/// <param name="Matter">The total available matter at the beginning of this turn.</param>
/// <param name="ArmyMap">The location of all armies of the given player at the beginning of this turn.</param>
/// <param name="StructureMap">The location of all structures of the given player at the beginning of this turn.</param>
/// <param name="TrainingMap">The number of remaining turns to complete the training of units per coordiantes at the beginning of this turn. A 0 indicates, that the army will be available at the beginning of next turn.</param>
/// <param name="ConstructionMap">The number of remaining turns to complete the construction of buildings per coordiantes at the beginning of this turn. A 0 indicates, that the structure will be available at the beginning of next turn.</param>
/// <param name="Fights">The fights of the given player fought after the previous turn.</param>
public record PlayerState(short Matter,
    HexMap<Army> ArmyMap,
    HexMap<Structure> StructureMap,
    TurnMap<HexMap<Army>> TrainingMap,
    TurnMap<HexMap<Structure>> ConstructionMap,
    HexMap<Fight> Fights);

public record State(PlayerMap<PlayerState> PlayerStates,
    HexMap<ushort> RemainingMatter);