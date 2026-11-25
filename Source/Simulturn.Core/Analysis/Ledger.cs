using Simulturn.Core.Engine;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Analysis;

/// <summary>
/// Keeps the <see cref="MatterLedger"/> of every player over a game: <see cref="Start"/> with the
/// first state, <see cref="After"/> with every resolved turn. What a player owns is read from the
/// state, what happened from the report; the prices come from <see cref="Costs"/>.
/// </summary>
public static class Ledger
{
    /// <summary>
    /// The ledgers of a game that starts with the state, keyed by player id.
    /// </summary>
    public static ImmutableDictionary<string, MatterLedger> Start(GameState state)
    {
        return state.PlayerStates.ToImmutableDictionary(
            x => x.Key,
            x =>
            {
                MatterLedger stock = WithStock(new MatterLedger(), state.GameSettings, x.Key, x.Value);
                return stock with { StartMatter = stock.OnHand, StartStock = stock.Stock };
            });
    }

    /// <summary>
    /// The ledgers after a turn, from the ledgers before it and the result of the turn.
    /// </summary>
    public static ImmutableDictionary<string, MatterLedger> After(IReadOnlyDictionary<string, MatterLedger> before, TurnResult turn)
    {
        GameSettings settings = turn.State.GameSettings;
        return turn.State.PlayerStates.ToImmutableDictionary(
            x => x.Key,
            x => WithStock(WithEvents(before[x.Key], settings, x.Key, x.Value, turn.Report), settings, x.Key, x.Value));
    }

    /// <summary>
    /// What the player owns in the state.
    /// </summary>
    private static MatterLedger WithStock(MatterLedger ledger, GameSettings settings, string playerId, PlayerState player)
    {
        Army army = player.Armies.Values.Sum();
        int pending = Costs.Training(settings, player.Trainings.Values.Sum()).Matter
            + Costs.Construction(settings, player.Constructions.Values.Sum()).Matter
            + player.Researches.Values.Sum(research => ResearchPrice(settings, player, research.Upgrade));

        int research = 0;
        ImmutableDictionary<Upgrade, byte> startLevels = UpgradeRules.StartLevels(settings, playerId);
        foreach ((Upgrade upgrade, byte level) in player.UpgradeLevels)
        {
            for (int completed = startLevels.GetValueOrDefault(upgrade); completed < level; completed++)
            {
                research += settings.NextUpgradeLevel(upgrade, completed)?.Cost ?? 0;
            }
        }

        return ledger with
        {
            OnHand = player.Matter,
            Dots = Costs.Training(settings, Dots(army)).Matter,
            FightingUnits = Costs.Training(settings, army - Dots(army)).Matter,
            Buildings = Costs.Construction(settings, player.Compounds.Values.Sum()).Matter,
            Research = research,
            Pending = pending
        };
    }

    /// <summary>
    /// What happened to the player in the turn, added to what happened before.
    /// </summary>
    private static MatterLedger WithEvents(MatterLedger ledger, GameSettings settings, string playerId, PlayerState player, TurnReport report)
    {
        PlayerActivityReport activity = report.Activities[playerId];

        int fallen = 0;
        int killed = 0;
        int killedNeutral = 0;
        foreach (FightReport fight in report.Fights.Values)
        {
            if (!fight.Losses.ContainsKey(playerId))
            {
                continue;
            }
            foreach ((string fighterId, Army loss) in fight.Losses)
            {
                int price = Costs.Training(settings, loss).Matter;
                if (fighterId == playerId)
                {
                    fallen += price;
                }
                else if (fighterId == Neutral.Id)
                {
                    killedNeutral += price;
                }
                else
                {
                    killed += price;
                }
            }
        }

        int destroyed = 0;
        int razed = 0;
        foreach (DestructionReport destruction in report.Destructions)
        {
            int price = Costs.Construction(settings, destruction.Buildings).Matter;
            if (destruction.OwnerId == playerId)
            {
                destroyed += price;
            }
            if (destruction.AttackerId == playerId)
            {
                razed += price;
            }
        }

        // A canceled research was still pending, so its level is the one after the completed ones.
        int canceled = Costs.Training(settings, activity.CanceledTrainings.Values.Sum()).Matter
            + Costs.Construction(settings, activity.CanceledConstructions.Values.Sum()).Matter
            + activity.CanceledResearches.Values.Sum(upgrade => ResearchPrice(settings, player, upgrade));

        Army ordered = activity.TrainingOrders.Values.Sum();

        return ledger with
        {
            Harvested = ledger.Harvested + activity.Incomes.Values.Sum(x => x.Matter),
            SpentOnDots = ledger.SpentOnDots + Dots(ordered) * activity.TrainingPrices,
            SpentOnFightingUnits = ledger.SpentOnFightingUnits + (ordered - Dots(ordered)) * activity.TrainingPrices,
            SpentOnBuildings = ledger.SpentOnBuildings + activity.ConstructionOrders.Values.Sum() * activity.ConstructionPrices,
            SpentOnResearch = ledger.SpentOnResearch + activity.ResearchOrders.Values.Sum(x => x.Matter),
            Fallen = ledger.Fallen + fallen,
            Destroyed = ledger.Destroyed + destroyed,
            Canceled = ledger.Canceled + canceled,
            Killed = ledger.Killed + killed,
            KilledNeutral = ledger.KilledNeutral + killedNeutral,
            Razed = ledger.Razed + razed
        };
    }

    private static Army Dots(Army army)
    {
        return Army.FromUnit(Unit.Dot, army.Dot);
    }

    /// <summary>
    /// The price of the level a pending research of the player buys.
    /// </summary>
    private static int ResearchPrice(GameSettings settings, PlayerState player, Upgrade upgrade)
    {
        return Costs.Research(settings, player.UpgradeLevels, upgrade).Matter;
    }
}
