namespace Simulturn.Core.Model.Report;

/// <summary>
/// A completed research.
/// </summary>
/// <param name="Level">The level the player reached with it; 1 is the first level.</param>
public sealed record ResearchReport(Upgrade Upgrade, byte Level);
