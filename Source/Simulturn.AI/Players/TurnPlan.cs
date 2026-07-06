using Simulturn.AI.Analysis;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.AI.Players;

/// <summary>
/// Tracks the budgets (matter, space, idle workers, unit capacities) while an AI adds
/// commands, so that the produced command set is valid by construction.
/// </summary>
internal sealed class TurnPlan
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
        // The main base follows the economy: it is the building hexagon where the workers are,
        // not necessarily the (possibly depleted) starting hexagon with the most buildings.
        MainBase = OwnCompoundHexagons
            .OrderByDescending(x => playerState.Armies.GetValueOrDefault(x)[analysis.Worker])
            .ThenByDescending(x => playerState.Compounds[x].Sum())
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

    /// <summary>
    /// The movable fighting units on the hexagon, without workers.
    /// </summary>
    public Army MovableFighters(Hexagon hexagon)
    {
        Army movable = MovableArmy(hexagon);
        Army fighters = Army.Empty;
        foreach (Unit fighter in _analysis.Fighters)
        {
            fighters = fighters.AddUnit(fighter, movable[fighter]);
        }
        return fighters;
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
