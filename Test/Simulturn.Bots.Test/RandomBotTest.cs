using Simulturn.Bots.Catalog;
using Simulturn.Core.Engine;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.Report;
using Simulturn.Core.Model.State;
using Simulturn.Core.Setup;
using Simulturn.Test.Common;

namespace Simulturn.Bots.Test;

public sealed class RandomBotTest
{
    private const string Player = "Player01";
    private const string Opponent = "Player02";
    private static readonly Hexagon _start = new Hexagon(-1, 0);
    private static readonly Hexagon _center = new Hexagon(0, 0);
    private static readonly Hexagon _opponentStart = new Hexagon(1, 0);
    private static readonly Army _fiveDots = new() { Dot = 5 };

    /// <summary>
    /// A map of 3 hexagons in a line. The bot starts on one end, an opponent with 5 dots and a
    /// plane on the other end, and the center is free.
    /// </summary>
    private static GameSettings CreateSettings(int startHexagonMatter, int centerMatter = 0, int startMatter = TestSettings.DefaultStartMatter, Army? army = null)
    {
        return TestSettings.Create(
            Maps.Line(-1, 1)
                .SetItem(_start, Maps.Start(Player, startHexagonMatter, army ?? _fiveDots))
                .SetItem(_center, Maps.Hexagon(centerMatter))
                .SetItem(_opponentStart, Maps.Start(Opponent, matter: 0)),
            startMatter);
    }

    /// <summary>
    /// Plays the bot against an opponent that never gives an order and returns the last state
    /// and the commands of the bot turn by turn.
    /// </summary>
    private static (GameState State, List<IReadOnlyDictionary<Hexagon, Command>> Commands) Play(GameSettings settings, RandomBot bot, int turns)
    {
        TurnResult turn = Game.Start(settings);
        List<IReadOnlyDictionary<Hexagon, Command>> played = [];
        for (int i = 0; i < turns; i++)
        {
            IReadOnlyDictionary<Hexagon, Command> commands = bot.GetCommands(FogOfWar.GetPlayerGameState(turn, Player));
            played.Add(commands);
            TurnAttempt attempt = Game.PlayTurn(turn.State, new Dictionary<string, IReadOnlyDictionary<Hexagon, Command>>()
            {
                { Player, commands },
                { Opponent, ScriptedBot.NoCommands }
            });
            turn = attempt.Value.ShouldBeOfType<TurnResult>();
        }
        return (turn.State, played);
    }

    private static IEnumerable<Army> FighterMovements(IEnumerable<IReadOnlyDictionary<Hexagon, Command>> commands)
    {
        return commands
            .SelectMany(x => x.Values)
            .SelectMany(x => x.Movements)
            .Select(x => x.Army with { Dot = 0 })
            .Where(x => !x.IsEmpty);
    }

    [Test]
    public void HexagonWithMatter_GetsAnotherDot()
    {
        GameSettings settings = CreateSettings(startHexagonMatter: 10000);

        var (_, commands) = Play(settings, new RandomBot(1), turns: 1);

        commands[0][_start].Training.Dot.ShouldBe((short)1);
    }

    [Test]
    public void HexagonWithoutMatter_GetsNoDot()
    {
        GameSettings settings = CreateSettings(startHexagonMatter: 0);

        var (_, commands) = Play(settings, new RandomBot(1), turns: 1);

        commands[0].Values.ShouldAllBe(x => x.Training.Dot == 0);
    }

    [Test]
    public void FreeHexagonWithMatter_KeepsThePriceOfAPlaneBack()
    {
        GameSettings settings = CreateSettings(startHexagonMatter: 10000, centerMatter: 4000);
        GameState start = Game.Start(settings).State;

        var (_, commands) = Play(settings, new RandomBot(1), turns: 1);

        int left = Costs.Budget(settings, start.PlayerStates[Player], commands[0]).Matter;
        left.ShouldBeGreaterThanOrEqualTo(Costs.Construction(settings, Building.Plane).Matter);
    }

    [Test]
    public void NoHexagonLeftToHarvest_SpendsThePriceOfAPlaneToo()
    {
        GameSettings settings = CreateSettings(startHexagonMatter: 10000);
        GameState start = Game.Start(settings).State;

        var (_, commands) = Play(settings, new RandomBot(1), turns: 1);

        int left = Costs.Budget(settings, start.PlayerStates[Player], commands[0]).Matter;
        left.ShouldBeLessThan(Costs.Construction(settings, Building.Plane).Matter);
    }

    [Test]
    public void SpareDot_ClaimsTheFreeHexagonWithAPlane()
    {
        GameSettings settings = CreateSettings(startHexagonMatter: 10000, centerMatter: 4000);
        RandomBot bot = new(1, new RandomBotOptions() { ExpeditionProbability = 1 });

        var (state, _) = Play(settings, bot, turns: 20);

        state.PlayerStates[Player].Compounds.GetValueOrDefault(_center).Plane.ShouldBe((short)1);
    }

    [Test]
    public void ArmyThatCanTakeTheOpponent_MovesAsAWhole()
    {
        // Without matter nothing is trained, so the army stays what it is.
        Army army = new() { Square = 8, Dot = 5 };
        GameSettings settings = CreateSettings(startHexagonMatter: 0, startMatter: 0, army: army);

        var (_, commands) = Play(settings, new RandomBot(1), turns: 10);

        List<Army> movements = FighterMovements(commands).ToList();
        movements.ShouldNotBeEmpty();
        movements.ShouldAllBe(x => x == new Army() { Square = 8 });
    }

    [Test]
    public void ArmyOutnumberedByTheOpponent_StaysWhileItStillGrows()
    {
        // 3 squares against the 5 dots of the opponent. The bot has income, so more will come.
        Army army = new() { Square = 3, Dot = 5 };
        GameSettings settings = CreateSettings(startHexagonMatter: 10000, startMatter: 0, army: army);

        var (_, commands) = Play(settings, new RandomBot(1), turns: 5);

        FighterMovements(commands).ShouldBeEmpty();
    }

    [Test]
    public void SameSeed_PlaysTheSameMatch()
    {
        GameSettings settings = DefaultSettings.Create(MapGeneratorOptions.Expansions);
        MatchLimits limits = new() { MaxTurns = 300 };
        Dictionary<string, IBot> Bots() => new() { { Player, new RandomBot(1) }, { Opponent, new RandomBot(2) } };

        MatchResult first = Match.Play(settings, Bots(), limits);
        MatchResult second = Match.Play(settings, Bots(), limits);

        second.ShouldBe(first);
    }

    [Test]
    public void MirrorMatch_IssuesOnlyValidCommandsOnEveryMapType()
    {
        MapGeneratorOptions[] maps = [MapGeneratorOptions.Open, MapGeneratorOptions.HomeOnly, MapGeneratorOptions.Expansions, MapGeneratorOptions.Guarded];
        MatchLimits limits = new() { MaxTurns = 300 };

        foreach (MapGeneratorOptions map in maps)
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                GameSettings settings = DefaultSettings.Create(map with { Seed = seed });
                Dictionary<string, IBot> bots = new() { { Player, new RandomBot(seed) }, { Opponent, new RandomBot(-seed) } };

                // Invalid commands of a bot throw.
                Should.NotThrow(() => Match.Play(settings, bots, limits));
            }
        }
    }
}
