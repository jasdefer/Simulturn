using Simulturn.AI.Analysis;
using Simulturn.Core.Model;
using Simulturn.Core.Setup;

namespace Simulturn.AI.Test;

public class GameSettingsAnalyzerTest
{
    [Test]
    public void Analyze_DerivesRolesFromDefaultSettings()
    {
        var gameSettings = DefaultSettings.Create(MapGeneratorOptions.Open);

        var analysis = GameSettingsAnalyzer.Analyze(gameSettings);

        analysis.Worker.ShouldBe(Unit.Dot);
        analysis.Scout.ShouldBe(Unit.Dot); // the dot is the cheapest unit in the default settings
        analysis.Fighters.ShouldBe([Unit.Triangle, Unit.Circle, Unit.Square], ignoreOrder: true);
        analysis.IncomeBuilding.ShouldBe(Building.Plane);
        analysis.SpaceBuilding.ShouldBe(Building.Axis); // 150 matter per 10 space beats the plane
    }
}
