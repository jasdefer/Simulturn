using Simulturn.Core.Model.Report;

namespace Simulturn.Core.Model.State;

/// <summary>
/// Everything a single player is allowed to know about the game.
/// Clients and AI players must only receive this view, never the full <see cref="GameState"/>.
/// </summary>
public sealed record PlayerGameState
{
    public required string PlayerId { get; init; }
    public required ushort Turn { get; init; }
    public required GameSettings GameSettings { get; init; }

    /// <summary>
    /// The ids of all players in the game, including the own player id.
    /// </summary>
    public required ImmutableHashSet<string> PlayerIds { get; init; }

    /// <summary>
    /// The full own state of the player, including visibilities and memories.
    /// </summary>
    public required PlayerState PlayerState { get; init; }

    /// <summary>
    /// The knowledge of the player about every hexagon of the map.
    /// </summary>
    public required ImmutableDictionary<Hexagon, HexagonObservation> Observations { get; init; }

    /// <summary>
    /// What the player is allowed to know about the resolution of the previous turn, the one
    /// that led to this state. Empty at the start of the game.
    /// </summary>
    public required PlayerTurnReport Report { get; init; }

    public required bool IsGameOver { get; init; }
}
