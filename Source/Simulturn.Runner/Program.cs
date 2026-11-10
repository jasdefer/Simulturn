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
    Console.WriteLine("  arena [--players <list>] [--games N] [--max-turns N] [--seed N] [--map <name>] [--radius N] [--matter N] [--map-seed N]");
    Console.WriteLine();
    Console.WriteLine("  --players    Comma separated player specs (default: Commander,StateMachine,Random,Random:Aggressive,Idle)");
    Console.WriteLine("               Known: Idle, Random[:Uniform|Aggressive|Defensive|Expansive], StateMachine, Commander");
    Console.WriteLine("  --games      Games per pairing and seat (default 10, so 20 games per pairing)");
    Console.WriteLine("  --max-turns  Turn limit per game, reaching it is a draw (default 200)");
    Console.WriteLine("  --seed       Base seed (default 0)");
    Console.WriteLine("  --map        Map type: open (default), homeonly, expansions");
    Console.WriteLine("  --radius     Map radius (default: the radius of the map type)");
    Console.WriteLine("  --matter     Matter per hexagon, start hexagons included (default: the matter of the map type)");
    Console.WriteLine("  --map-seed   Seed that places the expansions (default 1)");
    return 1;
}
