namespace Simulturn.Bots.Catalog;

/// <summary>
/// All bots there are. A new bot is a class and a line here; a variant of a bot with other
/// parameters is another line with its own name.
/// </summary>
public static class BotCatalog
{
    public static ImmutableArray<BotEntry> All { get; } =
    [
        new BotEntry("idle", _ => new IdleBot())
    ];
}
