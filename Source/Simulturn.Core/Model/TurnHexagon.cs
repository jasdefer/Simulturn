namespace Simulturn.Core.Model;

/// <summary>
/// A hexagon in a turn. Key of everything that is bound to both, for example the pending
/// trainings and constructions in <see cref="State.PlayerState"/>, where the turn is the completion turn.
/// </summary>
public readonly record struct TurnHexagon(ushort Turn, Hexagon Hexagon);
