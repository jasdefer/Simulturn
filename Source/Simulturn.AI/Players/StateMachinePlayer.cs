using Simulturn.AI.Analysis;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// A rule based player with two mode tracks:
/// an economy track (Eco, Expand, BuildArmy) that decides how matter is spent, and a
/// military track (Scout, Guard, Attack) that decides how armies move.
/// Attacks only happen when enemy buildings have been scouted and a simulated fight
/// against the estimated enemy army (unit counts are known, the composition is assumed
/// to be a uniform mix) predicts a win with a safety margin.
/// </summary>
public class StateMachinePlayer : IArtificialPlayer
{
    public enum EconomyMode { Eco, Expand, BuildArmy }
    public enum MilitaryMode { Scout, Guard, Attack }

    private readonly StateMachinePlayerOptions _options;
    private SettingsAnalysis? _analysis;
    private Hexagon? _expansionTarget;
    private int _estimatedEnemyUnits = -1;

    public EconomyMode Economy { get; private set; } = EconomyMode.Eco;
    public MilitaryMode Military { get; private set; } = MilitaryMode.Scout;
    public string Name { get; }

    public StateMachinePlayer(StateMachinePlayerOptions? options = null, string? name = null)
    {
        _options = options ?? new StateMachinePlayerOptions();
        Name = name ?? "StateMachine";
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        _analysis ??= GameSettingsAnalyzer.Analyze(playerGameState.GameSettings);
        var turn = new TurnPlan(playerGameState, _analysis, _options);
        UpdateEnemyEstimate(playerGameState);
        Economy = DecideEconomyMode(turn);
        Military = DecideMilitaryMode(turn);

        if (Economy == EconomyMode.Expand)
        {
            Expand(turn);
        }
        MigrateWorkers(turn);
        TrainWorkers(turn);
        if (Economy == EconomyMode.BuildArmy)
        {
            BuildArmy(turn);
        }
        EnsureSpace(turn);

        if (Military == MilitaryMode.Attack)
        {
            MarchFighters(turn, NearestKnownEnemyCompound(turn) ?? turn.MainBase);
        }
        else
        {
            MarchFighters(turn, turn.MainBase);
        }
        if (Military == MilitaryMode.Scout || IntelAge(turn) > _options.ScoutRefreshTurns)
        {
            SendScout(turn);
        }
        return turn.BuildCommands();
    }

    private void UpdateEnemyEstimate(PlayerGameState view)
    {
        if (_estimatedEnemyUnits < 0)
        {
            // Start positions and armies are part of the public game settings.
            _estimatedEnemyUnits = view.GameSettings.HexagonSettings.Values
                .Where(x => x.PlayerInitialization is not null && x.PlayerInitialization.Value.StartingPlayerId != view.PlayerId)
                .Sum(x => x.PlayerInitialization!.Value.InitialArmy.Total);
        }
        int observed = view.Observations.Values.Sum(x => x.OpponentUnitCounts.Values.Sum());
        if (observed > 0)
        {
            _estimatedEnemyUnits = observed;
        }
    }

    private EconomyMode DecideEconomyMode(TurnPlan turn)
    {
        if (turn.WorkerCount < turn.TargetWorkers && turn.WorkerCapacity > 0)
        {
            return EconomyMode.Eco;
        }
        // Only bases with harvestable matter left count; depleted bases are replaced by expansions.
        if (turn.ActiveIncomeHexagons.Count < _options.MaxIncomeBases && FindExpansionTarget(turn) is not null)
        {
            return EconomyMode.Expand;
        }
        return EconomyMode.BuildArmy;
    }

    private MilitaryMode DecideMilitaryMode(TurnPlan turn)
    {
        if (NearestKnownEnemyCompound(turn) is null)
        {
            return MilitaryMode.Scout;
        }
        Army fighters = turn.TotalFighterArmy;
        if (fighters.Total < _options.MinAttackArmy)
        {
            return MilitaryMode.Guard;
        }
        Army enemyEstimate = _analysis!.UniformFighterMix(_estimatedEnemyUnits);
        Army exponent = turn.View.GameSettings.FightExponent;
        double ownStrength = fighters.GetStrengthOver(enemyEstimate, exponent);
        double enemyStrength = enemyEstimate.GetStrengthOver(fighters, exponent);
        if (ownStrength > _options.AttackMargin * Math.Max(1, enemyStrength))
        {
            return MilitaryMode.Attack;
        }
        return MilitaryMode.Guard;
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

        // A worker already arrived: build the income building.
        if (turn.IdleWorkers(target) > 0 && turn.Matter >= planeCost &&
            turn.CountOwnedOrPendingBuildings(target, _analysis.IncomeBuilding) == 0)
        {
            turn.Construct(target, _analysis.IncomeBuilding);
            return;
        }

        // Reserve the money for the income building while the expansion party walks over.
        turn.Reserve(planeCost);

        // Walk every worker that is already underway (not on an income hexagon) further towards the target.
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

        // Top up the party from the main base.
        int underway = turn.OwnArmyHexagons
            .Where(x => x != turn.MainBase && !turn.IncomeHexagons.Contains(x))
            .Sum(x => (int)turn.View.PlayerState.Armies.GetValueOrDefault(x)[_analysis.Worker]);
        int partySize = Math.Min(_options.ExpansionWorkers - underway, turn.IdleWorkers(turn.MainBase) - 1);
        if (partySize > 0)
        {
            turn.Move(turn.MainBase, Navigation.StepToward(turn.View, turn.MainBase, target), Army.FromUnit(_analysis.Worker, (short)partySize));
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

    /// <summary>
    /// Walks idle workers from depleted income hexagons to the nearest income hexagon that still has matter.
    /// </summary>
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

    private void BuildArmy(TurnPlan turn)
    {
        GameSettings gameSettings = turn.View.GameSettings;

        // Training buildings per fighter type, cheapest buildings first.
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

        // Train the fighter type we own the least of first, to keep the composition mixed.
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

    private void EnsureSpace(TurnPlan turn)
    {
        GameSettings gameSettings = turn.View.GameSettings;
        int spacePerBuilding = gameSettings.ProvidedSpace[_analysis!.SpaceBuilding];
        if (turn.FreeSpace + turn.PendingSpace >= spacePerBuilding / 2 ||
            turn.Matter < gameSettings.CompoundCost[_analysis.SpaceBuilding] ||
            turn.IdleWorkers(turn.MainBase) <= 0)
        {
            return;
        }
        turn.Construct(turn.MainBase, _analysis.SpaceBuilding);
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
        // Pick the spare unit closest to the target and walk it over, one hexagon per turn.
        Hexagon? origin = null;
        int bestDistance = int.MaxValue;
        foreach (Hexagon hexagon in turn.OwnArmyHexagons)
        {
            if (hexagon == target)
            {
                return; // the scout arrived; vision refreshes the intel by itself
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
        var candidates = turn.View.Observations.Keys
            .Where(x => IsExpansionCandidate(turn, x))
            .OrderBy(x => x.DistanceTo(turn.MainBase))
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
            observation.OpponentCompounds.All(x => x.Value.IsEmpty);
    }

    private Hexagon? NearestKnownEnemyCompound(TurnPlan turn)
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
