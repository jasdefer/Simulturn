using Simulturn.Core.Model;
using Simulturn.Core.Model.State;

namespace Simulturn.Core.Engine;

internal sealed class PlayerStateBuilder
{
    private PlayerStateBuilder() { }

    public int Matter { get; set; }
    public int UsedSpace { get; set; }
    public int AvailableSpace { get; set; }
    public required ImmutableDictionary<Hexagon, Army>.Builder Armies { get; init; }
    public required ImmutableDictionary<Hexagon, Compound>.Builder Compounds { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>.Builder>.Builder Trainings { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>.Builder Constructions { get; init; }
    public required ImmutableDictionary<Hexagon, Research>.Builder Researches { get; init; }
    public required ImmutableDictionary<Upgrade, byte>.Builder UpgradeLevels { get; init; }
    public required ImmutableDictionary<Hexagon, Army>.Builder Losses { get; init; }

    /// <summary>
    /// Carried over unchanged; memories are updated by <see cref="FogOfWar.UpdateMemories"/> once all player states of a turn are resolved.
    /// </summary>
    public required ImmutableDictionary<Hexagon, HexagonMemory> Memories { get; init; }

    /// <summary>
    /// Reset every turn like the losses; filled during the fight resolution.
    /// </summary>
    public Dictionary<Hexagon, Dictionary<string, Army>> RevealedArmies { get; } = [];

    public PlayerState ToPlayerState(GameSettings gameSettings)
    {
        return new PlayerState()
        {
            Matter = Matter,
            UsedSpace = UsedSpace,
            AvailableSpace = AvailableSpace,
            Armies = Armies.ToImmutableDictionary(),
            Compounds = Compounds.ToImmutableDictionary(),
            Trainings = Trainings.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableDictionary()),
            Constructions = Constructions.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableDictionary()),
            Researches = Researches.ToImmutableDictionary(),
            UpgradeLevels = UpgradeLevels.ToImmutableDictionary(),
            Visibilities = FogOfWar.GetVisibility(Armies,
                gameSettings.HexagonSettings.Keys,
                gameSettings.PartialVisibilityRange,
                gameSettings.VisibilityRange),
            Losses = Losses.ToImmutableDictionary(),
            Memories = Memories,
            RevealedArmies = RevealedArmies.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableDictionary())
        };
    }

    public static PlayerStateBuilder FromPlayerState(PlayerState playerState)
    {
        return new PlayerStateBuilder
        {
            Matter = playerState.Matter,
            UsedSpace = playerState.UsedSpace,
            AvailableSpace = playerState.AvailableSpace,
            Armies = playerState.Armies.ToBuilder(),
            Compounds = playerState.Compounds.ToBuilder(),
            Trainings = playerState.Trainings.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToBuilder()).ToBuilder(),
            Constructions = playerState.Constructions.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToBuilder()).ToBuilder(),
            Researches = playerState.Researches.ToBuilder(),
            UpgradeLevels = playerState.UpgradeLevels.ToBuilder(),
            Losses = ImmutableDictionary<Hexagon, Army>.Empty.ToBuilder(),
            Memories = playerState.Memories
        };
    }
}
