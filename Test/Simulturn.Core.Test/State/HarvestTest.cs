using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;
using System.Collections.Immutable;

namespace Simulturn.Core.Test.State;

public sealed class HarvestTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Compound _plane = new() { Plane = 1 };
    private static readonly Dictionary<string, Dictionary<Hexagon, Command>> _noCommands = [];

    /// <summary>
    /// A map of 3 hexagons in a line. The players start on the opposite ends with 5 dots and a plane each
    /// and 1000 matter on their start hexagon. A dot harvests 10 matter, up to 12 dots harvest on a hexagon.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02")));

    private static IncomeReport HarvestAtHome(Army army, Compound? compound = null, Compound? startedConstructions = null, int remainingMatter = 1000)
    {
        return Harvest.Of(_gameSettings, _player1Start, army, compound ?? _plane, startedConstructions ?? Compound.Empty, remainingMatter);
    }

    [Test]
    public void DotsOnAPlane_HarvestTheIncomePerDot()
    {
        HarvestAtHome(new Army() { Dot = 5 }).ShouldBe(new IncomeReport(new Army() { Dot = 5 }, 50));
    }

    [Test]
    public void HexagonWithoutPlane_YieldsNoIncome()
    {
        HarvestAtHome(new Army() { Dot = 5 }, compound: new Compound() { Axis = 1 }).ShouldBe(new IncomeReport(Army.Empty, 0));
    }

    [Test]
    public void UnitsWithoutIncome_AreNoHarvesters()
    {
        HarvestAtHome(new Army() { Dot = 5, Square = 3 }).ShouldBe(new IncomeReport(new Army() { Dot = 5 }, 50));
    }

    [Test]
    public void StartedConstruction_TakesOneDotOffTheHarvest()
    {
        IncomeReport income = HarvestAtHome(new Army() { Dot = 5 }, startedConstructions: new Compound() { Axis = 1, Dome = 1 });

        income.ShouldBe(new IncomeReport(new Army() { Dot = 3 }, 30));
    }

    [Test]
    public void MoreDotsThanTheCapacity_HarvestOnlyWithTheCapacity()
    {
        Harvest.Capacity(_gameSettings, _player1Start).ShouldBe(new Army() { Dot = 12 });
        HarvestAtHome(new Army() { Dot = 15 }).ShouldBe(new IncomeReport(new Army() { Dot = 12 }, 120));
    }

    [Test]
    public void HexagonWithLittleMatterLeft_YieldsOnlyWhatIsLeft()
    {
        HarvestAtHome(new Army() { Dot = 5 }, remainingMatter: 20).ShouldBe(new IncomeReport(new Army() { Dot = 5 }, 20));
        HarvestAtHome(new Army() { Dot = 5 }, remainingMatter: 0).Matter.ShouldBe(0);
    }

    [Test]
    public void TurnResolution_CapsTheIncomeAtTheCapacityOfTheHexagon()
    {
        GameSettings gameSettings = TestSettings.Create(_gameSettings.HexagonSettings.ToImmutableDictionary()
            .SetItem(_player1Start, Maps.Start("Player01", matter: 10000, army: new Army() { Dot = 15 }) with
            {
                MaxNumberOfUnitsGeneratingMatter = new Army() { Dot = 3 }
            }));

        GameState turn1 = new GameState(gameSettings).PlayTurn(_noCommands);

        turn1.PlayerStates["Player01"].Matter.ShouldBe(gameSettings.StartMatter + 30);
        turn1.RemainingMatter[_player1Start].ShouldBe(10000 - 30);
    }

    [Test]
    public void Forecast_GivesTheIncomeTheTurnResolutionReports()
    {
        // Arrange: Player01 starts an axis, so 4 of the 5 dots harvest.
        GameState state = new(_gameSettings);
        var orders = Command.Create((_player1Start, new Command() { Construction = new Compound() { Axis = 1 } }));

        // Act
        var forecast = Harvest.Forecast(FogOfWar.GetPlayerGameState(state, "Player01"), orders);
        TurnResult result = state.ResolveTurn(new Dictionary<string, Dictionary<Hexagon, Command>>() { { "Player01", orders } });

        // Assert
        forecast.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, IncomeReport>(_player1Start, new IncomeReport(new Army() { Dot = 4 }, 40)));
        result.Report.Activities["Player01"].Incomes.ShouldHaveSingleItem().ShouldBe(forecast.Single());
    }

    [Test]
    public void ForecastWithoutOrders_GivesTheIncomeOfAnIdleTurn()
    {
        GameState state = new(_gameSettings);

        var forecast = Harvest.Forecast(FogOfWar.GetPlayerGameState(state, "Player02"), new Dictionary<Hexagon, Command>());

        forecast.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<Hexagon, IncomeReport>(_player2Start, new IncomeReport(new Army() { Dot = 5 }, 50)));
    }
}
