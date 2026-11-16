using Simulturn.Bots;

namespace Simulturn.Lab.Arena;

/// <summary>
/// What an arena run plays. The same options with the same bots and maps give the same result.
/// </summary>
public sealed record ArenaOptions
{
    /// <summary>
    /// The names of the bots. Every two of them play against each other; a name listed twice
    /// plays against itself.
    /// </summary>
    public ImmutableArray<string> Bots { get; init; } = [];

    /// <summary>
    /// The names of the map types, see <see cref="ArenaMaps"/>.
    /// </summary>
    public ImmutableArray<string> Maps { get; init; } = [];

    /// <summary>
    /// The number of maps per map type, built with the seeds 1 to this number. The engine does
    /// not roll dice, so the spread of a run comes from the maps and from bots that roll dice.
    /// </summary>
    public int MapSeeds { get; init; } = 1;

    public MatchLimits Limits { get; init; } = new();
}
