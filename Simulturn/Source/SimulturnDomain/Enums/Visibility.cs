namespace SimulturnDomain.Enums;
public enum Visibility
{
    /// <summary>
    /// The location is currently occupied by friendly units.
    /// Maximum visibility.
    /// </summary>
    Occupied,
    /// <summary>
    /// The location is currently visible, but it is not occupied by friendly units.
    /// </summary>
    Visibile,
    /// <summary>
    /// The location was once visible, but it is currently not visible.
    /// </summary>
    Visited,
    /// <summary>
    /// The location was never visited, visible, or anything else.
    /// No information are available.
    /// </summary>
    NotVisited,
}
