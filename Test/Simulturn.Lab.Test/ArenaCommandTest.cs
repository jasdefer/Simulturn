using Microsoft.Extensions.Configuration;
using Simulturn.Bots;
using Simulturn.Lab.Arena;

namespace Simulturn.Lab.Test;

public sealed class ArenaCommandTest
{
    private static readonly Dictionary<string, string?> _profile = new()
    {
        { "Arena:Profiles:quick:Bots:0", "idle" },
        { "Arena:Profiles:quick:Bots:1", "other" },
        { "Arena:Profiles:quick:Maps:0", "Open" },
        { "Arena:Profiles:quick:MapSeeds", "3" },
        { "Arena:Profiles:quick:MaxTurns", "400" },
        { "Arena:Profiles:quick:StallTurns", "50" }
    };

    private static ArenaOptions Read(params (string Key, string Value)[] commandLine)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_profile)
            .AddInMemoryCollection(commandLine.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();
        return ArenaCommand.ReadOptions(configuration, configuration.GetSection("Arena:Profiles:quick"));
    }

    [Test]
    public void Profile_GivesTheOptions()
    {
        ArenaOptions options = Read();

        options.Bots.ShouldBe(["idle", "other"]);
        options.Maps.ShouldBe(["Open"]);
        options.MapSeeds.ShouldBe(3);
        options.Limits.ShouldBe(new MatchLimits() { MaxTurns = 400, StallTurns = 50 });
    }

    [Test]
    public void ValueOnTheCommandLine_ReplacesTheOneOfTheProfile()
    {
        ArenaOptions options = Read(("MapSeeds", "50"), ("Bots", "a, b,c"));

        options.MapSeeds.ShouldBe(50);
        options.Bots.ShouldBe(["a", "b", "c"]);
        options.Maps.ShouldBe(["Open"]);
    }

    [Test]
    [Arguments("MaxTurns", "70000")]
    [Arguments("MaxTurns", "-1")]
    [Arguments("StallTurns", "70000")]
    public void TurnsOutsideTheRangeOfAMatch_Throw(string key, string value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Read((key, value))).ParamName.ShouldBe(key);
    }
}
