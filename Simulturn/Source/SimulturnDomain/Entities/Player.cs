namespace SimulturnDomain.Entities;
public record Player(string Name,
    ushort Matter,
    IReadOnlyDictionary<Coordinates, Army> Armies,
    IReadOnlyDictionary<Coordinates, Structure> Structures,
    IReadOnlyDictionary<Coordinates, Training> Trainings,
    IReadOnlyDictionary<Coordinates, Construction> Constructions,
    IReadOnlyDictionary<ushort, Income> IncomePerTurn,
    IReadOnlyDictionary<ushort, Movement> MovementPerTurn);
