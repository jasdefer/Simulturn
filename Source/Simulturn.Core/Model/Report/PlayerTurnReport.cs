namespace Simulturn.Core.Model.Report;

/// <summary>
/// The part of a <see cref="TurnReport"/> a single player is allowed to know.
/// Built by <see cref="Engine.FogOfWar.GetPlayerTurnReport"/>.
/// </summary>
public sealed record PlayerTurnReport
{
    public static PlayerTurnReport Empty { get; } = new();

    /// <summary>
    /// What happened to the player alone.
    /// </summary>
    public PlayerActivityReport Activity { get; init; } = PlayerActivityReport.Empty;

    /// <summary>
    /// The fights the player took part in, complete: whoever fought saw the armies and the
    /// losses of everyone on the hexagon, whatever the outcome.
    /// </summary>
    public ImmutableDictionary<Hexagon, FightReport> Fights { get; init; } = ImmutableDictionary<Hexagon, FightReport>.Empty;

    /// <summary>
    /// The destroyed buildings the player lost or destroyed.
    /// </summary>
    public ImmutableArray<DestructionReport> Destructions { get; init; } = [];
}
