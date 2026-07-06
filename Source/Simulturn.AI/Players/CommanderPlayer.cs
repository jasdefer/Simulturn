using Simulturn.AI.Analysis;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// The successor of the <see cref="StateMachinePlayer"/>. Same two track design,
/// with four additions measured in the arena:
/// a defend mode that rallies the army when enemies close in on own buildings,
/// a sweep mode that hunts for hidden enemy buildings instead of drawing at the turn limit,
/// a growth adjusted enemy estimate (the enemy keeps training while unseen), and
/// late game desperation that lowers the attack margin so long games still get decided.
/// Workers flee from hexagons where enemy units show up.
/// </summary>
public class CommanderPlayer : IArtificialPlayer
{
    public enum EconomyMode { Eco, Expand, BuildArmy }
    public enum MilitaryMode { Scout, Guard, Defend, Intercept, Attack, Raid, Sweep }

    private protected readonly CommanderPlayerOptions _options;
    private protected SettingsAnalysis? _analysis;
    private Hexagon? _expansionTarget;
    private int _lastObservedEnemyUnits = -1;
    private ushort _lastEnemyObservationTurn;

    public EconomyMode Economy { get; private set; } = EconomyMode.Eco;
    public MilitaryMode Military { get; private set; } = MilitaryMode.Scout;
    public string Name { get; }

    public CommanderPlayer(CommanderPlayerOptions? options = null, string? name = null)
    {
        _options = options ?? new CommanderPlayerOptions();
        Name = name ?? "Commander";
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        _analysis ??= GameSettingsAnalyzer.Analyze(playerGameState.GameSettings);
        UpdateEnemyEstimate(playerGameState);
        var probe = new TurnPlan(playerGameState, _analysis, _options);
        Economy = DecideEconomyMode(probe);
        Military = SelectMilitaryMode(playerGameState, probe);
        return BuildTurnCommands(playerGameState, Military);
    }

    /// <summary>
    /// Chooses the military mode. The base implementation applies the rules;
    /// derived players may evaluate candidates differently.
    /// </summary>
    private protected virtual MilitaryMode SelectMilitaryMode(PlayerGameState view, TurnPlan probe)
    {
        return DecideMilitaryMode(probe);
    }

    /// <summary>
    /// Produces the full command set of a turn for the given military mode.
    /// Deterministic for the same view and mode, so candidate command sets can be generated repeatedly.
    /// </summary>
    private protected IReadOnlyDictionary<Hexagon, Command> BuildTurnCommands(PlayerGameState view, MilitaryMode militaryMode)
    {
        var turn = new TurnPlan(view, _analysis!, _options);
        if (Economy == EconomyMode.Expand)
        {
            Expand(turn);
        }
        MigrateWorkers(turn);
        FleeWorkers(turn);
        TrainWorkers(turn);
        // The economy mode sets the priority, it does not gate army production:
        // once the initial worker economy stands, the army grows continuously.
        // Research before the army production: it is gated on banked matter, so it is
        // only active when the income outruns the army spending anyway.
        PlanResearch(turn);
        if (Economy != EconomyMode.Eco)
        {
            BuildArmy(turn);
        }
        EnsureSpace(turn);

        ExecuteMilitary(turn, militaryMode);
        if (militaryMode == MilitaryMode.Scout || IntelAge(turn) > _options.ScoutRefreshTurns)
        {
            SendScout(turn);
        }
        return turn.BuildCommands();
    }

    private void ExecuteMilitary(TurnPlan turn, MilitaryMode militaryMode)
    {
        switch (militaryMode)
        {
            case MilitaryMode.Defend:
                MarchFighters(turn, ThreatenedBase(turn) ?? turn.MainBase);
                break;
            case MilitaryMode.Intercept:
                MarchFighters(turn, IntruderHexagon(turn) ?? turn.MainBase);
                break;
            case MilitaryMode.Attack:
                AttackKnownEnemyCompounds(turn);
                break;
            case MilitaryMode.Raid:
                MarchFighters(turn, RaidTarget(turn) ?? turn.MainBase);
                break;
            case MilitaryMode.Sweep:
                Sweep(turn);
                break;
            default:
                MarchFighters(turn, turn.MainBase);
                break;
        }
    }

