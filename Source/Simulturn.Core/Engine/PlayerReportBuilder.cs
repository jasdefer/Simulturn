using Simulturn.Core.Model;
using Simulturn.Core.Model.Report;

namespace Simulturn.Core.Engine;

/// <summary>
/// Collects the <see cref="PlayerActivityReport"/> of one player while a turn is resolved.
/// </summary>
internal sealed class PlayerReportBuilder
{
    public ImmutableDictionary<Hexagon, IncomeReport>.Builder Incomes { get; } = ImmutableDictionary.CreateBuilder<Hexagon, IncomeReport>();
    public ImmutableDictionary<Hexagon, Army>.Builder CompletedTrainings { get; } = ImmutableDictionary.CreateBuilder<Hexagon, Army>();
    public ImmutableDictionary<Hexagon, Compound>.Builder CompletedConstructions { get; } = ImmutableDictionary.CreateBuilder<Hexagon, Compound>();
    public ImmutableDictionary<Hexagon, ResearchReport>.Builder CompletedResearches { get; } = ImmutableDictionary.CreateBuilder<Hexagon, ResearchReport>();
    public ImmutableArray<MovementReport>.Builder Movements { get; } = ImmutableArray.CreateBuilder<MovementReport>();
    public ImmutableDictionary<Hexagon, Compound>.Builder CanceledConstructions { get; } = ImmutableDictionary.CreateBuilder<Hexagon, Compound>();
    public ImmutableDictionary<Hexagon, Army>.Builder CanceledTrainings { get; } = ImmutableDictionary.CreateBuilder<Hexagon, Army>();
    public ImmutableDictionary<Hexagon, Upgrade>.Builder CanceledResearches { get; } = ImmutableDictionary.CreateBuilder<Hexagon, Upgrade>();

    public PlayerActivityReport ToReport(int matterBefore, int matterAfter)
    {
        return new PlayerActivityReport()
        {
            MatterBefore = matterBefore,
            MatterAfter = matterAfter,
            Incomes = Incomes.ToImmutable(),
            CompletedTrainings = CompletedTrainings.ToImmutable(),
            CompletedConstructions = CompletedConstructions.ToImmutable(),
            CompletedResearches = CompletedResearches.ToImmutable(),
            Movements = Movements.ToImmutable(),
            CanceledConstructions = CanceledConstructions.ToImmutable(),
            CanceledTrainings = CanceledTrainings.ToImmutable(),
            CanceledResearches = CanceledResearches.ToImmutable()
        };
    }
}
