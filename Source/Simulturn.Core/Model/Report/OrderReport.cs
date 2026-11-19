namespace Simulturn.Core.Model.Report;

/// <summary>
/// A training a player ordered in a turn: the units of one type on one hexagon and the matter paid for them.
/// </summary>
public sealed record TrainingOrderReport(Hexagon Hexagon, Unit Unit, short Count, int Matter);

/// <summary>
/// A construction a player ordered in a turn: the buildings of one type on one hexagon and the matter paid for them.
/// </summary>
public sealed record ConstructionOrderReport(Hexagon Hexagon, Building Building, short Count, int Matter);

/// <summary>
/// A research a player ordered in a turn: the level it buys and the matter paid for it.
/// </summary>
public sealed record ResearchOrderReport(Hexagon Hexagon, Upgrade Upgrade, byte Level, int Matter);
