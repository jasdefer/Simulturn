using Simulturn.Core.Model;

namespace Simulturn.Web.Services;

/// <summary>
/// Pure view state shared between the board, the hex detail panel, and the orders sidebar.
/// Kept separate from <see cref="GameSession"/> so the session stays engine-shaped.
/// </summary>
public sealed class GameViewState
{
    public event Action? Changed;

    public Hexagon? SelectedHex { get; private set; }

    /// <summary>Set while the user is picking a movement destination for this source hex.</summary>
    public Hexagon? MovementSource { get; private set; }

    /// <summary>Set once a destination was picked — the movement stepper is open, anchored here.</summary>
    public Hexagon? MovementDestination { get; private set; }

    /// <summary>Hex highlighted because an order row in the sidebar is hovered.</summary>
    public Hexagon? HoveredHex { get; private set; }

    public void SelectHex(Hexagon? hexagon)
    {
        if (SelectedHex != hexagon)
        {
            SelectedHex = hexagon;
            MovementSource = null;
            MovementDestination = null;
            RaiseChanged();
        }
    }

    public void BeginMovement(Hexagon source)
    {
        if (MovementSource != source || MovementDestination is not null)
        {
            MovementSource = source;
            MovementDestination = null;
            RaiseChanged();
        }
    }

    public void PickDestination(Hexagon destination)
    {
        if (MovementSource is not null && MovementDestination != destination)
        {
            MovementDestination = destination;
            RaiseChanged();
        }
    }

    /// <summary>Opens the movement stepper for an existing staged order (arrow / sidebar click).</summary>
    public void EditMovement(Hexagon source, Hexagon destination)
    {
        SelectedHex = source;
        MovementSource = source;
        MovementDestination = destination;
        RaiseChanged();
    }

    public void CancelMovement()
    {
        if (MovementSource is not null || MovementDestination is not null)
        {
            MovementSource = null;
            MovementDestination = null;
            RaiseChanged();
        }
    }

    public void SetHoveredHex(Hexagon? hexagon)
    {
        if (HoveredHex != hexagon)
        {
            HoveredHex = hexagon;
            RaiseChanged();
        }
    }

    public void Reset()
    {
        SelectedHex = null;
        MovementSource = null;
        MovementDestination = null;
        HoveredHex = null;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();
}
