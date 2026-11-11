namespace Simulturn.Core.Model.Report;

/// <summary>
/// The matter a player harvested on one hexagon.
/// </summary>
/// <param name="Harvesters">The idle units that were counted, capped at the limit of the hexagon.</param>
/// <param name="Matter">The harvested matter, capped at what the hexagon had left.</param>
public sealed record IncomeReport(Army Harvesters, int Matter);