    private protected virtual void UpdateEnemyEstimate(PlayerGameState view)
    {
        if (_lastObservedEnemyUnits < 0)
        {
            _lastObservedEnemyUnits = view.GameSettings.HexagonSettings.Values
                .Where(x => x.PlayerInitialization is not null && x.PlayerInitialization.Value.StartingPlayerId != view.PlayerId)
                .Sum(x => x.PlayerInitialization!.Value.InitialArmy.Total);
            _lastEnemyObservationTurn = 0;
        }
        int observed = view.Observations.Values.Sum(x => x.OpponentUnitCounts.Values.Sum());
        if (observed > 0)
        {
            _lastObservedEnemyUnits = observed;
            _lastEnemyObservationTurn = view.Turn;
        }
        else if (view.Observations.Values.Any(x =>
            x.Visibility >= Visibility.Visible &&
            x.OpponentCompounds.Any(y => !y.Value.IsEmpty)))
        {
            // Watching known enemy buildings without seeing a single unit:
            // the enemy army is dead or elsewhere, so the estimate decays.
            _lastObservedEnemyUnits = Math.Max(0, _lastObservedEnemyUnits - 1);
            _lastEnemyObservationTurn = view.Turn;
        }
    }

    /// <summary>
    /// The estimated enemy army. The enemy keeps producing while unseen, so the total grows with
    /// the age of the observation. Observed counts include enemy workers, which fight linearly
    /// with their count when defending; assuming the enemy runs a similar economy, up to the own
    /// worker count is modeled as workers and the remainder as a uniform fighter mix.
    /// </summary>
    private protected virtual Army EstimatedEnemyArmy(TurnPlan turn)
    {
        int turnsSinceObservation = turn.View.Turn - _lastEnemyObservationTurn;
        int growth = Math.Min(_options.MaxAssumedEnemyGrowth, (int)(_options.EnemyGrowthPerTurn * turnsSinceObservation));
        int total = _lastObservedEnemyUnits + growth;
        int workers = Math.Min(total, turn.WorkerCount);
        return _analysis!.UniformFighterMix(total - workers).AddUnit(_analysis.Worker, (short)workers);
    }

    private EconomyMode DecideEconomyMode(TurnPlan turn)
    {
        if (turn.WorkerCount < turn.TargetWorkers && turn.WorkerCapacity > 0)
        {
            return EconomyMode.Eco;
        }
        if (turn.ActiveIncomeHexagons.Count < _options.MaxIncomeBases && FindExpansionTarget(turn) is not null)
        {
            return EconomyMode.Expand;
        }
        return EconomyMode.BuildArmy;
    }

    private protected MilitaryMode DecideMilitaryMode(TurnPlan turn)
    {
        if (ThreatenedBase(turn) is not null)
        {
            return MilitaryMode.Defend;
        }
        Army fighters = turn.TotalFighterArmy;
        Army exponent = turn.View.GameSettings.FightExponent;
        double margin = turn.View.Turn >= _options.LateGameTurn ? 1.0 : _options.AttackMargin;

        // Crush intruders in the own territory when clearly stronger.
        Hexagon? intruder = IntruderHexagon(turn);
        if (intruder is not null)
        {
            int intruderCount = turn.View.Observations
                .Where(x => IsIntruder(turn, x.Key))
                .Sum(x => x.Value.OpponentUnitCounts.Values.Sum());
            Army intruderEstimate = _analysis!.UniformFighterMix(intruderCount);
            if (fighters.GetStrengthOver(intruderEstimate, exponent) >
                margin * Math.Max(1, intruderEstimate.GetStrengthOver(fighters, exponent)))
            {
                return MilitaryMode.Intercept;
            }
        }

        if (NearestKnownEnemyCompound(turn) is null)
        {
            // No known enemy buildings: hunt for them once an army exists, otherwise scout.
            return fighters.Total >= _options.MinAttackArmy ? MilitaryMode.Sweep : MilitaryMode.Scout;
        }
        // The map is mined out and no more units can be afforded: waiting has no value,
        // so attack with everything that is left instead of drawing at the turn limit.
        int cheapestFighterCost = _analysis!.Fighters.Min(x => turn.View.GameSettings.ArmyCost[x]);
        bool economyExhausted = turn.ActiveIncomeHexagons.Count == 0 && turn.Matter < cheapestFighterCost;
        if (economyExhausted && fighters.Total >= _options.MinAttackArmy / 2)
        {
            return MilitaryMode.Attack;
        }

        if (fighters.Total < _options.MinAttackArmy)
        {
            return MilitaryMode.Guard;
        }
        Army enemyEstimate = EstimatedEnemyArmy(turn);
        double ownStrength = fighters.GetStrengthOver(enemyEstimate, exponent);
        double enemyStrength = enemyEstimate.GetStrengthOver(fighters, exponent);
        if (ownStrength > margin * Math.Max(1, enemyStrength))
        {
            return MilitaryMode.Attack;
        }
        // Not strong enough for the main base: raze outlying enemy expansions instead of waiting.
        if (RaidTarget(turn) is not null)
        {
            return MilitaryMode.Raid;
        }
        return MilitaryMode.Guard;
    }

