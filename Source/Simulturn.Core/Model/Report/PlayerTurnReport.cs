namespace Simulturn.Core.Model.Report;

/// <summary>
/// The part of a <see cref="TurnReport"/> a single player is allowed to know.
/// Built by <see cref="Engine.FogOfWar.GetPlayerTurnReport"/>.
/// </summary>
public sealed record PlayerTurnReport
{
    public static PlayerTurnReport Empty { get; } = new();

    /// <summary>
    /// The own losses of the turn.
    /// </summary>
    public ImmutableDictionary<Hexagon, Army> Losses { get; init; } = ImmutableDictionary<Hexagon, Army>.Empty;

    /// <summary>
    /// The armies of other players this player fought against in the turn, keyed by
    /// hexagon and opponent id. Fighting reveals the full composition of the participating
    /// armies (as they entered the fight, before losses).
    /// </summary>
    public ImmutableDictionary<Hexagon, ImmutableDictionary<string, Army>> RevealedArmies { get; init; } = ImmutableDictionary<Hexagon, ImmutableDictionary<string, Army>>.Empty;
}
