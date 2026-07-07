using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.AI.Evaluation;

/// <summary>
/// Lets artificial players compete in a round robin tournament to measure their relative strength.
/// Every pairing is played in both starting positions to remove any map bias.
/// A player that returns invalid commands forfeits the game.
/// </summary>
public static class Arena
{
    public static ImmutableArray<PairingResult> Run(GameSettings gameSettings,
        IReadOnlyList<ArenaPlayer> players,
        ArenaOptions? options = null,
        Action<string>? log = null)
    {
        options ??= new ArenaOptions();
        var playerIds = gameSettings.HexagonSettings.Values
            .Where(x => x.PlayerInitialization is not null)
            .Select(x => x.PlayerInitialization!.Value.StartingPlayerId)
            .Distinct()
            .OrderBy(x => x)
            .ToImmutableArray();
        if (playerIds.Length != 2)
        {
            throw new ArgumentException($"The arena requires game settings with exactly 2 players, found {playerIds.Length}.", nameof(gameSettings));
        }

        List<PairingResult> results = [];
        for (int i = 0; i < players.Count - 1; i++)
        {
            for (int j = i + 1; j < players.Count; j++)
            {
                results.Add(RunPairing(gameSettings, playerIds, players[i], players[j], options, log));
            }
        }
        return [.. results];
    }

    private static PairingResult RunPairing(GameSettings gameSettings,
        ImmutableArray<string> playerIds,
        ArenaPlayer playerA,
        ArenaPlayer playerB,
        ArenaOptions options,
        Action<string>? log)
    {
        // seat 0: A starts as the first player id, seat 1: seats swapped.
        var games = Enumerable.Range(0, options.GamesPerSeatPairing)
            .SelectMany(game => new[] { (game, seat: 0), (game, seat: 1) })
            .ToList();
        object aggregationLock = new();
        int winsA = 0, winsB = 0, draws = 0, forfeitsA = 0, forfeitsB = 0;
        long totalTurns = 0;

        Parallel.ForEach(games,
            new ParallelOptions() { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism },
            job =>
            {
                int seed = options.Seed + job.game;
                string idA = playerIds[job.seat];
                string idB = playerIds[1 - job.seat];
                var gamePlayers = new Dictionary<string, IArtificialPlayer>()
                {
                    { idA, playerA.CreatePlayer(seed) },
                    { idB, playerB.CreatePlayer(seed + 1_000_000) }
                };

                string? winnerId;
                ushort turns;
                bool forfeitA = false, forfeitB = false;
                try
                {
                    GameResult result = GameRunner.Run(gameSettings, gamePlayers, options.MaxTurns);
                    winnerId = result.WinnerId;
                    turns = result.Turns;
                }
                catch (InvalidCommandsException exception)
                {
                    // Invalid commands forfeit the game.
                    winnerId = exception.PlayerId == idA ? idB : idA;
                    forfeitA = exception.PlayerId == idA;
                    forfeitB = exception.PlayerId == idB;
                    turns = 0;
                }

                lock (aggregationLock)
                {
                    totalTurns += turns;
                    forfeitsA += forfeitA ? 1 : 0;
                    forfeitsB += forfeitB ? 1 : 0;
                    if (winnerId == idA)
                    {
                        winsA++;
                    }
                    else if (winnerId == idB)
                    {
                        winsB++;
                    }
                    else
                    {
                        draws++;
                    }
                }
            });

        var result = new PairingResult()
        {
            PlayerA = playerA.Name,
            PlayerB = playerB.Name,
            WinsA = winsA,
            WinsB = winsB,
            Draws = draws,
            ForfeitsA = forfeitsA,
            ForfeitsB = forfeitsB,
            AverageTurns = games.Count > 0 ? totalTurns / (double)games.Count : 0
        };
        log?.Invoke(result.ToString());
        return result;
    }
}
