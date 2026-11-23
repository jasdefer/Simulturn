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

    private static IConfiguration Configuration(params (string Key, string Value)[] commandLine)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(_profile)
            .AddInMemoryCollection(commandLine.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();
    }

    private static ArenaOptions Read(params (string Key, string Value)[] commandLine)
    {
        return ArenaCommand.ReadProfile(Configuration(commandLine), TextWriter.Null).ShouldNotBeNull().Options;
    }

    [Test]
    public void NoProfileNamed_GivesTheDefaultProfile()
    {
        ArenaProfile? profile = ArenaCommand.ReadProfile(Configuration(), TextWriter.Null);

        profile.ShouldNotBeNull().Name.ShouldBe(ArenaCommand.DefaultProfile);
    }

    [Test]
    public void UnknownProfile_GivesNothingAndNamesTheKnownOnes()
    {
        StringWriter output = new();

        ArenaProfile? profile = ArenaCommand.ReadProfile(Configuration(("profile", "missing")), output);

        profile.ShouldBeNull();
        output.ToString().ShouldContain("'missing'");
        output.ToString().ShouldContain("quick");
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
