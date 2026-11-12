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

    private CommandValidator(GameSettings settings, string playerId, PlayerState playerState, IReadOnlyDictionary<Hexagon, Command> commands)
    {
        _settings = settings;
        _playerId = playerId;
        _playerState = playerState;
        _commands = commands;
        _upgradesInResearch = _playerState.UpgradesInResearch();
        _requestedUpgradeCounts = GetRequestedUpgradeCounts(commands);
        _arrivingDots = GetArrivingDots(commands);
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

    private IEnumerable<CommandValidation> ValidateCommands()
    {
        return ValidateSpace()
            .Concat(_commands.SelectMany(x => ValidateCommand(x.Key, x.Value)))
            .Concat(ValidateMatter());
    }

    private IEnumerable<CommandValidation> ValidateCommand(Hexagon hexagon, Command command)
    {
        return ValidateTraining(hexagon, command)
            .Concat(ValidateConstruction(hexagon, command))
            .Concat(ValidateMovement(hexagon, command))
            .Concat(ValidateResearch(hexagon, command))
            .Concat(ValidateResearchKeepsDot(hexagon, command));
    }

    /// <summary>
    /// Only checked if there is a training. A player can use more space than available, but not train in such a case.
    /// </summary>
    private IEnumerable<CommandValidation> ValidateSpace()
    {
        int additionalSpace = _commands.Sum(command => command.Value.Training * _settings.RequiredSpace);
        if (_commands.Any(x => x.Value.Training.Total > 0) &&
            _playerState.UsedSpace + additionalSpace > _playerState.AvailableSpace)
        {
            yield return new InsufficientSpace(_playerId, _playerState.UsedSpace + additionalSpace, _playerState.AvailableSpace);
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
        Army trainings = _playerState.Trainings
            .Select(x => x.Value.GetValueOrDefault(hexagon))
            .Sum();
        trainings += command.Training;
        foreach (var unit in EnumValues.Units)
        {
            short trainedUnits = trainings[unit];
            short availableBuildings = compound[GameSettings.TrainingBuilding(unit)];
            if (trainedUnits > availableBuildings)
            {
                yield return new MissingBuildingForTraining(_playerId, hexagon, unit, trainedUnits, availableBuildings);
            }
        }
    }

    /// <summary>
    /// Every building under construction, pending ones included, needs its own dot (worker).
    /// </summary>
    private IEnumerable<CommandValidation> ValidateConstruction(Hexagon hexagon, Command command)
    {
        if (command.Construction.IsEmpty)
        {
            yield break;
        }
        Compound constructions = _playerState.Constructions
            .Select(x => x.Value.GetValueOrDefault(hexagon))
            .Sum();
        constructions += command.Construction;
        short dots = _playerState.Armies.GetValueOrDefault(hexagon)[Unit.Dot];
        if (constructions.Sum() > dots)
        {
            yield return new MissingDotsForConstruction(_playerId, hexagon, constructions, dots);
        }
    }

    private IEnumerable<CommandValidation> ValidateMovement(Hexagon hexagon, Command command)
    {
        Army departingArmy = command.MovementCommands.Select(x => x.Army).Sum();
        Army availableArmy = _playerState.Armies.GetValueOrDefault(hexagon);
        foreach (var unit in EnumValues.Units)
        {
            if (departingArmy[unit] > availableArmy[unit])
            {
                yield return new MissingArmyForMovement(_playerId, hexagon, unit, departingArmy[unit], availableArmy[unit]);
            }
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
        Army departingArmy = command.MovementCommands.Select(x => x.Army).Sum();
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
        int trainingCost = _commands.Sum(command => command.Value.Training * _settings.ArmyCost);
        int constructionCost = _commands.Sum(command => command.Value.Construction * _settings.CompoundCost);
        int totalCost = trainingCost + constructionCost + GetResearchCost();
        if (totalCost > _playerState.Matter)
        {
            yield return new InsufficientMatter(_playerId, totalCost, _playerState.Matter);
        }
    }

    /// <summary>
    /// Every requested upgrade pays the level after the completed ones. An upgrade
    /// requested twice is invalid anyway and is counted once.
    /// </summary>
    private int GetResearchCost()
    {
        int researchCost = 0;
        foreach (Upgrade upgrade in _requestedUpgradeCounts.Keys)
        {
            int level = _playerState.UpgradeLevels.GetValueOrDefault(upgrade);
            if (level < _settings.MaxUpgradeLevel(upgrade))
            {
                researchCost += _settings.Upgrades[upgrade][level].Cost; // a level of 1 means the first upgrade, which is at index 0
            }
        }
        return researchCost;
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
        foreach (MovementCommand movement in commands.Values.SelectMany(command => command.MovementCommands))
        {
            arrivingDots[movement.Destination] = arrivingDots.GetValueOrDefault(movement.Destination) + movement.Army.Dot;
        }
        return arrivingDots;
    }
}
