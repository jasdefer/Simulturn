namespace Simulturn.Core.Model.Report;

/// <summary>
/// The research a player ordered on one hexagon in a turn: the level it buys and the matter paid for it.
/// </summary>
public sealed record ResearchOrderReport(Upgrade Upgrade, byte Level, int Matter);
