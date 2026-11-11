using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

namespace Simulturn.AI.Players;

/// <summary>
/// What a player knows about a hexagon: the current view, completed by the last sighting
/// where the view shows nothing. The remembered values can be outdated.
/// </summary>
internal sealed record KnownHexagon
{
    public required Visibility Visibility { get; init; }

    /// <summary>
    /// The turn the remaining matter and the buildings were last seen, the current turn while
    /// the hexagon is at least partially visible, or null if it was never seen.
    /// </summary>
    public required ushort? LastSeenTurn { get; init; }

    /// <summary>
    /// The remaining matter as of <see cref="LastSeenTurn"/>, or null if the hexagon was never seen.
    /// </summary>
    public required int? RemainingMatter { get; init; }

    /// <summary>
    /// The buildings of other players as of <see cref="LastSeenTurn"/>, keyed by player id.
    /// </summary>
    public required ImmutableDictionary<string, Compound> OpponentCompounds { get; init; }

    /// <summary>
    /// The units of other players the current view shows, keyed by player id. Not remembered.
    /// </summary>
    public required ImmutableDictionary<string, int> OpponentUnitCounts { get; init; }
}

/// <summary>
/// Carries what a player knows from turn to turn: every view replaces what it shows and
/// leaves the rest as it was last seen.
/// </summary>
/// <remarks>
/// The same result follows from <see cref="FogOfWar.GetMemories"/> over all views so far.
/// The players carry it forward instead, because the look-ahead asks for it thousands of
/// times per game and going back through the views every time is too slow for that.
/// </remarks>
internal sealed class Knowledge
{
    private ImmutableDictionary<Hexagon, KnownHexagon> _known;

    /// <param name="earlier">
    /// What is known from the turns before the first observed one, for a player that takes
    /// over a running game.
    /// </param>
    public Knowledge(ImmutableDictionary<Hexagon, KnownHexagon>? earlier = null)
    {
        _known = earlier ?? ImmutableDictionary<Hexagon, KnownHexagon>.Empty;
    }

    /// <summary>
    /// Takes in the view of the current turn and returns what the player knows about every hexagon.
    /// </summary>
    public ImmutableDictionary<Hexagon, KnownHexagon> Observe(PlayerGameState view)
    {
        var known = ImmutableDictionary.CreateBuilder<Hexagon, KnownHexagon>();
        foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
        {
            KnownHexagon? earlier = _known.GetValueOrDefault(hexagon);
            bool isCurrent = observation.Visibility >= FogOfWar.StaticObservationThreshold;
            known[hexagon] = new KnownHexagon()
            {
                Visibility = observation.Visibility,
                LastSeenTurn = isCurrent ? view.Turn : earlier?.LastSeenTurn,
                RemainingMatter = isCurrent ? observation.RemainingMatter : earlier?.RemainingMatter,
                OpponentCompounds = isCurrent
                    ? observation.OpponentCompounds
                    : earlier?.OpponentCompounds ?? ImmutableDictionary<string, Compound>.Empty,
                OpponentUnitCounts = observation.OpponentUnitCounts
            };
        }
        _known = known.ToImmutableDictionary();
        return _known;
    }
}
