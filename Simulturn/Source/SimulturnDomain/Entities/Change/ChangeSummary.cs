namespace SimulturnDomain.Entities.Change;
public record ChangeSummary(Income Income,
    IEnumerable<Movement> Movements,
    IReadOnlyDictionary<Coordinates, Fight> FightsPerCoordinates,
    IReadOnlyDictionary<Coordinates, Army> TrainedArmiesPerCoordinates,
    IReadOnlyDictionary<Coordinates, Structure> BuiltStructuresPerCoordinates);
