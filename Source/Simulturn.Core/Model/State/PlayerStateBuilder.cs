namespace Simulturn.Core.Model.State;
public class PlayerStateBuilder
{
    private PlayerStateBuilder() { }

    public int Matter { get; init; }
    public int UsedSpace { get; init; }
    public int AvailableSpace { get; init; }
    public required ImmutableDictionary<Hexagon, Army>.Builder Armies { get; init; }
    public required ImmutableDictionary<Hexagon, Compound>.Builder Compounds { get; init; }
    public required List<(ushort Turn, Hexagon Hexagon, Army Training)> Trainings { get; init; }
    public required List<(ushort Turn, Hexagon Hexagon, Compound Construction)> Constructions { get; init; }
    public required ImmutableDictionary<Upgrade, byte>.Builder UpgradeLevels { get; init; }

    public PlayerState ToPlayerState()
    {
        throw new NotImplementedException();
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
            Trainings = playerState.Trainings.SelectMany(x => x.Value.Select(y => (x.Key, y.Key, y.Value))).ToList(),
            Constructions = playerState.Constructions.SelectMany(x => x.Value.Select(y => (x.Key, y.Key, y.Value))).ToList(),
            UpgradeLevels = playerState.UpgradeLevels.ToBuilder()
        };
    }
}
