namespace Simulturn.Bots.Catalog;

/// <summary>
/// A bot by name. <paramref name="Create"/> gives a fresh instance for one game on one seat;
/// the seed is all the randomness the bot may use, so the same seed gives the same play.
/// </summary>
public sealed record BotEntry(string Name, Func<int, IBot> Create);
