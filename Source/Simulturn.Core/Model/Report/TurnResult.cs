using Simulturn.Core.Model.State;

namespace Simulturn.Core.Model.Report;

/// <summary>
/// The two outputs of resolving one turn. The state is the input of the next turn;
/// the report tells what happened and never goes back into the engine.
/// Both are all-knowing: a player only ever receives the cut made by <see cref="Engine.FogOfWar"/>.
/// </summary>
public sealed record TurnResult(GameState State, TurnReport Report);
