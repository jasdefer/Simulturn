using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Simulturn.Bots;
using Simulturn.Bots.Catalog;

namespace Simulturn.Lab.Arena;

/// <summary>
/// The <c>arena</c> study of the lab: reads a profile from the configuration, plays the run,
/// prints the table and writes the result to a file.
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
        output.Write(ArenaTable.Render(result));

        string folder = configuration["OutputFolder"] ?? "measurements";
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"arena-{profile}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(result, _json));
        output.WriteLine();
        output.WriteLine($"Written to {Path.GetFullPath(file)}");
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
                MaxTurns = (ushort)ReadNumber(configuration, profile, "MaxTurns", defaults.MaxTurns),
                StallTurns = (ushort)ReadNumber(configuration, profile, "StallTurns", defaults.StallTurns)
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

    private static int ReadNumber(IConfiguration configuration, IConfigurationSection profile, string key, int fallback)
    {
        string? value = configuration[key] ?? profile[key];
        return value is null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
