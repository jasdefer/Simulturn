using Simulturn.AI.Evaluation;
using Simulturn.AI.Players;

namespace Simulturn.Runner;

/// <summary>
/// Parses player specifications like "Idle", "Random", "Random:Aggressive" or "StateMachine"
/// into arena player factories.
/// </summary>
public static class PlayerCatalog
{
    public static ArenaPlayer Parse(string specification)
    {
        string[] parts = specification.Split(':', 2);
        string kind = parts[0].Trim().ToLowerInvariant();
        string? variant = parts.Length > 1 ? parts[1].Trim() : null;
        Func<int, Simulturn.AI.IArtificialPlayer> factory = kind switch
        {
            "idle" => _ => new IdlePlayer(),
            "random" => seed => new RandomPlayer(seed, ParseRandomOptions(variant)),
            "statemachine" or "sm" => _ => new StateMachinePlayer(),
            _ => throw new ArgumentException($"Unknown player '{specification}'. Known players: Idle, Random[:Uniform|Aggressive|Defensive|Expansive], StateMachine.")
        };
        return new ArenaPlayer()
        {
            Name = specification.Trim(),
            CreatePlayer = factory
        };
    }

    private static RandomPlayerOptions ParseRandomOptions(string? variant)
    {
        return variant?.ToLowerInvariant() switch
        {
            null or "" or "default" => new RandomPlayerOptions(),
            "uniform" => RandomPlayerOptions.Uniform,
            "aggressive" => RandomPlayerOptions.Aggressive,
            "defensive" => RandomPlayerOptions.Defensive,
            "expansive" => RandomPlayerOptions.Expansive,
            _ => throw new ArgumentException($"Unknown random player variant '{variant}'.")
        };
    }
}
