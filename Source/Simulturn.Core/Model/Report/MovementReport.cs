namespace Simulturn.Core.Model.Report;

/// <summary>
/// One army that moved from one hexagon to another.
/// </summary>
public sealed record MovementReport(Hexagon Origin, Hexagon Destination, Army Army);
