using Simulturn.AI.Evaluation;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Simulturn.Runner;

public static class ArenaWorker
{
    public static int Run(Dictionary<string, string> arguments)
    {
        string playersArgument = arguments.GetValueOrDefault("players", "Commander,StateMachine,Random,Random:Aggressive,Idle");
        var players = playersArgument.Split(',').Select(PlayerCatalog.Parse).ToList();
        if (players.Count < 2)
        {
            Console.Error.WriteLine("The arena requires at least 2 players.");
            return 1;
        }
        var options = new ArenaOptions()
        {
            GamesPerSeatPairing = int.Parse(arguments.GetValueOrDefault("games", "10")),
            MaxTurns = ushort.Parse(arguments.GetValueOrDefault("max-turns", "200")),
            Seed = int.Parse(arguments.GetValueOrDefault("seed", "0"))
        };
        var gameSettings = GameSettingsFactory.HexDisc(
            radius: int.Parse(arguments.GetValueOrDefault("radius", "3")),
            matterPerHexagon: int.Parse(arguments.GetValueOrDefault("matter", "1500")));

        Console.WriteLine($"Arena: {players.Count} players, {options.GamesPerSeatPairing * 2} games per pairing, " +
            $"max {options.MaxTurns} turns, map radius {arguments.GetValueOrDefault("radius", "3")}, seed {options.Seed}");
        Console.WriteLine();

        var stopwatch = Stopwatch.StartNew();
        ImmutableArray<PairingResult> results = Arena.Run(gameSettings, players, options, Console.WriteLine);
        stopwatch.Stop();

        Console.WriteLine();
        PrintStandings(players.Select(x => x.Name).ToList(), results);
        Console.WriteLine();
        Console.WriteLine($"Finished {results.Sum(x => x.Games)} games in {stopwatch.Elapsed.TotalSeconds:F1}s.");
        return 0;
    }

    private static void PrintStandings(IReadOnlyList<string> playerNames, ImmutableArray<PairingResult> results)
    {
        Console.WriteLine($"{"Player",-28} {"Wins",6} {"Losses",7} {"Draws",6} {"Win rate",9}");
        var standings = playerNames
            .Select(name =>
            {
                int wins = results.Sum(x => x.PlayerA == name ? x.WinsA : x.PlayerB == name ? x.WinsB : 0);
                int losses = results.Sum(x => x.PlayerA == name ? x.WinsB : x.PlayerB == name ? x.WinsA : 0);
                int draws = results.Sum(x => x.PlayerA == name || x.PlayerB == name ? x.Draws : 0);
                int games = wins + losses + draws;
                return (name, wins, losses, draws, rate: games > 0 ? wins / (double)games : 0);
            })
            .OrderByDescending(x => x.rate);
        foreach ((string name, int wins, int losses, int draws, double rate) in standings)
        {
            Console.WriteLine($"{name,-28} {wins,6} {losses,7} {draws,6} {rate,8:P0}");
        }
    }
}
