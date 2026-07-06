using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

namespace Simulturn.AI.Players;

/// <summary>
/// Issues a random sequence of legal atomic actions per turn.
/// Armies move as stacks towards randomly weighted destinations (see <see cref="RandomPlayerOptions"/>)
/// instead of scattering unit by unit. All produced commands pass validation by construction.
/// </summary>
public class RandomPlayer : IArtificialPlayer
{
    private static readonly (short X, short Y)[] _directions = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)];
    private static readonly double[] _movementFractions = [0.25, 0.5, 1.0];
    private static readonly ImmutableArray<Unit> _units = [.. Enum.GetValues<Unit>()];
    private static readonly ImmutableArray<Building> _buildings = [.. Enum.GetValues<Building>()];

    private readonly Random _random;
    private readonly RandomPlayerOptions _options;

    public string Name { get; }

    public RandomPlayer(int seed, RandomPlayerOptions? options = null, string? name = null)
    {
        _random = new Random(seed);
        _options = options ?? new RandomPlayerOptions();
        Name = name ?? $"Random {seed}";
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        PlayerState playerState = playerGameState.PlayerState;
        GameSettings gameSettings = playerGameState.GameSettings;
        ushort turn = playerGameState.Turn;
        int matter = playerState.Matter;
        int freeSpace = playerState.AvailableSpace - playerState.UsedSpace;

        // Hexagons are processed in coordinate order so that a seed always produces the same commands.
        List<Hexagon> armyHexagons = playerState.Armies
            .Where(x => !x.Value.IsEmpty)
            .Select(x => x.Key)
            .OrderBy(x => x.X).ThenBy(x => x.Y)
            .ToList();

        // Dots occupied by pending constructions must stay on their hexagon;
        // a pending research requires at least one dot to remain.
        Dictionary<Hexagon, int> constructionDots = GetPendingConstructionDots(playerState, turn);
        HashSet<Hexagon> researchHexagons = playerState.Researches
            .Where(x => x.Key >= turn)
            .SelectMany(x => x.Value.Keys)
            .ToHashSet();

        (Hexagon Hexagon, Upgrade Upgrade)? research = TryPickResearch(playerGameState, armyHexagons, ref matter);
        if (research is not null)
        {
            researchHexagons.Add(research.Value.Hexagon);
        }

        Dictionary<Hexagon, Compound> constructions = PickConstructions(playerGameState, armyHexagons, constructionDots, ref matter);
        Dictionary<Hexagon, Army> trainings = PickTrainings(playerGameState, ref matter, ref freeSpace);
        Dictionary<Hexagon, MovementCommand> movements = PickMovements(playerGameState, armyHexagons, constructionDots, researchHexagons);

        Dictionary<Hexagon, Command> commands = [];
        IEnumerable<Hexagon> commandHexagons = constructions.Keys
            .Union(trainings.Keys)
            .Union(movements.Keys);
        if (research is not null)
        {
            commandHexagons = commandHexagons.Union([research.Value.Hexagon]);
        }
        foreach (Hexagon hexagon in commandHexagons)
        {
            commands[hexagon] = new Command()
            {
                Construction = constructions.GetValueOrDefault(hexagon),
                Training = trainings.GetValueOrDefault(hexagon),
                MovementCommands = movements.TryGetValue(hexagon, out var movement) ? [movement] : [],
                Upgrade = research?.Hexagon == hexagon ? research.Value.Upgrade : null
            };
        }
        return commands;
    }

    private (Hexagon Hexagon, Upgrade Upgrade)? TryPickResearch(PlayerGameState playerGameState,
        List<Hexagon> armyHexagons,
        ref int matter)
    {
        if (_random.NextDouble() >= _options.ResearchProbability)
        {
            return null;
        }
        PlayerState playerState = playerGameState.PlayerState;
        GameSettings gameSettings = playerGameState.GameSettings;

        // Researching an upgrade that is already in progress could exceed the available level on completion.
        HashSet<Upgrade> pendingUpgrades = playerState.Researches
            .Where(x => x.Key >= playerGameState.Turn)
            .SelectMany(x => x.Value.Values)
            .ToHashSet();

        List<(Hexagon Hexagon, Upgrade Upgrade, short Cost)> candidates = [];
        foreach (Hexagon hexagon in armyHexagons)
        {
            if (playerState.Armies[hexagon].Dot <= 0)
            {
                continue;
            }
            foreach (Upgrade upgrade in gameSettings.HexagonSettings[hexagon].ResearchableUpgrades.Distinct().OrderBy(x => x))
            {
                if (pendingUpgrades.Contains(upgrade))
                {
                    continue;
                }
                byte currentLevel = playerState.UpgradeLevels.GetValueOrDefault(upgrade);
                int availableLevels = gameSettings.HexagonSettings[hexagon].ResearchableUpgrades.Count(x => x == upgrade);
                if (currentLevel >= availableLevels || currentLevel >= gameSettings.Upgrades[upgrade].Length)
                {
                    continue;
                }
                short cost = gameSettings.Upgrades[upgrade][currentLevel].Cost;
                if (cost <= matter)
                {
                    candidates.Add((hexagon, upgrade, cost));
                }
            }
        }
        if (candidates.Count == 0)
        {
            return null;
        }
        var candidate = candidates[_random.Next(candidates.Count)];
        matter -= candidate.Cost;
        return (candidate.Hexagon, candidate.Upgrade);
    }

    private Dictionary<Hexagon, Compound> PickConstructions(PlayerGameState playerGameState,
        List<Hexagon> armyHexagons,
        Dictionary<Hexagon, int> constructionDots,
        ref int matter)
    {
        PlayerState playerState = playerGameState.PlayerState;
        GameSettings gameSettings = playerGameState.GameSettings;
        Dictionary<Hexagon, Compound> constructions = [];
        foreach (Hexagon hexagon in armyHexagons)
        {
            if (!gameSettings.HexagonSettings[hexagon].IsBuildable ||
                _random.NextDouble() >= _options.ConstructionProbability)
            {
                continue;
            }
            int idleDots = playerState.Armies[hexagon].Dot - constructionDots.GetValueOrDefault(hexagon);
            if (idleDots <= 0)
            {
                continue;
            }
            int budget = matter;
            List<Building> affordable = _buildings.Where(x => gameSettings.CompoundCost[x] <= budget).ToList();
            if (affordable.Count == 0)
            {
                continue;
            }
            Building building = affordable[_random.Next(affordable.Count)];
            constructions[hexagon] = Compound.FromBuilding(building, 1);
            constructionDots[hexagon] = constructionDots.GetValueOrDefault(hexagon) + 1;
            matter -= gameSettings.CompoundCost[building];
        }
        return constructions;
    }

    private Dictionary<Hexagon, Army> PickTrainings(PlayerGameState playerGameState,
        ref int matter,
        ref int freeSpace)
    {
        PlayerState playerState = playerGameState.PlayerState;
        GameSettings gameSettings = playerGameState.GameSettings;
        Dictionary<Hexagon, Army> trainings = [];
        var compoundHexagons = playerState.Compounds
            .Where(x => !x.Value.IsEmpty)
            .OrderBy(x => x.Key.X).ThenBy(x => x.Key.Y);
        foreach ((Hexagon hexagon, Compound compound) in compoundHexagons)
        {
            if (_random.NextDouble() >= _options.TrainingProbability)
            {
                continue;
            }

            // Buildings already training units on or after this turn are occupied.
            Army pendingTrainings = playerState.Trainings
                .Where(x => x.Key >= playerGameState.Turn)
                .Select(x => x.Value.GetValueOrDefault(hexagon))
                .Sum();

            int budget = matter;
            int space = freeSpace;
            List<Unit> trainable = _units.Where(x =>
                compound[GameSettings.TrainingBuildingPerUnit[x]] - pendingTrainings[x] > 0 &&
                gameSettings.ArmyCost[x] <= budget &&
                gameSettings.RequiredSpace[x] <= space).ToList();
            if (trainable.Count == 0)
            {
                continue;
            }
            Unit unit = trainable[_random.Next(trainable.Count)];
            int maxCount = compound[GameSettings.TrainingBuildingPerUnit[unit]] - pendingTrainings[unit];
            if (gameSettings.ArmyCost[unit] > 0)
            {
                maxCount = Math.Min(maxCount, matter / gameSettings.ArmyCost[unit]);
            }
            if (gameSettings.RequiredSpace[unit] > 0)
            {
                maxCount = Math.Min(maxCount, freeSpace / gameSettings.RequiredSpace[unit]);
            }
            short count = (short)(1 + _random.Next(maxCount));
            trainings[hexagon] = Army.FromUnit(unit, count);
            matter -= count * gameSettings.ArmyCost[unit];
            freeSpace -= count * gameSettings.RequiredSpace[unit];
        }
        return trainings;
    }

    private Dictionary<Hexagon, MovementCommand> PickMovements(PlayerGameState playerGameState,
        List<Hexagon> armyHexagons,
        Dictionary<Hexagon, int> constructionDots,
        HashSet<Hexagon> researchHexagons)
    {
        PlayerState playerState = playerGameState.PlayerState;
        List<Hexagon> enemyHexagons = playerGameState.Observations
            .Where(x => x.Value.OpponentCompounds.Any(y => !y.Value.IsEmpty))
            .Select(x => x.Key)
            .OrderBy(x => x.X).ThenBy(x => x.Y)
            .ToList();
        List<Hexagon> ownCompoundHexagons = playerState.Compounds
            .Where(x => !x.Value.IsEmpty)
            .Select(x => x.Key)
            .OrderBy(x => x.X).ThenBy(x => x.Y)
            .ToList();

        Dictionary<Hexagon, MovementCommand> movements = [];
        foreach (Hexagon hexagon in armyHexagons)
        {
            if (_random.NextDouble() >= _options.MovementProbability)
            {
                continue;
            }
            Army army = playerState.Armies[hexagon];
            int requiredDots = Math.Max(constructionDots.GetValueOrDefault(hexagon),
                researchHexagons.Contains(hexagon) ? 1 : 0);
            Army movable = army with { Dot = (short)Math.Max(0, army.Dot - requiredDots) };
            double fraction = _movementFractions[_random.Next(_movementFractions.Length)];
            Army moving = new Army()
            {
                Dot = (short)(movable.Dot * fraction),
                Triangle = (short)(movable.Triangle * fraction),
                Circle = (short)(movable.Circle * fraction),
                Square = (short)(movable.Square * fraction)
            };
            if (moving.IsEmpty)
            {
                continue;
            }
            List<Hexagon> neighbors = _directions
                .Select(x => new Hexagon((short)(hexagon.X + x.X), (short)(hexagon.Y + x.Y)))
                .Where(playerGameState.Observations.ContainsKey)
                .ToList();
            if (neighbors.Count == 0)
            {
                continue;
            }
            Hexagon destination = PickWeightedDestination(playerGameState, hexagon, neighbors, enemyHexagons, ownCompoundHexagons);
            movements[hexagon] = new MovementCommand()
            {
                Army = moving,
                Destination = destination
            };
        }
        return movements;
    }

    private Hexagon PickWeightedDestination(PlayerGameState playerGameState,
        Hexagon origin,
        List<Hexagon> candidates,
        List<Hexagon> enemyHexagons,
        List<Hexagon> ownCompoundHexagons)
    {
        double[] weights = new double[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            Hexagon candidate = candidates[i];
            double weight = 1;
            if (enemyHexagons.Count > 0 &&
                enemyHexagons.Min(x => x.DistanceTo(candidate)) < enemyHexagons.Min(x => x.DistanceTo(origin)))
            {
                weight += _options.AttackWeight;
            }
            if (ownCompoundHexagons.Count > 0 &&
                ownCompoundHexagons.Min(x => x.DistanceTo(candidate)) < ownCompoundHexagons.Min(x => x.DistanceTo(origin)))
            {
                weight += _options.DefendWeight;
            }
            HexagonObservation observation = playerGameState.Observations[candidate];
            if (observation.LastSeenTurn is null ||
                (observation.RemainingMatter > 0 && !ownCompoundHexagons.Contains(candidate)))
            {
                weight += _options.ExpandWeight;
            }
            weights[i] = weight;
        }
        double roll = _random.NextDouble() * weights.Sum();
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
            {
                return candidates[i];
            }
        }
        return candidates[^1];
    }

    private static Dictionary<Hexagon, int> GetPendingConstructionDots(PlayerState playerState, ushort turn)
    {
        Dictionary<Hexagon, int> constructionDots = [];
        foreach ((ushort completionTurn, var constructionsPerHexagon) in playerState.Constructions.Where(x => x.Key >= turn))
        {
            foreach ((Hexagon hexagon, Compound compound) in constructionsPerHexagon)
            {
                constructionDots[hexagon] = constructionDots.GetValueOrDefault(hexagon) + compound.Sum();
            }
        }
        return constructionDots;
    }
}
