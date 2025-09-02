namespace Simulturn.Core.Model;
public enum Unit
{
    Dot,
    Triangle,
    Circle,
    Square
}

public enum Building
{
    Plane,
    Axis,
    Dome,
    Pyramid,
    Cube,
}

public enum Upgrade
{
    UpgradeStartMatter,
    DotUpgrade,
}

public enum Visibility
{
    /// <summary>
    /// The hex is completely unknown; the player has no knowledge of it.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The hex is revealed on the map but has never been seen in play.
    /// </summary>
    Mapped = 1,

    /// <summary>
    /// The hex is faintly visible, on the edge of vision.
    /// </summary>
    PartiallyVisible = 2,

    /// <summary>
    /// The hex is clearly visible to the player.
    /// </summary>
    Visible = 3,

    /// <summary>
    /// The hex is fully visible because it is controlled by the player.
    /// </summary>
    Occupied = 4
}