using Simulturn.Core.Extensions;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.Validation;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Core.Test.State;

public sealed class GameTest
{
    private static readonly Hexagon _player1Start = new Hexagon(-1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Hexagon _player2Start = new Hexagon(1, 0);

    /// <summary>
    /// A map of 3 hexagons in a line. Player01 starts with 5 dots and a plane, Player02 with
    /// 10 squares that cross the whole line in one turn.
    /// </summary>
    private static readonly GameSettings _gameSettings = TestSettings.Create(Maps.Line(-1, 1)
        .SetItem(_player1Start, Maps.Start("Player01"))
        .SetItem(_player2Start, Maps.Start("Player02", army: new Army() { Square = 10 }))) with
    {
        MovementRange = new Army() { Triangle = 1, Circle = 1, Square = 2, Dot = 1 }
    };

    [Test]
    public void Start_GivesTheStartStateWithAnEmptyReport()
    {
        TurnResult start = Game.Start(_gameSettings);

        start.State.Turn.ShouldBe((ushort)0);
        start.State.PlayerIds.ShouldBe(["Player01", "Player02"], ignoreOrder: true);
        start.Report.ShouldBe(TurnReport.Empty);
    }

    [Test]
    public void ValidCommands_AreResolved()
    {
        GameState state = Game.Start(_gameSettings).State;
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 2 })
        ]);

        TurnAttempt attempt = Game.PlayTurn(state, commands);

        TurnResult result = attempt.Value.ShouldBeOfType<TurnResult>();
        result.State.Turn.ShouldBe((ushort)1);
        result.State.PlayerStates["Player01"].Armies[_center].ShouldBe(new Army() { Dot = 2 });
        result.Report.Activities["Player01"].Movements.ShouldHaveSingleItem();
    }

    [Test]
    public void TurnWithoutCommands_IsResolved()
    {
        GameState state = Game.Start(_gameSettings).State;

        TurnAttempt attempt = Game.PlayTurn(state, new Dictionary<string, Dictionary<Hexagon, Command>>());

        attempt.Value.ShouldBeOfType<TurnResult>().State.Turn.ShouldBe((ushort)1);
    }

    [Test]
    public void InvalidCommandsOfOnePlayer_RefuseTheTurnWithTheBrokenRules()
    {
        // Arrange: Player01 moves more dots than there are, Player02 gives a valid order.
        GameState state = Game.Start(_gameSettings).State;
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player01", _player1Start, _center, new Army() { Dot = 10 }),
            ("Player02", _player2Start, _center, new Army() { Square = 10 })
        ]);

        // Act
        TurnAttempt attempt = Game.PlayTurn(state, commands);

        // Assert: only the player with a broken rule is listed.
        RefusedCommands refused = attempt.Value.ShouldBeOfType<RefusedCommands>();
        refused.Validations.Keys.ShouldBe(["Player01"]);
        refused.Validations["Player01"].ShouldHaveSingleItem().Value
            .ShouldBe(new MissingArmyForMovement("Player01", _player1Start, Unit.Dot, 10, 5));
    }

    [Test]
    public void CommandsOfAnUnknownPlayer_RefuseTheTurn()
    {
        GameState state = Game.Start(_gameSettings).State;
        var commands = DictionaryExtensions.MovementsToDictionary([
            ("Player03", _player1Start, _center, new Army() { Dot = 1 })
        ]);

        RefusedCommands refused = Game.PlayTurn(state, commands).Value.ShouldBeOfType<RefusedCommands>();

        refused.Validations["Player03"].ShouldHaveSingleItem().Value.ShouldBe(new InvalidPlayerId("Player03"));
    }

    [Test]
    public void TurnAfterTheGameIsOver_Throws()
    {
        // Arrange: the squares kill the dots and destroy the plane, the only building of Player01.
        GameState state = Game.Start(_gameSettings).State;
        TurnResult raid = Game.PlayTurn(state, DictionaryExtensions.MovementsToDictionary([
            ("Player02", _player2Start, _player1Start, new Army() { Square = 10 })
        ])).Value.ShouldBeOfType<TurnResult>();
        Standing.Of(raid.State).Outcome.Value.ShouldBe(new Victory("Player02"));

        // Act and assert
        Should.Throw<InvalidOperationException>(() => Game.PlayTurn(raid.State, new Dictionary<string, Dictionary<Hexagon, Command>>()));
    }

    [Test]
    public void ReplayingTheCommandsOfAGame_GivesTheSameState()
    {
        // Arrange: the settings and the commands of every turn are all a stored game needs.
        List<Dictionary<string, Dictionary<Hexagon, Command>>> turns =
        [
            DictionaryExtensions.MovementsToDictionary([("Player01", _player1Start, _center, new Army() { Dot = 2 })]),
            DictionaryExtensions.CommandsToDictionary([("Player01", _player1Start, new Command() { Training = new Army() { Dot = 1 } })]),
            DictionaryExtensions.MovementsToDictionary([("Player02", _player2Start, _center, new Army() { Square = 4 })])
        ];

        // Act
        string first = GameStateSnapshot.Of(Replay(turns));
        string second = GameStateSnapshot.Of(Replay(turns));

        // Assert
        second.ShouldBe(first);
    }

    private static GameState Replay(List<Dictionary<string, Dictionary<Hexagon, Command>>> turns)
    {
        GameState state = Game.Start(_gameSettings).State;
        foreach (var commands in turns)
        {
            state = Game.PlayTurn(state, commands).Value.ShouldBeOfType<TurnResult>().State;
        }
        return state;
    }
}
