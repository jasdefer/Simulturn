namespace Simulturn.Core.Model.State;
public class PlayerStateBuilder
{
    private PlayerStateBuilder() { }

    public int Matter { get; set; }
    public int UsedSpace { get; set; }
    public int AvailableSpace { get; set; }
    public required ImmutableDictionary<Hexagon, Army>.Builder Armies { get; init; }
    public required ImmutableDictionary<Hexagon, Compound>.Builder Compounds { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>.Builder>.Builder Trainings { get; init; }
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Compound>.Builder>.Builder Constructions { get; init; }
    public required ImmutableDictionary<Upgrade, byte>.Builder UpgradeLevels { get; init; }

    public PlayerState ToPlayerState()
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
            UpgradeLevels = playerState.UpgradeLevels.ToBuilder()
        };
    }
}
