using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.AI.Analysis;

public static class GameSettingsAnalyzer
{
    private static readonly ImmutableArray<Unit> _units = [.. Enum.GetValues<Unit>()];
    private static readonly ImmutableArray<Building> _buildings = [.. Enum.GetValues<Building>()];

    public static SettingsAnalysis Analyze(GameSettings gameSettings)
    {
        // The engine ties construction and income to dots, but deriving the worker from the
        // income settings keeps the analysis correct if that ever becomes configurable.
        Unit worker = _units
            .Where(x => gameSettings.Income[x] > 0)
            .OrderByDescending(x => gameSettings.Income[x])
            .DefaultIfEmpty(Unit.Dot)
            .First();
        Unit scout = _units
            .OrderBy(x => gameSettings.ArmyCost[x])
            .ThenBy(x => x)
            .First();
        ImmutableArray<Unit> fighters = _units
            .Where(x => x != worker)
            .OrderBy(x => gameSettings.ArmyCost[x])
            .ThenBy(x => x)
            .ToImmutableArray();
        Building spaceBuilding = _buildings
            .Where(x => gameSettings.ProvidedSpace[x] > 0)
            .OrderBy(x => gameSettings.CompoundCost[x] / (double)gameSettings.ProvidedSpace[x])
            .ThenBy(x => x)
            .DefaultIfEmpty(Building.Axis)
            .First();
        return new SettingsAnalysis()
        {
            Worker = worker,
            Scout = scout,
            Fighters = fighters,
            // Income requires a plane on the hexagon; this is fixed in the engine.
            IncomeBuilding = Building.Plane,
            SpaceBuilding = spaceBuilding
        };
    }
}
