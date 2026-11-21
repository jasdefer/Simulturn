using System.Globalization;
using Microsoft.Extensions.Configuration;
using Simulturn.Bots.Catalog;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Replay;

/// <summary>
/// Runs the replay study from the configuration: reads the game, plays it, prints the ledgers
/// of the game and writes the ledgers of every turn to a file.
/// </summary>
public static class ReplayCommand
{
    /// <summary>
    /// Plays one game of the arena profile <c>--profile</c>: the first two bots and the first map
    /// of the profile or of <c>--Bots</c> and <c>--Maps</c>, with <c>--MapSeed</c> and <c>--Seat</c>
    /// (both 1 when missing). The limits are the ones of the profile, so the game ends as in the run.
    /// </summary>
    /// <returns>The exit code: 0 after a replay, 2 when the game is not named.</returns>
    public static int Run(IConfiguration configuration, TextWriter output)
    {
        string profile = configuration["profile"] ?? ArenaCommand.DefaultProfile;
        IConfigurationSection section = configuration.GetSection($"Arena:Profiles:{profile}");
        if (!section.Exists())
        {
            IEnumerable<string> known = configuration.GetSection("Arena:Profiles").GetChildren().Select(x => x.Key);
            output.WriteLine($"There is no arena profile '{profile}'. Known: {string.Join(", ", known)}.");
            return 2;
        }

        ArenaOptions options = ArenaCommand.ReadOptions(configuration, section);
        if (options.Bots.Length < 2 || options.Maps.IsEmpty)
        {
            output.WriteLine("A replay needs two bots and a map: --Bots <bot>,<opponent> --Maps <map> --MapSeed <seed> --Seat <1|2>");
            return 2;
        }
        ArenaGameKey key = new(options.Bots[0], options.Bots[1], options.Maps[0], ReadNumber(configuration, "MapSeed"), ReadNumber(configuration, "Seat"));

        ReplayResult replay = ReplayStudy.Run(key, options.Limits, BotCatalog.All, ArenaMaps.All);
        output.Write(ReplayTable.Render(replay));

        string folder = configuration["OutputFolder"] ?? "measurements";
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"replay-{key.Bot}-{key.Opponent}-{key.Map}-{key.MapSeed}-{key.Seat}.csv");
        File.WriteAllText(file, ReplayTable.RenderCsv(replay));
        output.WriteLine();
        output.WriteLine($"Every turn written to {Path.GetFullPath(file)}");
        return 0;
    }

    private static int ReadNumber(IConfiguration configuration, string key)
    {
        return configuration[key] is { } value ? int.Parse(value, CultureInfo.InvariantCulture) : 1;
    }
}
