using Simulturn.AI;
using Simulturn.AI.Players;
using System.Collections.Immutable;

namespace Simulturn.Web.Services;

/// <summary>
/// The AI opponents a human can pick when starting a game, with display metadata and a factory.
/// Mirrors the choices in <c>Simulturn.Runner.PlayerCatalog</c> but geared for UI selection.
/// </summary>
public sealed record AiKind(string Id, string Name, string Description, Func<int, IArtificialPlayer> Create)
{
    /// <summary>The human option — no AI; the person at the keyboard issues the orders.</summary>
    public const string HumanId = "human";
}

public static class AiCatalog
{
    public static ImmutableArray<AiKind> All { get; } =
    [
        new("commander", "Commander", "Strongest built-in AI: expands, defends its base, and pushes counter-composition attacks.",
            _ => new CommanderPlayer()),
        new("tactician", "Tactician", "Focuses on winning fights with the right counter units.",
            _ => new TacticianPlayer()),
        new("simulator", "Simulator", "Rolls out believed enemy states to choose a turn.",
            _ => new SimulatorPlayer()),
        new("statemachine", "State machine", "Straightforward economy-then-army opponent. A solid baseline.",
            _ => new StateMachinePlayer()),
        new("random-aggressive", "Random (aggressive)", "Legal random moves biased toward attacking.",
            seed => new RandomPlayer(seed, RandomPlayerOptions.Aggressive)),
        new("random-defensive", "Random (defensive)", "Legal random moves biased toward defending.",
            seed => new RandomPlayer(seed, RandomPlayerOptions.Defensive)),
        new("random-expansive", "Random (expansive)", "Legal random moves biased toward expansion.",
            seed => new RandomPlayer(seed, RandomPlayerOptions.Expansive)),
        new("random", "Random", "Uniformly random legal moves. Good for stress-testing.",
            seed => new RandomPlayer(seed, RandomPlayerOptions.Uniform)),
        new("idle", "Idle", "Does nothing. A punching bag for testing.",
            _ => new IdlePlayer()),
    ];

    public static AiKind? Find(string id) => All.FirstOrDefault(kind => kind.Id == id);
}
