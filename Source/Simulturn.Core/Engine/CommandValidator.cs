using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;

namespace Simulturn.Core.Engine;

/// <summary>
/// Checks the commands of one player against the rules. An instance holds the context of exactly one validation.
/// </summary>
public sealed class CommandValidator
{
    private readonly GameSettings _settings;
    private readonly string _playerId;
    private readonly PlayerState _playerState;
    private readonly IReadOnlyDictionary<Hexagon, Command> _commands;
    private readonly ImmutableHashSet<Upgrade> _upgradesInResearch;
    private readonly Dictionary<Upgrade, byte> _requestedUpgradeCounts;
    private readonly Dictionary<Hexagon, int> _arrivingDots;
    private readonly OrderCost _cost;

    private CommandValidator(GameSettings settings, string playerId, PlayerState playerState, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        _settings = settings;
        _playerId = playerId;
        _playerState = playerState;
        _commands = commands;
        _upgradesInResearch = _playerState.UpgradesInResearch();
        _requestedUpgradeCounts = GetRequestedUpgradeCounts(commands);
        _arrivingDots = GetArrivingDots(commands);
        _cost = Costs.Of(settings, playerState.UpgradeLevels, commands);
    }

    /// <summary>
    /// Returns every rule the commands of the player break; empty when the commands are valid.
    /// Only valid commands may be passed to <see cref="TurnResolution.Resolve"/>.
    /// </summary>
    public static IEnumerable<CommandValidation> Validate(GameState state, string playerId, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        if (!state.PlayerIds.Contains(playerId))
        {
            yield return new InvalidPlayerId(playerId);
            yield break;
        }

        CommandValidator validator = new(state.GameSettings, playerId, state.PlayerStates[playerId], commands);
        foreach (CommandValidation validation in validator.ValidateCommands())
        {
            yield return validation;
        }
    }

    /// <summary>
    /// The same check for a player who only holds the own view: the rules read nothing but the
    /// settings and the own state, so the result equals the one for the full game state.
    /// </summary>
    public static IEnumerable<CommandValidation> Validate(PlayerGameState view, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        CommandValidator validator = new(view.GameSettings, view.PlayerId, view.PlayerState, commands);
        return validator.ValidateCommands();
    }

    /// <summary>
    /// A defeated player takes no more orders. Nothing else is reported then: every other rule
    /// would only repeat that the player has nothing left to order with.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateCommands()
    {
        if (Standing.IsDefeated(_playerState))
        {
            return _commands.Values.Any(HasOrders) ? [new PlayerDefeated(_playerId)] : [];
        }
        return ValidateSpace()
            .Concat(_commands.SelectMany(x => ValidateCommand(x.Key, x.Value)))
            .Concat(ValidateMatter());
    }

    private static bool HasOrders(Command command)
    {
        return !command.Training.IsEmpty ||
            !command.Construction.IsEmpty ||
            command.Research is not null ||
            command.Movements.Any(x => !x.Army.IsEmpty);
    }

    private IEnumerable<CommandValidation> ValidateCommand(Hexagon hexagon, Command command)
    {
        return ValidateTraining(hexagon, command)
            .Concat(ValidateConstruction(hexagon, command))
            .Concat(ValidateMovement(hexagon, command))
            .Concat(ValidateConstructionKeepsDots(hexagon, command))
            .Concat(ValidateResearch(hexagon, command))
            .Concat(ValidateResearchKeepsDot(hexagon, command));
    }

    /// <summary>
    /// Only checked if there is a training. A player can use more space than available, but not train in such a case.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateSpace()
    {
        if (_commands.Any(x => x.Value.Training.Total > 0) &&
            _playerState.UsedSpace + _cost.Space > _playerState.AvailableSpace)
        {
            yield return new InsufficientSpace(_playerId, _playerState.UsedSpace + _cost.Space, _playerState.AvailableSpace);
        }
    }

    /// <summary>
    /// Every unit in training, pending ones included, needs its own building.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateTraining(Hexagon hexagon, Command command)
    {
        if (command.Training.IsEmpty)
        {
            yield break;
        }
        Compound compound = _playerState.Compounds.GetValueOrDefault(hexagon, Compound.Empty);
        Army trainings = _playerState.Trainings.On(hexagon) + command.Training;
        Army slots = Occupancy.TrainingSlots(compound);
        Army freeSlots = Occupancy.FreeTrainingSlots(compound, trainings);
        foreach (var unit in EnumValues.Units)
        {
            if (freeSlots[unit] < 0)
            {
                yield return new MissingBuildingForTraining(_playerId, hexagon, unit, trainings[unit], slots[unit]);
            }
        }
    }

    /// <summary>
    /// Every building under construction, pending ones included, needs its own dot (worker),
    /// and the hexagon must be buildable.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateConstruction(Hexagon hexagon, Command command)
    {
        if (command.Construction.IsEmpty)
        {
            yield break;
        }
        if (!_settings.HexagonSettings.TryGetValue(hexagon, out var hexagonSettings) || !hexagonSettings.IsBuildable)
        {
            yield return new HexagonNotBuildable(_playerId, hexagon);
        }
        Compound constructions = _playerState.Constructions.On(hexagon) + command.Construction;
        short dots = _playerState.Armies.GetValueOrDefault(hexagon)[Unit.Dot];
        if (constructions.Sum() > dots)
        {
            yield return new MissingDotsForConstruction(_playerId, hexagon, constructions, dots);
        }
    }

