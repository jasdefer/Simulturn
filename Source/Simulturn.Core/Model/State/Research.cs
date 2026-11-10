namespace Simulturn.Core.Model.State;

/// <summary>
/// A pending research: the upgrade and the turn in which it completes. Keyed by hexagon in
/// <see cref="PlayerState.Researches"/>, because a hexagon hosts at most one research at a time.
/// </summary>
public readonly record struct Research(Upgrade Upgrade, ushort CompletionTurn);
