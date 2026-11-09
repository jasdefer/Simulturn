namespace Simulturn.Core.Model.State.StateValidation;

internal interface IPlayerStateValidation : IStateValidation
{
    public string PlayerId { get; }
}

public record NegativeUnitCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;
public record NegativeTrainingCount(string PlayerId, Hexagon Hexagon, Unit Unit, short Count) : IPlayerStateValidation;

public record NegativeBuildingCount(string PlayerId, Hexagon Hexagon, Building Building, short Count) : IPlayerStateValidation;
public record NegativeConstructionCount(string PlayerId, Hexagon Hexagon, Building Building, short ConstructionCount) : IPlayerStateValidation;

/// <summary>
/// An entry whose completion turn lies in the past. The queues only hold pending work:
/// <see cref="GameState.NextTurn"/> removes every entry in the turn it is applied.
/// </summary>
public record ExpiredTraining(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;
public record ExpiredConstruction(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;
public record ExpiredResearch(string PlayerId, ushort CompletionTurn) : IPlayerStateValidation;

/// <summary>
/// The completed and pending levels of an upgrade exceed the levels defined in
/// <see cref="GameSettings.Upgrades"/>. Such a state cannot be reached through validated commands.
/// </summary>
public record UpgradeLevelsExceedTable(string PlayerId, Upgrade Upgrade, byte CompletedLevels, byte PendingLevels, int MaxLevel) : IPlayerStateValidation;
