using Simulturn.AI.Analysis;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

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

    private static readonly (short X, short Y)[] _directions = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)];

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
        Army enemyEstimate = UniformMix(_estimatedEnemyUnits);
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
                turn.Move(hexagon, StepToward(turn.View, hexagon, target), Army.FromUnit(_analysis.Worker, (short)idle));
            }
        }

        // Top up the party from the main base.
        int underway = turn.OwnArmyHexagons
            .Where(x => x != turn.MainBase && !turn.IncomeHexagons.Contains(x))
            .Sum(x => (int)turn.View.PlayerState.Armies.GetValueOrDefault(x)[_analysis.Worker]);
        int partySize = Math.Min(_options.ExpansionWorkers - underway, turn.IdleWorkers(turn.MainBase) - 1);
        if (partySize > 0)
        {
            turn.Move(turn.MainBase, StepToward(turn.View, turn.MainBase, target), Army.FromUnit(_analysis.Worker, (short)partySize));
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
            turn.Move(hexagon, StepToward(turn.View, hexagon, destination), Army.FromUnit(_analysis!.Worker, (short)idle));
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
            Army fighters = turn.MovableArmy(hexagon) with { Dot = 0 };
            Army fighterOnly = Army.Empty;
            foreach (Unit fighter in _analysis!.Fighters)
            {
                fighterOnly = fighterOnly.AddUnit(fighter, fighters[fighter]);
            }
            if (fighterOnly.IsEmpty)
            {
                continue;
            }
            turn.Move(hexagon, StepToward(turn.View, hexagon, target), fighterOnly);
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
            turn.Move(origin.Value, StepToward(turn.View, origin.Value, target), Army.FromUnit(_analysis!.Scout, 1));
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

    private Army UniformMix(int unitCount)
    {
        Army army = Army.Empty;
        int perType = unitCount / _analysis!.Fighters.Length;
        int remainder = unitCount % _analysis.Fighters.Length;
        for (int i = 0; i < _analysis.Fighters.Length; i++)
        {
            army = army.AddUnit(_analysis.Fighters[i], (short)(perType + (i < remainder ? 1 : 0)));
        }
        return army;
    }

    private static Hexagon StepToward(PlayerGameState view, Hexagon from, Hexagon target)
    {
        return _directions
            .Select(x => new Hexagon((short)(from.X + x.X), (short)(from.Y + x.Y)))
            .Where(view.Observations.ContainsKey)
            .OrderBy(x => x.DistanceTo(target))
            .ThenBy(x => x.X).ThenBy(x => x.Y)
            .First();
    }

    /// <summary>
    /// Tracks the budgets (matter, space, idle workers, unit capacities) while the modes
    /// add commands, so that the produced command set is valid by construction.
    /// </summary>
    private sealed class TurnPlan
    {
        private readonly SettingsAnalysis _analysis;
        private readonly Dictionary<Hexagon, Compound> _constructions = [];
        private readonly Dictionary<Hexagon, Army> _trainings = [];
        private readonly Dictionary<Hexagon, List<MovementCommand>> _movements = [];
        private readonly Dictionary<Hexagon, Army> _movedAway = [];
        private readonly Dictionary<Hexagon, int> _anchoredDots = [];
        private readonly Dictionary<Unit, int> _pendingUnitTrainings = [];
        private int _reservedMatter;

        public PlayerGameState View { get; }
        public int Matter { get; private set; }
        public int FreeSpace { get; private set; }
        public int PendingSpace { get; }
        public Hexagon MainBase { get; }
        public List<Hexagon> IncomeHexagons { get; }
        public List<Hexagon> ActiveIncomeHexagons { get; }
        public List<Hexagon> OwnCompoundHexagons { get; }
        public List<Hexagon> OwnArmyHexagons { get; }
        public int WorkerCount { get; }
        public int WorkerCapacity { get; }
        public int TargetWorkers { get; }
        public Army TotalFighterArmy { get; }

        public TurnPlan(PlayerGameState view, SettingsAnalysis analysis, StateMachinePlayerOptions options)
        {
            View = view;
            _analysis = analysis;
            PlayerState playerState = view.PlayerState;
            GameSettings gameSettings = view.GameSettings;
            Matter = playerState.Matter;
            FreeSpace = playerState.AvailableSpace - playerState.UsedSpace;

            foreach ((ushort completionTurn, var constructionsPerHexagon) in playerState.Constructions.Where(x => x.Key >= view.Turn))
            {
                foreach ((Hexagon hexagon, Compound compound) in constructionsPerHexagon)
                {
                    _anchoredDots[hexagon] = _anchoredDots.GetValueOrDefault(hexagon) + compound.Sum();
                }
            }
            foreach ((ushort completionTurn, var trainingsPerHexagon) in playerState.Trainings.Where(x => x.Key >= view.Turn))
            {
                foreach (Unit unit in Enum.GetValues<Unit>())
                {
                    _pendingUnitTrainings[unit] = _pendingUnitTrainings.GetValueOrDefault(unit) +
                        trainingsPerHexagon.Values.Sum(x => (int)x[unit]);
                }
            }
            PendingSpace = playerState.Constructions
                .Where(x => x.Key >= view.Turn)
                .SelectMany(x => x.Value.Values)
                .Sum(x => x * gameSettings.ProvidedSpace);

            OwnCompoundHexagons = playerState.Compounds
                .Where(x => !x.Value.IsEmpty)
                .Select(x => x.Key)
                .OrderBy(x => x.X).ThenBy(x => x.Y)
                .ToList();
            IncomeHexagons = playerState.Compounds
                .Where(x => x.Value[analysis.IncomeBuilding] > 0)
                .Select(x => x.Key)
                .OrderBy(x => x.X).ThenBy(x => x.Y)
                .ToList();
            ActiveIncomeHexagons = IncomeHexagons
                .Where(x => (view.Observations[x].RemainingMatter ?? gameSettings.HexagonSettings[x].Matter) > 0)
                .ToList();
            OwnArmyHexagons = playerState.Armies
                .Where(x => !x.Value.IsEmpty)
                .Select(x => x.Key)
                .OrderBy(x => x.X).ThenBy(x => x.Y)
                .ToList();
            MainBase = OwnCompoundHexagons
                .OrderByDescending(x => playerState.Compounds[x].Sum())
                .ThenBy(x => x.X).ThenBy(x => x.Y)
                .Cast<Hexagon?>()
                .FirstOrDefault() ?? OwnArmyHexagons.FirstOrDefault();

            WorkerCount = playerState.Armies.Values.Sum(x => (int)x[analysis.Worker]);
            WorkerCapacity = IncomeHexagons.Sum(hexagon =>
            {
                int remaining = view.Observations[hexagon].RemainingMatter ?? gameSettings.HexagonSettings[hexagon].Matter;
                return remaining > 0 ? (int)gameSettings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter[analysis.Worker] : 0;
            });
            TargetWorkers = Math.Min(WorkerCapacity, options.MaxWorkers);

            Army fighters = Army.Empty;
            foreach (Army army in playerState.Armies.Values)
            {
                foreach (Unit fighter in analysis.Fighters)
                {
                    fighters = fighters.AddUnit(fighter, army[fighter]);
                }
            }
            TotalFighterArmy = fighters;
        }

        public int IdleWorkers(Hexagon hexagon)
        {
            return View.PlayerState.Armies.GetValueOrDefault(hexagon)[_analysis.Worker]
                - _anchoredDots.GetValueOrDefault(hexagon)
                - _movedAway.GetValueOrDefault(hexagon)[_analysis.Worker];
        }

        public Army MovableArmy(Hexagon hexagon)
        {
            Army army = View.PlayerState.Armies.GetValueOrDefault(hexagon) - _movedAway.GetValueOrDefault(hexagon);
            return army with { Dot = (short)Math.Max(0, army.Dot - _anchoredDots.GetValueOrDefault(hexagon)) };
        }

        public int PendingUnitTrainings(Unit unit)
        {
            return _pendingUnitTrainings.GetValueOrDefault(unit);
        }

        public int CountOwnedOrPendingBuildings(Hexagon hexagon, Building building)
        {
            return View.PlayerState.Compounds.GetValueOrDefault(hexagon)[building] +
                _constructions.GetValueOrDefault(hexagon)[building] +
                View.PlayerState.Constructions
                    .Where(x => x.Key >= View.Turn)
                    .Sum(x => x.Value.GetValueOrDefault(hexagon)[building]);
        }

        public void Reserve(int matter)
        {
            _reservedMatter = Math.Max(_reservedMatter, matter);
        }

        public void Construct(Hexagon hexagon, Building building)
        {
            short cost = View.GameSettings.CompoundCost[building];
            if (Matter - _reservedMatter < cost || IdleWorkers(hexagon) <= 0)
            {
                return;
            }
            _constructions[hexagon] = _constructions.GetValueOrDefault(hexagon) + Compound.FromBuilding(building, 1);
            _anchoredDots[hexagon] = _anchoredDots.GetValueOrDefault(hexagon) + 1;
            Matter -= cost;
        }

        /// <summary>
        /// Trains up to <paramref name="wanted"/> units and returns how many trainings were issued.
        /// </summary>
        public int Train(Hexagon hexagon, Unit unit, int wanted)
        {
            GameSettings gameSettings = View.GameSettings;
            Building building = GameSettings.TrainingBuildingPerUnit[unit];
            Army pendingOnHexagon = View.PlayerState.Trainings
                .Where(x => x.Key >= View.Turn)
                .Select(x => x.Value.GetValueOrDefault(hexagon))
                .Sum();
            int capacity = View.PlayerState.Compounds.GetValueOrDefault(hexagon)[building]
                - pendingOnHexagon[unit]
                - _trainings.GetValueOrDefault(hexagon)[unit];
            int count = Math.Min(wanted, capacity);
            if (gameSettings.ArmyCost[unit] > 0)
            {
                count = Math.Min(count, (Matter - _reservedMatter) / gameSettings.ArmyCost[unit]);
            }
            if (gameSettings.RequiredSpace[unit] > 0)
            {
                count = Math.Min(count, FreeSpace / gameSettings.RequiredSpace[unit]);
            }
            if (count <= 0)
            {
                return 0;
            }
            _trainings[hexagon] = _trainings.GetValueOrDefault(hexagon) + Army.FromUnit(unit, (short)count);
            _pendingUnitTrainings[unit] = _pendingUnitTrainings.GetValueOrDefault(unit) + count;
            Matter -= count * gameSettings.ArmyCost[unit];
            FreeSpace -= count * gameSettings.RequiredSpace[unit];
            return count;
        }

        public void Move(Hexagon from, Hexagon to, Army army)
        {
            if (army.IsEmpty || from == to)
            {
                return;
            }
            Army movable = MovableArmy(from);
            army = Army.Min(army, movable);
            if (army.IsEmpty || Enum.GetValues<Unit>().Any(x => army[x] < 0))
            {
                return;
            }
            if (!_movements.TryGetValue(from, out var movements))
            {
                movements = [];
                _movements[from] = movements;
            }
            movements.Add(new MovementCommand() { Army = army, Destination = to });
            _movedAway[from] = _movedAway.GetValueOrDefault(from) + army;
        }

        public IReadOnlyDictionary<Hexagon, Command> BuildCommands()
        {
            Dictionary<Hexagon, Command> commands = [];
            IEnumerable<Hexagon> hexagons = _constructions.Keys
                .Union(_trainings.Keys)
                .Union(_movements.Keys);
            foreach (Hexagon hexagon in hexagons)
            {
                commands[hexagon] = new Command()
                {
                    Construction = _constructions.GetValueOrDefault(hexagon),
                    Training = _trainings.GetValueOrDefault(hexagon),
                    MovementCommands = _movements.TryGetValue(hexagon, out var movements)
                        ? [.. movements]
                        : []
                };
            }
            return commands;
        }
    }
}
