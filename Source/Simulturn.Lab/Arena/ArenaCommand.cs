using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Simulturn.Bots;
using Simulturn.Bots.Catalog;
using Simulturn.Lab.Comparison;

namespace Simulturn.Lab.Arena;

/// <summary>
/// The <c>arena</c> study of the lab: reads a profile from the configuration, plays the run,
/// prints the table and the comparison of its games and writes the result, the games, the
/// comparison and the games to look at to files.
/// </summary>
public static class ArenaCommand
{
    public const string DefaultProfile = "quick";

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public static int Run(IConfiguration configuration, TextWriter output)
    {
        string profile = configuration["profile"] ?? DefaultProfile;
        IConfigurationSection section = configuration.GetSection($"Arena:Profiles:{profile}");
        if (!section.Exists())
        {
            IEnumerable<string> known = configuration.GetSection("Arena:Profiles").GetChildren().Select(x => x.Key);
            output.WriteLine($"There is no arena profile '{profile}'. Known: {string.Join(", ", known)}.");
            return 2;
        }

        ArenaOptions options = ReadOptions(configuration, section);
        ArenaResult result = ArenaStudy.Run(options, BotCatalog.All, ArenaMaps.All);
        ImmutableArray<ComparisonGroup> comparison = ComparisonStudy.Of(result.Games);
        output.Write(ArenaTable.Render(result));
        output.WriteLine();
        output.Write(ComparisonTable.Render(comparison));

        string folder = configuration["OutputFolder"] ?? "measurements";
        Directory.CreateDirectory(folder);
        string name = Path.Combine(folder, $"arena-{profile}-{DateTime.Now:yyyyMMdd-HHmmss}");
        File.WriteAllText($"{name}.json", JsonSerializer.Serialize(result, _json));
        File.WriteAllText($"{name}-games.csv", ArenaTable.RenderGamesCsv(result));
        File.WriteAllText($"{name}-comparison.csv", ComparisonTable.RenderCsv(comparison));
        File.WriteAllText($"{name}-picks.csv", ComparisonTable.RenderPicksCsv(comparison));
        output.WriteLine();
        output.WriteLine($"Written to {Path.GetFullPath(name)}.json, the games to -games.csv, the comparison to -comparison.csv, the games to look at to -picks.csv");
        output.WriteLine($"Look at a game with: replay --profile {profile} --Bots <bot>,<opponent> --Maps <map> --MapSeed <seed> --Seat <seat>");
        return 0;
    }

    /// <summary>
    /// The options of a profile. A value given on the command line (<c>--MapSeeds 50</c>,
    /// <c>--Bots idle,idle</c>) replaces the one of the profile.
    /// </summary>
    public static ArenaOptions ReadOptions(IConfiguration configuration, IConfigurationSection profile)
    {
        MatchLimits defaults = new();
        return new ArenaOptions()
        {
            Bots = ReadList(configuration, profile, "Bots"),
            Maps = ReadList(configuration, profile, "Maps"),
            MapSeeds = ReadNumber(configuration, profile, "MapSeeds", 1),
            Limits = new MatchLimits()
            {
                MaxTurns = ReadTurns(configuration, profile, "MaxTurns", defaults.MaxTurns),
                StallTurns = ReadTurns(configuration, profile, "StallTurns", defaults.StallTurns)
            }
        };
    }

    private static ImmutableArray<string> ReadList(IConfiguration configuration, IConfigurationSection profile, string key)
    {
        if (configuration[key] is { } overridden)
        {
            return [.. overridden.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }
        return [.. profile.GetSection(key).GetChildren().Select(x => x.Value).OfType<string>()];
    }

    private static ushort ReadTurns(IConfiguration configuration, IConfigurationSection profile, string key, ushort fallback)
    {
        int turns = ReadNumber(configuration, profile, key, fallback);
        if (turns is < 0 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(key, turns, $"{key} must lie between 0 and {ushort.MaxValue}.");
        }
        return (ushort)turns;
    }

    private static int ReadNumber(IConfiguration configuration, IConfigurationSection profile, string key, int fallback)
    {
        string? value = configuration[key] ?? profile[key];
        return value is null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
