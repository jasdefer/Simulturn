namespace Simulturn.AI.Evaluation;

public record ArenaOptions
{
    /// <summary>
    /// Games per pairing and seat assignment. Every pairing is played twice per game index,
    /// once with either player in either starting position, to remove any map bias.
    /// </summary>
    public int GamesPerSeatPairing { get; init; } = 10;

    public ushort MaxTurns { get; init; } = 200;

    public int Seed { get; init; } = 0;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}
