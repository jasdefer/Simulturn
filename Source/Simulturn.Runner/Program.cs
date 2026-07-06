using Simulturn.Runner;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

Dictionary<string, string> arguments = [];
for (int i = 1; i < args.Length; i++)
{
    if (args[i].StartsWith("--") && i + 1 < args.Length)
    {
        arguments[args[i][2..]] = args[i + 1];
        i++;
    }
}

return args[0].ToLowerInvariant() switch
{
    "arena" => ArenaWorker.Run(arguments),
    _ => PrintUsage()
};

static int PrintUsage()
{
    Console.WriteLine("Simulturn Runner - simulation tools");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  arena [--players <list>] [--games N] [--max-turns N] [--seed N] [--radius N] [--matter N]");
    Console.WriteLine();
    Console.WriteLine("  --players    Comma separated player specs (default: StateMachine,Random,Random:Aggressive,Idle)");
    Console.WriteLine("               Known: Idle, Random[:Uniform|Aggressive|Defensive|Expansive], StateMachine");
    Console.WriteLine("  --games      Games per pairing and seat (default 10, so 20 games per pairing)");
    Console.WriteLine("  --max-turns  Turn limit per game, reaching it is a draw (default 200)");
    Console.WriteLine("  --seed       Base seed (default 0)");
    Console.WriteLine("  --radius     Map radius of the hexagon disc (default 3)");
    Console.WriteLine("  --matter     Harvestable matter per hexagon (default 1500)");
    return 1;
}
