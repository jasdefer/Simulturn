using Simulturn.AI.Analysis;
using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// The counter-composition successor of the <see cref="CommanderPlayer"/>.
/// It exploits the rock-paper-scissors combat design: the enemy composition revealed by
/// fights (a dying scout is a fight, so scouting buys composition intel) sharpens the
/// enemy estimate, and the army is trained toward the composition that maximizes the
/// simulated fight result instead of a uniform mix. An equally expensive counter-composed
/// army beats a uniform one, which also makes the attack margin reachable in matchups
/// that previously stalled.
/// </summary>
public class TacticianPlayer : CommanderPlayer
{
    private readonly TacticianPlayerOptions _tacticianOptions;

    public TacticianPlayer(TacticianPlayerOptions? options = null, string? name = null)
        : base(options ?? new TacticianPlayerOptions(), name ?? "Tactician")
    {
        _tacticianOptions = options ?? new TacticianPlayerOptions();
    }

    private protected override Army EstimatedEnemyArmy(TurnPlan turn)
    {
        return ScaleToRevealedComposition(base.EstimatedEnemyArmy(turn));
    }

    private protected override void BuildArmy(TurnPlan turn)
    {
        GameSettings gameSettings = turn.View.GameSettings;

        // Training buildings per fighter type. Unlike the commander, the count is global:
        // training buildings survive base migrations, so rebuilding them at every new main
        // base would burn matter that should become fighters.
        foreach (Unit fighter in _analysis!.Fighters)
        {
            Building building = GameSettings.TrainingBuildingPerUnit[fighter];
            int owned = turn.OwnCompoundHexagons.Sum(x => turn.CountOwnedOrPendingBuildings(x, building));
            if (owned < _options.TrainingBuildingsPerFighter &&
                turn.IdleWorkers(turn.MainBase) > 0 &&
                turn.Matter >= gameSettings.CompoundCost[building])
            {
                turn.Construct(turn.MainBase, building);
            }
        }

        // Train toward the composition that maximizes the fight result against the estimate.
        Army currentWithPending = turn.TotalFighterArmy;
        foreach (Unit fighter in _analysis.Fighters)
        {
            currentWithPending = currentWithPending.AddUnit(fighter, (short)turn.PendingUnitTrainings(fighter));
        }
        int trainableNow = MaxTrainableFighters(turn);
        int targetSize = currentWithPending.Total + trainableNow;
        if (targetSize <= 0)
        {
            return;
        }
        Army target = CompositionOptimizer.BestComposition(targetSize,
            EstimatedEnemyArmy(turn),
            gameSettings,
            _analysis,
            _tacticianOptions.CompositionHedge);
        foreach (Unit fighter in _analysis.Fighters
            .OrderByDescending(x => target[x] - currentWithPending[x])
            .ThenBy(x => x))
        {
            int deficit = target[fighter] - currentWithPending[fighter];
            foreach (Hexagon hexagon in turn.OwnCompoundHexagons)
            {
                if (deficit <= 0)
                {
                    break;
                }
                deficit -= turn.Train(hexagon, fighter, deficit);
            }
        }

        // The wanted composition may bottleneck on a single training building type;
        // keep the production saturated with whatever else can be built. The army drifts
        // toward the target composition over time without sacrificing production rate.
        foreach (Unit fighter in _analysis.Fighters
            .OrderByDescending(x => target[x] - currentWithPending[x])
            .ThenBy(x => x))
        {
            foreach (Hexagon hexagon in turn.OwnCompoundHexagons)
            {
                turn.Train(hexagon, fighter, int.MaxValue);
            }
        }
    }

    /// <summary>
    /// A rough upper bound of the fighters trainable this turn, limited by matter,
    /// space and the total number of training buildings.
    /// </summary>
    private int MaxTrainableFighters(TurnPlan turn)
    {
        GameSettings gameSettings = turn.View.GameSettings;
        int cheapestCost = _analysis!.Fighters.Min(x => (int)gameSettings.ArmyCost[x]);
        int smallestSpace = _analysis.Fighters.Min(x => (int)gameSettings.RequiredSpace[x]);
        int byMatter = cheapestCost > 0 ? turn.Matter / cheapestCost : int.MaxValue;
        int bySpace = smallestSpace > 0 ? turn.FreeSpace / smallestSpace : int.MaxValue;
        int byBuildings = turn.OwnCompoundHexagons
            .Sum(hexagon => _analysis.Fighters
                .Sum(fighter => (int)turn.View.PlayerState.Compounds.GetValueOrDefault(hexagon)[GameSettings.TrainingBuildingPerUnit[fighter]]));
        return Math.Max(0, Math.Min(byBuildings, Math.Min(byMatter, bySpace)));
    }
}
