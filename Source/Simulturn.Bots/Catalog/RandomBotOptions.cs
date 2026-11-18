namespace Simulturn.Bots.Catalog;

/// <summary>
/// The dice of the <see cref="RandomBot"/>. A variant of the bot is another set of these values
/// and its own line in the <see cref="BotCatalog"/>.
/// </summary>
public sealed record RandomBotOptions
{
    /// <summary>
    /// The chance per turn that a spare dot leaves for a hexagon worth claiming, while there is one.
    /// </summary>
    public double ExpeditionProbability { get; init; } = 0.25;

    /// <summary>
    /// How many times its price a dot must be able to harvest from what its hexagon has left
    /// before it is trained to harvest there.
    /// </summary>
    public int DotPayback { get; init; } = 4;

    /// <summary>
    /// The fewest turns an army guards a hexagon before it leaves for the next one.
    /// </summary>
    public int MinGuardTurns { get; init; } = 2;

    /// <summary>
    /// The most turns an army guards a hexagon before it leaves for the next one.
    /// </summary>
    public int MaxGuardTurns { get; init; } = 8;

    /// <summary>
    /// How much more likely than a free hexagon an army picks a hexagon with own buildings to guard.
    /// </summary>
    public double OwnHexagonWeight { get; init; } = 3;

    /// <summary>
    /// How much more likely than a free hexagon an army picks a hexagon on which it remembers
    /// buildings of an opponent.
    /// </summary>
    public double OpponentHexagonWeight { get; init; } = 6;
}
