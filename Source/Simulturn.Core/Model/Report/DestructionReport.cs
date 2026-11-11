namespace Simulturn.Core.Model.Report;

/// <summary>
/// The buildings of one player that the army of another player destroyed on one hexagon.
/// </summary>
public sealed record DestructionReport(Hexagon Hexagon, string AttackerId, string OwnerId, Compound Buildings);
