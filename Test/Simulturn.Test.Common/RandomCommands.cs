using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.Upgrades;

namespace Simulturn.Test.Common;

/// <summary>
/// The command source of the random games of layer 3: issues a random sequence of legal atomic
/// actions per turn for one player of one game. It only gets the view of its player.
/// Armies move as stacks towards randomly weighted destinations (see <see cref="RandomCommandsOptions"/>)
/// instead of scattering unit by unit. All produced commands pass validation by construction.
/// A test tool for the engine, not a bot: it is not meant to play well.
/// </summary>
public sealed class RandomCommands
{
    private static readonly (short X, short Y)[] _directions = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)];
    private static readonly double[] _movementFractions = [0.25, 0.5, 1.0];
    private static readonly ImmutableArray<Unit> _units = [.. Enum.GetValues<Unit>()];
    private static readonly ImmutableArray<Building> _buildings = [.. Enum.GetValues<Building>()];

    private readonly Random _random;
    private readonly RandomCommandsOptions _options;
    private ImmutableDictionary<Hexagon, HexagonMemory> _memories = ImmutableDictionary<Hexagon, HexagonMemory>.Empty;

    public RandomCommands(int seed, RandomCommandsOptions? options = null)
    {
        _random = new Random(seed);
        _options = options ?? new RandomCommandsOptions();
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState playerGameState)
    {
        _memories = FogOfWar.UpdateMemories(_memories, playerGameState);
        PlayerState playerState = playerGameState.PlayerState;
        GameSettings gameSettings = playerGameState.GameSettings;
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
        Dictionary<Hexagon, int> constructionDots = GetPendingConstructionDots(playerState);
        HashSet<Hexagon> researchHexagons = playerState.Researches.Keys.ToHashSet();

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
                Research = research?.Hexagon == hexagon ? research.Value.Upgrade : null
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

        // One pending research per upgrade and per hexagon, as the engine demands.
        var upgradesInResearch = playerState.UpgradesInResearch();

        List<(Hexagon Hexagon, Upgrade Upgrade, int Cost)> candidates = [];
        foreach (Hexagon hexagon in armyHexagons)
        {
            if (playerState.Armies[hexagon].Dot <= 0 || playerState.Researches.ContainsKey(hexagon))
            {
                continue;
            }
            foreach (Upgrade upgrade in gameSettings.HexagonSettings[hexagon].ResearchableUpgrades.OrderBy(x => x))
            {
                if (upgradesInResearch.Contains(upgrade))
                {
                    continue;
                }
                byte currentLevel = playerState.UpgradeLevels.GetValueOrDefault(upgrade);
                if (gameSettings.NextUpgradeLevel(upgrade, currentLevel) is null)
                {
                    continue;
                }
                int cost = Costs.Research(gameSettings, playerState.UpgradeLevels, upgrade).Matter;
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
            List<Building> affordable = _buildings.Where(x => Costs.Construction(gameSettings, x).Matter <= budget).ToList();
            if (affordable.Count == 0)
            {
                continue;
            }
            Building building = affordable[_random.Next(affordable.Count)];
            constructions[hexagon] = Compound.FromBuilding(building, 1);
            constructionDots[hexagon] = constructionDots.GetValueOrDefault(hexagon) + 1;
            matter -= Costs.Construction(gameSettings, building).Matter;
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
            Army pendingTrainings = playerState.Trainings.On(hexagon);

            int budget = matter;
            int space = freeSpace;
            List<Unit> trainable = _units.Where(x =>
                compound[GameSettings.TrainingBuilding(x)] - pendingTrainings[x] > 0 &&
                Costs.Training(gameSettings, x).Matter <= budget &&
                Costs.Training(gameSettings, x).Space <= space).ToList();
            if (trainable.Count == 0)
            {
                continue;
            }
            Unit unit = trainable[_random.Next(trainable.Count)];
            int maxCount = compound[GameSettings.TrainingBuilding(unit)] - pendingTrainings[unit];
            OrderCost price = Costs.Training(gameSettings, unit);
            if (price.Matter > 0)
            {
                maxCount = Math.Min(maxCount, matter / price.Matter);
            }
            if (price.Space > 0)
            {
                maxCount = Math.Min(maxCount, freeSpace / price.Space);
            }
            short count = (short)(1 + _random.Next(maxCount));
            trainings[hexagon] = Army.FromUnit(unit, count);
            matter -= count * price.Matter;
            freeSpace -= count * price.Space;
        }
        return trainings;
    }

    private Dictionary<Hexagon, MovementCommand> PickMovements(PlayerGameState playerGameState,
        List<Hexagon> armyHexagons,
        Dictionary<Hexagon, int> constructionDots,
        HashSet<Hexagon> researchHexagons)
    {
        PlayerState playerState = playerGameState.PlayerState;
        List<Hexagon> enemyHexagons = _memories
            .Where(x => x.Value.Static.OpponentCompounds.Any(y => !y.Value.IsEmpty))
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
            Hexagon destination = PickWeightedDestination(hexagon, neighbors, enemyHexagons, ownCompoundHexagons);
            movements[hexagon] = new MovementCommand()
            {
                Army = moving,
                Destination = destination
            };
        }
        return movements;
    }

    private Hexagon PickWeightedDestination(
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
            StaticSighting? sighting = _memories.GetValueOrDefault(candidate)?.Static;
            if (sighting is null ||
                (sighting.RemainingMatter > 0 && !ownCompoundHexagons.Contains(candidate)))
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

    private static Dictionary<Hexagon, int> GetPendingConstructionDots(PlayerState playerState)
    {
        Dictionary<Hexagon, int> constructionDots = [];
        foreach ((TurnHexagon key, Compound compound) in playerState.Constructions)
        {
            constructionDots[key.Hexagon] = constructionDots.GetValueOrDefault(key.Hexagon) + compound.Sum();
        }
        return constructionDots;
    }
}