    private IEnumerable<CommandValidation> ValidateMovement(Hexagon hexagon, Command command)
    {
        Army departingArmy = command.Movements.Select(x => x.Army).Sum();
        Army availableArmy = _playerState.Armies.GetValueOrDefault(hexagon);
        foreach (var unit in EnumValues.Units)
        {
            if (departingArmy[unit] > availableArmy[unit])
            {
                yield return new MissingArmyForMovement(_playerId, hexagon, unit, departingArmy[unit], availableArmy[unit]);
            }
        }
        foreach (MovementOrder movement in command.Movements)
        {
            // The turn resolution skips a movement without units.
            if (movement.Army.IsEmpty)
            {
                continue;
            }
            if (!_settings.HexagonSettings.ContainsKey(movement.Destination))
            {
                yield return new MovementDestinationOffMap(_playerId, hexagon, movement.Destination);
            }
            int distance = hexagon.DistanceTo(movement.Destination);
            Army reachableArmy = Movement.ReachableArmy(_settings, movement.Army, distance);
            foreach (var unit in EnumValues.Units)
            {
                if (movement.Army[unit] != reachableArmy[unit])
                {
                    yield return new MovementExceedsRange(_playerId, hexagon, movement.Destination, unit, distance, Movement.Range(_settings)[unit]);
                }
            }
        }
    }

    /// <summary>
    /// Movements leave one dot per pending or requested construction on the hexagon. A dot arriving
    /// this turn does not stand in for a dot that builds.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateConstructionKeepsDots(Hexagon hexagon, Command command)
    {
        short departingDots = command.Movements.Select(x => x.Army).Sum().Dot;
        if (departingDots <= 0)
        {
            yield break;
        }
        Compound constructions = _playerState.Constructions.On(hexagon) + command.Construction;
        if (constructions.IsEmpty)
        {
            yield break;
        }
        int remainingDots = _playerState.Armies.GetValueOrDefault(hexagon).Dot - departingDots;
        if (constructions.Sum() > remainingDots)
        {
            yield return new MovementLeavesConstructionWithoutDot(_playerId, hexagon, constructions, (short)Math.Max(0, remainingDots));
        }
    }

    /// <summary>
    /// One pending research per upgrade and per hexagon (work item research-cost-and-duration).
    /// </summary>
    private IEnumerable<CommandValidation> ValidateResearch(Hexagon hexagon, Command command)
    {
        if (command.Research is not Upgrade upgrade)
        {
            yield break;
        }
        if (!_settings.HexagonSettings.TryGetValue(hexagon, out var hexagonSettings) ||
            !hexagonSettings.ResearchableUpgrades.Contains(upgrade))
        {
            yield return new UpgradeNotResearchableOnHexagon(_playerId, hexagon, upgrade);
        }

        byte completedLevels = _playerState.UpgradeLevels.GetValueOrDefault(upgrade);
        int maxLevel = _settings.MaxUpgradeLevel(upgrade);
        if (completedLevels >= maxLevel)
        {
            yield return new UpgradeExceedsAvailableLevel(_playerId, hexagon, upgrade, completedLevels, maxLevel);
        }
        if (_upgradesInResearch.Contains(upgrade))
        {
            yield return new UpgradeAlreadyInResearch(_playerId, hexagon, upgrade);
        }
        if (_requestedUpgradeCounts[upgrade] > 1)
        {
            yield return new UpgradeRequestedOnMultipleHexagons(_playerId, hexagon, upgrade, _requestedUpgradeCounts[upgrade]);
        }
        if (_playerState.Researches.TryGetValue(hexagon, out Research pendingResearch))
        {
            yield return new HexagonAlreadyResearching(_playerId, hexagon, upgrade, pendingResearch.Upgrade);
        }
        if (!_playerState.Armies.TryGetValue(hexagon, out var army) || army.Dot <= 0)
        {
            yield return new ResearchRequiresDot(_playerId, hexagon, upgrade);
        }
    }

    /// <summary>
    /// Movements leave a dot on a hexagon with a pending or requested research.
    /// A dot arriving this turn keeps a research alive, so the dots moving in count.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateResearchKeepsDot(Hexagon hexagon, Command command)
    {
        Army departingArmy = command.Movements.Select(x => x.Army).Sum();
        Army availableArmy = _playerState.Armies.GetValueOrDefault(hexagon);
        if (availableArmy.Dot > 0 &&
            availableArmy.Dot - departingArmy.Dot + _arrivingDots.GetValueOrDefault(hexagon) <= 0 &&
            (command.Research is not null || _playerState.Researches.ContainsKey(hexagon)))
        {
            Upgrade researched = command.Research ?? _playerState.Researches[hexagon].Upgrade;
            yield return new MovementLeavesResearchWithoutDot(_playerId, hexagon, researched);
        }
    }

    private IEnumerable<CommandValidation> ValidateMatter()
    {
        if (_cost.Matter > _playerState.Matter)
        {
            yield return new InsufficientMatter(_playerId, _cost.Matter, _playerState.Matter);
        }
    }

    /// <summary>
    /// Counting the requests per upgrade keeps the result independent of the command order.
    /// </summary>
    private static Dictionary<Upgrade, byte> GetRequestedUpgradeCounts(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        return commands.Values
            .Where(command => command.Research is not null)
            .GroupBy(command => command.Research!.Value)
            .ToDictionary(group => group.Key, group => (byte)group.Count());
    }

    private static Dictionary<Hexagon, int> GetArrivingDots(IReadOnlyDictionary<Hexagon, Command> commands)
    {
        Dictionary<Hexagon, int> arrivingDots = [];
        foreach (MovementOrder movement in commands.Values.SelectMany(command => command.Movements))
        {
            arrivingDots[movement.Destination] = arrivingDots.GetValueOrDefault(movement.Destination) + movement.Army.Dot;
        }
        return arrivingDots;
    }
}
