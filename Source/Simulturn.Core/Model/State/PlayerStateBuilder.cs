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
    public required ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Upgrade>.Builder>.Builder Researches { get; init; }
    public required ImmutableDictionary<Upgrade, byte>.Builder UpgradeLevels { get; init; }

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
            Researches = Researches.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableDictionary()),
            UpgradeLevels = UpgradeLevels.ToImmutableDictionary(),
            Visibilities = GetVisibility(Armies,
                gameSettings.HexagonSettings.Keys,
                gameSettings.PartialVisibilityRange,
                gameSettings.VisibilityRange)
        };
    }

    public static ImmutableDictionary<Hexagon, Visibility> GetVisibility(IDictionary<Hexagon, Army> armies,
        IEnumerable<Hexagon> hexagons,
        byte partialVisibilityRange,
        byte visibilityRange)
    {
        ImmutableDictionary<Hexagon, Visibility>.Builder visibilities = ImmutableDictionary.CreateBuilder<Hexagon, Visibility>();
        foreach (Hexagon hexagon in hexagons)
        {
            Visibility visibility = Visibility.Mapped;
            foreach ((Hexagon armyHexagon, Army army) in armies)
            {
                if (army.IsEmpty)
                {
                    continue;
                }
                var distance = hexagon.DistanceTo(armyHexagon);
                if (distance == 0)
                {
                    visibility = Visibility.Occupied;
                    break;
                }
                if (distance <= visibilityRange)
                {
                    visibility = Visibility.Visible;
                    continue;
                }
                if (visibility < Visibility.Visible && distance <= partialVisibilityRange)
                {
                    visibility = Visibility.PartiallyVisible;
                }
            }
            if (visibility > Visibility.Unknown)
            {
                visibilities[hexagon] = visibility;
            }
        }
        return visibilities.ToImmutableDictionary();
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
            Researches = playerState.Researches.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToBuilder()).ToBuilder(),
            UpgradeLevels = playerState.UpgradeLevels.ToBuilder()
        };
    }
}
