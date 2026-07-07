using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.Web.Models;

/// <summary>What happened during one turn resolution, derived by diffing consecutive states.</summary>
public sealed record TurnReport(ushort FromTurn, ushort ToTurn, ImmutableArray<PlayerTurnReport> Players);

public sealed record PlayerTurnReport(
    string PlayerId,
    int MatterBefore,
    int MatterAfter,
    ImmutableArray<BattleEvent> Battles,
    ImmutableArray<TrainingCompletedEvent> CompletedTrainings,
    ImmutableArray<ConstructionCompletedEvent> CompletedConstructions,
    ImmutableArray<ResearchCompletedEvent> CompletedResearch,
    ImmutableArray<StructureLossEvent> StructuresLost,
    ImmutableArray<MovementEvent> Movements,
    ImmutableArray<IncomeEvent> Income)
{
    public bool IsEmpty =>
        Battles.IsEmpty && CompletedTrainings.IsEmpty && CompletedConstructions.IsEmpty &&
        CompletedResearch.IsEmpty && StructuresLost.IsEmpty && Movements.IsEmpty &&
        Income.IsEmpty && MatterBefore == MatterAfter;
}

/// <summary>Units this player lost in a fight at a hex.</summary>
public sealed record BattleEvent(Hexagon Hexagon, Army Losses);

public sealed record TrainingCompletedEvent(Hexagon Hexagon, Army Army);

public sealed record ConstructionCompletedEvent(Hexagon Hexagon, Compound Compound);

public sealed record ResearchCompletedEvent(Upgrade Upgrade, byte NewLevel);

/// <summary>Buildings this player lost (destroyed by enemy units).</summary>
public sealed record StructureLossEvent(Hexagon Hexagon, Compound Compound);

public sealed record MovementEvent(Hexagon From, Hexagon To, Army Army);

public sealed record IncomeEvent(Hexagon Hexagon, int Amount);
