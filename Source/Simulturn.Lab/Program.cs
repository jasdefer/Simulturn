using Microsoft.Extensions.Configuration;
using Simulturn.Lab.Arena;
using Simulturn.Lab.Replay;

// The lab runs what is not a test: studies that take a while and whose result a human reads.
// Usage: Simulturn.Lab <study> [--profile <name>] [--<Key> <value> ...]
// A study reads its profile from appsettings.json; a value on the command line replaces it.

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: Simulturn.Lab <study> [--profile <name>] [--<Key> <value> ...]");
    Console.Error.WriteLine("Studies: arena, replay");
    return 2;
}

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddCommandLine(args[1..])
    .Build();

try
{
    switch (args[0])
    {
        case "arena":
            return ArenaCommand.Run(configuration, Console.Out);
        case "replay":
            return ReplayCommand.Run(configuration, Console.Out);
        default:
            Console.Error.WriteLine($"There is no study '{args[0]}'. Studies: arena, replay");
            return 2;
    }
}
catch (Exception exception) when (exception is ArenaGameException or ArgumentException or FormatException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
