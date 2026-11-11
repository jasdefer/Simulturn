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
/// Collects the views of a player over the turns of a game and joins the current view with
/// the memories of the earlier ones (<see cref="FogOfWar.GetMemories"/>).
/// </summary>
internal sealed class Knowledge
{
    private readonly List<PlayerGameState> _views;

    /// <param name="earlierViews">
    /// The views of the turns before the first observed one, for a player that takes over
    /// a running game.
    /// </param>
    public Knowledge(IEnumerable<PlayerGameState>? earlierViews = null)
    {
        _views = [.. earlierViews ?? []];
    }

    /// <summary>
    /// The views observed so far, in ascending turn order.
    /// </summary>
    public IReadOnlyList<PlayerGameState> Views => _views;

    /// <summary>
    /// Adds the view of the current turn and returns what the player knows about every hexagon.
    /// </summary>
    public ImmutableDictionary<Hexagon, KnownHexagon> Observe(PlayerGameState view)
    {
        _views.Add(view);
        var memories = FogOfWar.GetMemories(_views);
        var known = ImmutableDictionary.CreateBuilder<Hexagon, KnownHexagon>();
        foreach ((Hexagon hexagon, HexagonObservation observation) in view.Observations)
        {
            StaticSighting? sighting = memories.GetValueOrDefault(hexagon)?.Static;
            bool isCurrent = observation.Visibility >= FogOfWar.StaticObservationThreshold;
            known[hexagon] = new KnownHexagon()
            {
                Visibility = observation.Visibility,
                LastSeenTurn = isCurrent ? view.Turn : sighting?.Turn,
                RemainingMatter = isCurrent ? observation.RemainingMatter : sighting?.RemainingMatter,
                OpponentCompounds = isCurrent
                    ? observation.OpponentCompounds
                    : sighting?.OpponentCompounds ?? ImmutableDictionary<string, Compound>.Empty,
                OpponentUnitCounts = observation.OpponentUnitCounts
            };
        }
        return known.ToImmutableDictionary();
    }
}
