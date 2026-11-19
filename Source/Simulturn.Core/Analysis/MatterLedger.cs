namespace Simulturn.Core.Analysis;

/// <summary>
/// Where the matter of one player came from and what became of it, from the start of a game up
/// to one turn. Every value is matter. The ledger of the last turn is the ledger of the game,
/// the ledgers of all turns are its course; the difference of two is what happened in between.
/// <para>
/// It is a fact, not a judgement, and it knows everything: it is made after the game from the
/// full state, never from the view of a player.
/// </para>
/// </summary>
public sealed record MatterLedger
{
    /// <summary>The matter the player started with.</summary>
    public int StartMatter { get; init; }

    /// <summary>The price of the army and the buildings the player started with. Nobody paid it.</summary>
    public int StartStock { get; init; }

    /// <summary>The matter harvested so far.</summary>
    public int Harvested { get; init; }

    /// <summary>The matter paid for dots so far.</summary>
    public int SpentOnDots { get; init; }

    /// <summary>The matter paid for triangles, circles and squares so far.</summary>
    public int SpentOnFightingUnits { get; init; }

    /// <summary>The matter paid for buildings so far.</summary>
    public int SpentOnBuildings { get; init; }

    /// <summary>The matter paid for researches so far.</summary>
    public int SpentOnResearch { get; init; }

    /// <summary>The matter the player holds.</summary>
    public int OnHand { get; init; }

    /// <summary>The price of the dots in the army.</summary>
    public int Dots { get; init; }

    /// <summary>The price of the triangles, circles and squares in the army.</summary>
    public int FightingUnits { get; init; }

    /// <summary>The price of the standing buildings.</summary>
    public int Buildings { get; init; }

    /// <summary>The price of the completed researches. A start upgrade cost nothing and does not count.</summary>
    public int Research { get; init; }

    /// <summary>The price of the pending trainings, constructions and researches.</summary>
    public int Pending { get; init; }

    /// <summary>The price of the units lost in fights so far.</summary>
    public int Fallen { get; init; }

    /// <summary>The price of the buildings an opponent destroyed so far.</summary>
    public int Destroyed { get; init; }

    /// <summary>The price of the orders that were canceled so far. No cancellation returns matter.</summary>
    public int Canceled { get; init; }

    /// <summary>
    /// The price of the units other players lost in fights the player took part in. Not part of
    /// the balance: it is in the ledgers of the others as <see cref="Fallen"/>.
    /// </summary>
    public int Killed { get; init; }

    /// <summary>
    /// The price of the neutral units that fell in fights the player took part in. Not part of
    /// the balance, and in no other ledger: a neutral army has none.
    /// </summary>
    public int KilledNeutral { get; init; }

    /// <summary>
    /// The price of the buildings of others the player destroyed. Not part of the balance: it is
    /// in the ledgers of the others as <see cref="Destroyed"/>.
    /// </summary>
    public int Razed { get; init; }

    /// <summary>Everything the player ever had to spend.</summary>
    public int Income => StartMatter + Harvested;

    /// <summary>Everything the player paid for orders.</summary>
    public int Spent => SpentOnDots + SpentOnFightingUnits + SpentOnBuildings + SpentOnResearch;

    /// <summary>What the player still owns of the start stock and of what was paid.</summary>
    public int Stock => Dots + FightingUnits + Buildings + Research + Pending;

    /// <summary>What is gone of the start stock and of what was paid.</summary>
    public int Lost => Fallen + Destroyed + Canceled;

    /// <summary>
    /// The income that is neither on hand nor spent. Always zero.
    /// </summary>
    public int UnexplainedIncome => Income - OnHand - Spent;

    /// <summary>
    /// The start stock and the spending that are neither owned nor lost. Zero as long as the
    /// price of an order does not change during a game: a unit does not remember what was paid
    /// for it, so stock and losses count with the price of today.
    /// </summary>
    public int UnexplainedSpending => StartStock + Spent - Stock - Lost;
}
