using Simulturn.Core.Analysis;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.Analysis;

public sealed class LedgerTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. Player01 starts with 5 dots, a plane and a cube and can
    /// research the triangle upgrade at home, Player02 starts with 10 squares and a plane.
    /// A dot costs 75, a square 200, a plane 350, a cube and an axis 150.
    /// </summary>
    private static readonly GameSettings _settings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01", compound: new Compound() { Plane = 1, Cube = 1 }).WithResearch(Upgrade.TriangleExponent))
        .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 })));

    private static readonly int _researchPrice = _settings.NextUpgradeLevel(Upgrade.TriangleExponent, 0)!.Cost;

    [Test]
    public void Start_CountsTheStartMatterAndThePriceOfTheStartStock()
    {
        var ledgers = Ledger.Start(new GameState(_settings));

        ledgers["Player01"].ShouldBe(new MatterLedger()
        {
            StartMatter = 500,
            StartStock = 875,
            OnHand = 500,
            Dots = 375,
            Buildings = 500
        });
        ledgers["Player02"].ShouldBe(new MatterLedger()
        {
            StartMatter = 500,
            StartStock = 2350,
            OnHand = 500,
            FightingUnits = 2000,
            Buildings = 350
        });
    }

    [Test]
    public void Orders_AreSpentByKindAndStayInTheStockUntilTheyComplete()
    {
        GameState start = new GameState(_settings);
        var commands = DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command()
            {
                Training = new Army() { Dot = 1, Square = 1 },
                Construction = new Compound() { Axis = 1 }
            })
        ]);

        TurnResult turn = start.ResolveTurn(commands);
        MatterLedger ledger = Ledger.After(Ledger.Start(start), turn)["Player01"];

        // The construction takes one of the 5 dots off the harvest. The dot is trained at once,
        // the square and the axis are pending.
        ledger.Harvested.ShouldBe(40);
        ledger.SpentOnDots.ShouldBe(75);
        ledger.SpentOnFightingUnits.ShouldBe(200);
        ledger.SpentOnBuildings.ShouldBe(150);
        ledger.SpentOnResearch.ShouldBe(0);
        ledger.OnHand.ShouldBe(115);
        ledger.Dots.ShouldBe(450);
        ledger.FightingUnits.ShouldBe(0);
        ledger.Pending.ShouldBe(350);
        ledger.UnexplainedIncome.ShouldBe(0);
        ledger.UnexplainedSpending.ShouldBe(0);
    }

    [Test]
    public void FightAgainstANeutralArmy_CountsItsLossesApartFromTheOnesOfPlayers()
    {
        GameState start = new GameState(TestSettings.Create(Maps.Line(-1, 1)
            .SetItem(_player1Start, Maps.Start("Player01"))
            .SetItem(_center, Maps.Hexagon().WithGuard(new Army() { Dot = 3 }))
            .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 }))));

        TurnResult turn = start.ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player2Start, _center, new Army() { Square = 10 })
        ]));
        MatterLedger ledger = Ledger.After(Ledger.Start(start), turn)["Player02"];

        Army neutralLoss = turn.Report.Fights[_center].Losses[Neutral.Id];
        neutralLoss.IsEmpty.ShouldBeFalse();
        ledger.KilledNeutral.ShouldBe(neutralLoss.Dot * 75);
        ledger.Killed.ShouldBe(0);
        ledger.UnexplainedSpending.ShouldBe(0);
    }

    [Test]
    public void Raid_ShowsWhatFellWasDestroyedAndWasCanceledOnBothSides()
    {
        // Player02 marches 10 squares into the home of Player01, who in the same turn orders a
        // square, an axis and the triangle upgrade there. The 5 dots die with 1 of the squares,
        // the 9 surviving squares destroy the cube, and all three orders are canceled.
        GameState start = new GameState(_settings);
        TurnResult turn1 = start.ResolveTurn(DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player2Start, _center, new Army() { Square = 10 })
        ]));
        TurnResult turn2 = turn1.State.ResolveTurn(DictionaryExtensions.CommandsToDictionary([
            ("Player01", _player1Start, new Command()
            {
                Training = new Army() { Square = 1 },
                Construction = new Compound() { Axis = 1 },
                Research = Upgrade.TriangleExponent
            }),
            ("Player02", _center, new Command()
            {
                Movements = [new MovementOrder() { Destination = _player1Start, Army = new Army() { Square = 10 } }]
            })
        ]));

        var ledgers = Ledger.After(Ledger.After(Ledger.Start(start), turn1), turn2);

        MatterLedger raided = ledgers["Player01"];
        raided.SpentOnResearch.ShouldBe(_researchPrice);
        raided.Fallen.ShouldBe(375);
        raided.Destroyed.ShouldBe(150);
        raided.Canceled.ShouldBe(200 + 150 + _researchPrice);
        raided.Killed.ShouldBe(200);
        raided.Razed.ShouldBe(0);
        raided.Stock.ShouldBe(350);
        raided.UnexplainedIncome.ShouldBe(0);
        raided.UnexplainedSpending.ShouldBe(0);

        MatterLedger raider = ledgers["Player02"];
        raider.Fallen.ShouldBe(200);
        raider.Killed.ShouldBe(375);
        raider.Razed.ShouldBe(150);
        raider.FightingUnits.ShouldBe(1800);
        raider.UnexplainedIncome.ShouldBe(0);
        raider.UnexplainedSpending.ShouldBe(0);
    }
}
