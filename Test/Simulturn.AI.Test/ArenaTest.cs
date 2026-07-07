using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;

namespace Simulturn.AI.Test;

public class ArenaTest
{
    [Test]
    public void RoundRobin_PlaysAllPairingsInBothSeats()
    {
        var gameSettings = GameSettingsFactory.HexDisc(radius: 2, matterPerHexagon: 500);
        List<ArenaPlayer> players =
        [
            new ArenaPlayer() { Name = "Idle A", CreatePlayer = _ => new IdlePlayer() },
            new ArenaPlayer() { Name = "Idle B", CreatePlayer = _ => new IdlePlayer() },
            new ArenaPlayer() { Name = "Random", CreatePlayer = seed => new RandomPlayer(seed) }
        ];

        var results = Arena.Run(gameSettings, players, new ArenaOptions() { GamesPerSeatPairing = 1, MaxTurns = 20 });

        results.Length.ShouldBe(3); // 3 pairings in a 3 player round robin
        results.ShouldAllBe(x => x.Games == 2); // every pairing plays both seat assignments
        // Two idle players can never destroy anything: every game is a turn limit draw.
        results.Single(x => x.PlayerA == "Idle A" && x.PlayerB == "Idle B").Draws.ShouldBe(2);
    }
}
