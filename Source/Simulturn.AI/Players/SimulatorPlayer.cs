using Simulturn.AI.Analysis;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// The successor of the <see cref="CommanderPlayer"/>. Same economy and modes, but the
/// military mode is chosen by look-ahead instead of rules: candidate modes are rolled out
/// for several turns on a believed game state (fog of war unknowns filled with estimates,
/// the opponent modeled as a commander) and the mode with the best evaluated outcome wins.
/// Fights reveal the enemy army composition, which sharpens the believed state.
/// </summary>
public class SimulatorPlayer : CommanderPlayer
{
    private readonly SimulatorPlayerOptions _simulatorOptions;

    public SimulatorPlayer(SimulatorPlayerOptions? options = null, string? name = null)
        : base(options ?? new SimulatorPlayerOptions(), name ?? "Simulator")
    {
        _simulatorOptions = options ?? new SimulatorPlayerOptions();
    }

    private protected override Army EstimatedEnemyArmy(TurnPlan turn)
    {
        // A fight reveals the true composition of the enemy army.
        return ScaleToRevealedComposition(base.EstimatedEnemyArmy(turn));
    }

    private protected override MilitaryMode SelectMilitaryMode(PlayerGameState view, TurnPlan probe)
    {
        MilitaryMode ruleMode = base.SelectMilitaryMode(view, probe);

        // The rules own the aggression: they are proven in the arena, and a fabricated
        // believed state must not talk the player into attacks. The look-ahead only serves
        // as a safety veto: it may pull an aggressive rule decision back to a defensive
        // mode when that is clearly better under every simulated enemy behavior.
        List<MilitaryMode> candidates = [];
        if (ruleMode is MilitaryMode.Attack or MilitaryMode.Raid or MilitaryMode.Intercept or MilitaryMode.Sweep)
        {
            candidates.Add(MilitaryMode.Guard);
            if (IntruderHexagon(probe) is not null)
            {
                candidates.Add(MilitaryMode.Defend);
            }
        }
        if (candidates.Count == 0)
        {
            return ruleMode;
        }

        // The true opponent behavior is unknown, so every candidate is rolled out against
        // two enemy models: a well playing commander and a passive enemy. The rules are
        // proven in the arena; a candidate only overrides them when it is clearly better
        // under BOTH models. This prevents the perfect-defense model from vetoing attacks
        // that win against weaker real opponents.
        GameState believed = BelievedState.Build(view, EstimatedEnemyArmy(probe), _analysis!);
        Func<IArtificialPlayer>[] enemyModels =
        [
            () => new CommanderPlayer(_simulatorOptions),
            () => new IdlePlayer()
        ];
        double[] ruleScores = enemyModels
            .Select(x => Rollout(believed, view.PlayerId, ruleMode, x))
            .ToArray();
        MilitaryMode best = ruleMode;
        double bestTotal = ruleScores.Sum();
        foreach (MilitaryMode candidate in candidates.Where(x => x != ruleMode))
        {
            double[] scores = enemyModels
                .Select(x => Rollout(believed, view.PlayerId, candidate, x))
                .ToArray();
            bool dominates = scores
                .Zip(ruleScores, (candidateScore, ruleScore) => candidateScore > ruleScore + _simulatorOptions.OverrideMargin)
                .All(x => x);
            if (dominates && scores.Sum() > bestTotal)
            {
                best = candidate;
                bestTotal = scores.Sum();
            }
        }
        return best;
    }

    /// <summary>
    /// Plays the believed state forward: we hold the candidate mode for the whole horizon,
    /// every opponent plays the given enemy model. Returns the evaluation of the final position.
    /// </summary>
    private double Rollout(GameState believed, string playerId, MilitaryMode candidate, Func<IArtificialPlayer> enemyModel)
    {
        Dictionary<string, IArtificialPlayer> policies = [];
        foreach (string id in believed.PlayerIds.OrderBy(x => x))
        {
            policies[id] = id == playerId
                ? new ForcedModeCommander(candidate, _simulatorOptions)
                : enemyModel();
        }
        GameState state = believed;
        for (int i = 0; i < _simulatorOptions.SimulationHorizon && !state.IsGameOver; i++)
        {
            Dictionary<string, IReadOnlyDictionary<Hexagon, Command>> commands = [];
            foreach ((string id, IArtificialPlayer policy) in policies)
            {
                commands[id] = policy.GetCommands(state.GetPlayerGameState(id));
            }
            state = state.NextTurn(commands);
        }
        return StateEvaluator.Evaluate(state, playerId);
    }

    /// <summary>
    /// A commander that always plays one fixed military mode, used to roll out a candidate.
    /// </summary>
    private sealed class ForcedModeCommander : CommanderPlayer
    {
        private readonly MilitaryMode _forcedMode;

        public ForcedModeCommander(MilitaryMode forcedMode, CommanderPlayerOptions options)
            : base(options, $"Forced {forcedMode}")
        {
            _forcedMode = forcedMode;
        }

        private protected override MilitaryMode SelectMilitaryMode(PlayerGameState view, TurnPlan probe)
        {
            return _forcedMode;
        }
    }
}
