namespace Simulturn.AI.Evaluation;

/// <summary>
/// The aggregated outcome of all games between two arena players.
/// </summary>
public record PairingResult
{
    public required string PlayerA { get; init; }
    public required string PlayerB { get; init; }
    public required int WinsA { get; init; }
    public required int WinsB { get; init; }
    public required int Draws { get; init; }

    /// <summary>
    /// Games lost by returning invalid commands (already counted in the opponent's wins).
    /// </summary>
    public required int ForfeitsA { get; init; }
    public required int ForfeitsB { get; init; }

    public required double AverageTurns { get; init; }

    public int Games => WinsA + WinsB + Draws;

    public override string ToString()
    {
        return $"{PlayerA} vs {PlayerB}: {WinsA}-{WinsB} ({Draws} draws, avg {AverageTurns:F1} turns)" +
            (ForfeitsA + ForfeitsB > 0 ? $" [forfeits: {PlayerA} {ForfeitsA}, {PlayerB} {ForfeitsB}]" : "");
    }
}
