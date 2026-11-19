namespace Simulturn.Core.Model.Report;

/// <summary>
/// The training a player ordered on one hexagon in a turn, with the price that was paid.
/// </summary>
/// <param name="Units">The ordered units.</param>
/// <param name="UnitPrice">The matter one unit of each type cost in this turn.</param>
public sealed record TrainingOrderReport(Army Units, Army UnitPrice)
{
    /// <summary>The matter paid for the order.</summary>
    public int Matter => Units * UnitPrice;
}

/// <summary>
/// The construction a player ordered on one hexagon in a turn, with the price that was paid.
/// </summary>
/// <param name="Buildings">The ordered buildings.</param>
/// <param name="UnitPrice">The matter one building of each type cost in this turn.</param>
public sealed record ConstructionOrderReport(Compound Buildings, Compound UnitPrice)
{
    /// <summary>The matter paid for the order.</summary>
    public int Matter => Buildings * UnitPrice;
}

/// <summary>
/// The research a player ordered on one hexagon in a turn: the level it buys and the matter paid for it.
/// </summary>
public sealed record ResearchOrderReport(Upgrade Upgrade, byte Level, int Matter);