    /// <summary>
    /// A hexagon is an intruder position when visible enemy units stand close to own buildings.
    /// </summary>
    private bool IsIntruder(TurnPlan turn, Hexagon hexagon)
    {
        return turn.View.Observations[hexagon].OpponentUnitCounts.Values.Sum() >= 3 &&
            turn.OwnCompoundHexagons.Any(x => x.DistanceTo(hexagon) <= 3);
    }

    private protected Hexagon? IntruderHexagon(TurnPlan turn)
    {
        return turn.View.Observations.Keys
            .Where(x => IsIntruder(turn, x))
            .OrderByDescending(x => turn.View.Observations[x].OpponentUnitCounts.Values.Sum())
            .ThenBy(x => x.DistanceTo(turn.MainBase))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .Cast<Hexagon?>()
            .FirstOrDefault();
    }

    /// <summary>
    /// An outlying known enemy building hexagon, never the biggest known enemy base.
    /// </summary>
    private protected Hexagon? RaidTarget(TurnPlan turn)
    {
        var known = turn.View.Observations
            .Where(x => x.Value.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Select(x => (Hexagon: x.Key, Buildings: x.Value.OpponentCompounds.Values.Sum(y => y.Sum())))
            .ToList();
        if (known.Count < 2)
        {
            return null;
        }
        Hexagon enemyMain = known
            .OrderByDescending(x => x.Buildings)
            .ThenBy(x => x.Hexagon.X).ThenBy(x => x.Hexagon.Y)
            .First().Hexagon;
        return known
            .Where(x => x.Hexagon != enemyMain)
            .OrderBy(x => x.Hexagon.DistanceTo(turn.MainBase))
            .ThenBy(x => x.Hexagon.X).ThenBy(x => x.Hexagon.Y)
            .Select(x => (Hexagon?)x.Hexagon)
            .FirstOrDefault();
    }

    /// <summary>
    /// The main base if visible enemy units are close to it, otherwise null.
    /// Border expansions are not defended: their workers flee and the buildings are
    /// cheap compared to feeding the army piecewise into enemy stacks.
    /// </summary>
    private Hexagon? ThreatenedBase(TurnPlan turn)
    {
        bool threatened = turn.View.Observations.Any(x =>
            x.Value.OpponentUnitCounts.Values.Sum() > 0 &&
            x.Key.DistanceTo(turn.MainBase) <= _options.DefenseRadius);
        return threatened ? turn.MainBase : null;
    }

    /// <summary>
    /// Spreads the fighter stacks over the least recently seen hexagons to find hidden enemy buildings.
    /// </summary>
    private void Sweep(TurnPlan turn)
    {
        List<Hexagon> staleHexagons = turn.View.Observations
            .OrderBy(x => (int?)x.Value.LastSeenTurn ?? -1)
            .ThenBy(x => x.Key.X).ThenBy(x => x.Key.Y)
            .Select(x => x.Key)
            .Take(6)
            .ToList();
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            Army fighters = turn.MovableFighters(hexagon);
            if (fighters.IsEmpty)
            {
                continue;
            }
            Hexagon target = staleHexagons
                .OrderBy(x => x.DistanceTo(hexagon))
                .ThenBy(x => x.X).ThenBy(x => x.Y)
                .First();
            if (hexagon == target)
            {
                staleHexagons.Remove(target); // this spot is refreshed; send the next stack elsewhere
                continue;
            }
            turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, target), fighters);
            staleHexagons.Remove(target);
            if (staleHexagons.Count == 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Idle workers run home when enemy units stand on their hexagon.
    /// </summary>
    private void FleeWorkers(TurnPlan turn)
    {
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            if (hexagon == turn.MainBase ||
                turn.View.Observations[hexagon].OpponentUnitCounts.Values.Sum() == 0)
            {
                continue;
            }
            int idle = turn.IdleWorkers(hexagon);
            if (idle > 0)
            {
                turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, turn.MainBase), Army.FromUnit(_analysis!.Worker, (short)idle));
            }
        }
    }

    private void Expand(TurnPlan turn)
    {
        _expansionTarget = FindExpansionTarget(turn);
        if (_expansionTarget is null)
        {
            return;
        }
        Hexagon target = _expansionTarget.Value;
        GameSettings gameSettings = turn.View.GameSettings;
        short planeCost = gameSettings.CompoundCost[_analysis!.IncomeBuilding];

        if (turn.IdleWorkers(target) > 0 && turn.Matter >= planeCost &&
            turn.CountOwnedOrPendingBuildings(target, _analysis.IncomeBuilding) == 0)
        {
            turn.Construct(target, _analysis.IncomeBuilding);
            return;
        }

        turn.Reserve(planeCost);
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            if (hexagon == target || hexagon == turn.MainBase || turn.IncomeHexagons.Contains(hexagon))
            {
                continue;
            }
            int idle = turn.IdleWorkers(hexagon);
            if (idle > 0)
            {
                turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, target), Army.FromUnit(_analysis.Worker, (short)idle));
            }
        }
        int underway = turn.OwnArmyHexagons
            .Where(x => x != turn.MainBase && !turn.IncomeHexagons.Contains(x))
            .Sum(x => (int)turn.View.PlayerState.Armies.GetValueOrDefault(x)[_analysis.Worker]);
        int partySize = Math.Min(_options.ExpansionWorkers - underway, turn.IdleWorkers(turn.MainBase) - 1);
        if (partySize > 0)
        {
            turn.Move(turn.MainBase, Navigation.StepToward(turn.View, turn.MainBase, target), Army.FromUnit(_analysis.Worker, (short)partySize));
        }
    }

    private void MigrateWorkers(TurnPlan turn)
    {
        if (turn.ActiveIncomeHexagons.Count == 0)
        {
            return;
        }
        foreach (Hexagon hexagon in turn.IncomeHexagons.Except(turn.ActiveIncomeHexagons))
        {
            int idle = turn.IdleWorkers(hexagon);
            if (idle <= 0)
            {
                continue;
            }
            Hexagon destination = turn.ActiveIncomeHexagons
                .OrderBy(x => x.DistanceTo(hexagon))
                .ThenBy(x => x.X).ThenBy(x => x.Y)
                .First();
            turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, destination), Army.FromUnit(_analysis!.Worker, (short)idle));
        }
    }

    private void TrainWorkers(TurnPlan turn)
    {
        int wanted = turn.TargetWorkers - turn.WorkerCount - turn.PendingUnitTrainings(_analysis!.Worker);
        foreach (Hexagon hexagon in turn.ActiveIncomeHexagons)
        {
            if (wanted <= 0)
            {
                break;
            }
            wanted -= turn.Train(hexagon, _analysis.Worker, wanted);
        }
    }

    private void BuildArmy(TurnPlan turn)
    {
        GameSettings gameSettings = turn.View.GameSettings;
        foreach (Unit fighter in _analysis!.Fighters)
        {
            Building building = GameSettings.TrainingBuildingPerUnit[fighter];
            if (turn.CountOwnedOrPendingBuildings(turn.MainBase, building) < _options.TrainingBuildingsPerFighter &&
                turn.IdleWorkers(turn.MainBase) > 0 &&
                turn.Matter >= gameSettings.CompoundCost[building])
            {
                turn.Construct(turn.MainBase, building);
            }
        }
        Army fighters = turn.TotalFighterArmy;
        foreach (Unit fighter in _analysis.Fighters
            .OrderBy(x => fighters[x] + turn.PendingUnitTrainings(x))
            .ThenBy(x => x))
        {
            foreach (Hexagon hexagon in turn.OwnCompoundHexagons)
            {
                turn.Train(hexagon, fighter, int.MaxValue);
            }
        }
    }

    /// <summary>
    /// Keeps a space buffer so unit production is never blocked, building multiple
    /// space buildings per turn if necessary.
    /// </summary>
    private void EnsureSpace(TurnPlan turn)
    {
        int spacePerBuilding = turn.View.GameSettings.ProvidedSpace[_analysis!.SpaceBuilding];
        if (spacePerBuilding <= 0)
        {
            return;
        }
        for (int planned = 0; planned < 3; planned++)
        {
            if (turn.FreeSpace + turn.PendingSpace + planned * spacePerBuilding >= _options.SpaceBuffer)
            {
                break;
            }
            int matterBefore = turn.Matter;
            turn.Construct(turn.MainBase, _analysis.SpaceBuilding);
            if (turn.Matter == matterBefore)
            {
                break; // no matter or no idle worker left
            }
        }
    }

    /// <summary>
    /// Every fighter stack razes the known enemy building hexagon nearest to it,
    /// so scattered enemy bases are destroyed in parallel instead of one by one.
    /// </summary>
    private void AttackKnownEnemyCompounds(TurnPlan turn)
    {
        List<Hexagon> targets = turn.View.Observations
            .Where(x => x.Value.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Select(x => x.Key)
            .ToList();
        if (targets.Count == 0)
        {
            MarchFighters(turn, turn.MainBase);
            return;
        }
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            Army fighters = turn.MovableFighters(hexagon);
            if (fighters.IsEmpty)
            {
                continue;
            }
            Hexagon target = targets
                .OrderBy(x => x.DistanceTo(hexagon))
                .ThenBy(x => x.X).ThenBy(x => x.Y)
                .First();
            if (hexagon == target)
            {
                continue; // stay and raze
            }
            turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, target), fighters);
        }
    }

    /// <summary>
    /// Researches upgrades with banked matter. If no own dot stands on a researchable
    /// hexagon yet, a single worker walks over (only while the economy is stable).
    /// </summary>
    private void PlanResearch(TurnPlan turn)
    {
        if (turn.View.PlayerState.Matter < _options.ResearchMatterSurplus)
        {
            return;
        }
        GameSettings gameSettings = turn.View.GameSettings;
        PlayerState playerState = turn.View.PlayerState;
        HashSet<Upgrade> pendingUpgrades = playerState.Researches
            .Where(x => x.Key >= turn.View.Turn)
            .SelectMany(x => x.Value.Values)
            .ToHashSet();
        var researchHexagons = gameSettings.HexagonSettings
            .Where(x => !x.Value.ResearchableUpgrades.IsEmpty)
            .Select(x => x.Key)
            .OrderBy(x => x.DistanceTo(turn.MainBase))
            .ThenBy(x => x.X).ThenBy(x => x.Y);
        foreach (Hexagon hexagon in researchHexagons)
        {
            foreach (Upgrade upgrade in gameSettings.HexagonSettings[hexagon].ResearchableUpgrades.Distinct().OrderBy(x => x))
            {
                byte currentLevel = playerState.UpgradeLevels.GetValueOrDefault(upgrade);
                int availableLevels = gameSettings.HexagonSettings[hexagon].ResearchableUpgrades.Count(x => x == upgrade);
                if (pendingUpgrades.Contains(upgrade) ||
                    currentLevel >= availableLevels ||
                    currentLevel >= gameSettings.Upgrades[upgrade].Length)
                {
                    continue;
                }
                if (turn.Research(hexagon, upgrade))
                {
                    return;
                }
                // No dot on the hexagon yet: send a worker over, but only while not expanding
                // (the expansion logic would redirect wandering workers anyway) and only to
                // hexagons without visible enemies.
                if (Economy == EconomyMode.BuildArmy &&
                    turn.View.Observations[hexagon].OpponentUnitCounts.Values.Sum() == 0 &&
                    turn.IdleWorkers(turn.MainBase) > 1)
                {
                    turn.Move(turn.MainBase, Navigation.StepToward(turn.View, turn.MainBase, hexagon), Army.FromUnit(_analysis!.Worker, 1));
                    return;
                }
            }
        }
    }

    private void MarchFighters(TurnPlan turn, Hexagon target)
    {
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            if (hexagon == target)
            {
                continue;
            }
            Army fighters = turn.MovableFighters(hexagon);
            if (fighters.IsEmpty)
            {
                continue;
            }
            turn.Move(hexagon, Navigation.StepToward(turn.View, hexagon, target), fighters);
        }
    }

    private void SendScout(TurnPlan turn)
    {
        Hexagon target = NearestKnownEnemyCompound(turn) ?? NearestEnemyStart(turn);
        Hexagon? origin = null;
        int bestDistance = int.MaxValue;
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            if (hexagon == target)
            {
                return;
            }
            bool hasSpareScout = _analysis!.Scout == _analysis.Worker
                ? turn.IdleWorkers(hexagon) > (hexagon == turn.MainBase ? 1 : 0)
                : turn.MovableArmy(hexagon)[_analysis.Scout] > 0;
            int distance = hexagon.DistanceTo(target);
            if (hasSpareScout && distance < bestDistance)
            {
                bestDistance = distance;
                origin = hexagon;
            }
        }
        if (origin is not null)
        {
            turn.Move(origin.Value, Navigation.StepToward(turn.View, origin.Value, target), Army.FromUnit(_analysis!.Scout, 1));
        }
    }

    private Hexagon? FindExpansionTarget(TurnPlan turn)
    {
        if (_expansionTarget is not null &&
            IsExpansionCandidate(turn, _expansionTarget.Value))
        {
            return _expansionTarget;
        }
        // The richest hexagon first (contested wealth must be fought for), safety second:
        // among equally rich spots, prefer the ones away from the enemy.
        List<Hexagon> knownEnemyCompounds = turn.View.Observations
            .Where(x => x.Value.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Select(x => x.Key)
            .ToList();
        var candidates = turn.View.Observations.Keys
            .Where(x => IsExpansionCandidate(turn, x))
            .OrderByDescending(x => turn.View.Observations[x].RemainingMatter ?? turn.View.GameSettings.HexagonSettings[x].Matter)
            .ThenByDescending(x => knownEnemyCompounds.Count == 0 ? 0 : knownEnemyCompounds.Min(y => y.DistanceTo(x)))
            .ThenBy(x => x.DistanceTo(turn.MainBase))
            .ThenBy(x => x.X).ThenBy(x => x.Y);
        return candidates.Cast<Hexagon?>().FirstOrDefault();
    }

    private bool IsExpansionCandidate(TurnPlan turn, Hexagon hexagon)
    {
        GameSettings gameSettings = turn.View.GameSettings;
        HexagonObservation observation = turn.View.Observations[hexagon];
        int expectedMatter = observation.RemainingMatter ?? gameSettings.HexagonSettings[hexagon].Matter;
        return gameSettings.HexagonSettings[hexagon].IsBuildable &&
            gameSettings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter[_analysis!.Worker] > 0 &&
            expectedMatter >= _options.MinExpansionMatter &&
            turn.View.PlayerState.Compounds.GetValueOrDefault(hexagon).IsEmpty &&
            observation.OpponentCompounds.All(x => x.Value.IsEmpty) &&
            observation.OpponentUnitCounts.Values.Sum() == 0;
    }

    private protected Hexagon? NearestKnownEnemyCompound(TurnPlan turn)
    {
        return turn.View.Observations
            .Where(x => x.Value.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Select(x => x.Key)
            .OrderBy(x => x.DistanceTo(turn.MainBase))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .Cast<Hexagon?>()
            .FirstOrDefault();
    }

    private Hexagon NearestEnemyStart(TurnPlan turn)
    {
        return turn.View.GameSettings.HexagonSettings
            .Where(x => x.Value.PlayerInitialization is not null &&
                x.Value.PlayerInitialization.Value.StartingPlayerId != turn.View.PlayerId)
            .Select(x => x.Key)
            .OrderBy(x => x.DistanceTo(turn.MainBase))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .First();
    }

    private int IntelAge(TurnPlan turn)
    {
        ushort? lastSeen = turn.View.Observations.Values
            .Where(x => x.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Max(x => x.LastSeenTurn);
        return lastSeen is null ? int.MaxValue : turn.View.Turn - lastSeen.Value;
    }
}
