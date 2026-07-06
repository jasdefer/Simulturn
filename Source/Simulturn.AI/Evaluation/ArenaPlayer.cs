namespace Simulturn.AI.Evaluation;

/// <summary>
/// A named factory for arena participants. A fresh player instance is created per game,
/// parameterized with the seed of that game.
/// </summary>
public record ArenaPlayer
{
    public required string Name { get; init; }
    public required Func<int, IArtificialPlayer> CreatePlayer { get; init; }
}
