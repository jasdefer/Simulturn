using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using System.Collections.Immutable;

namespace Simulturn.Web.Models;

/// <summary>
/// Mutable mirror of the engine's immutable <see cref="Command"/> for form binding.
/// Fields are <see cref="int"/> for clean numeric-input binding and clamped to
/// <see cref="short"/> range on conversion.
/// </summary>
public sealed class DraftCommand
{
    public CompoundDraft Construction { get; } = new();
    public List<DraftMovement> Movements { get; } = [];
    public ArmyDraft Training { get; } = new();
    public Upgrade? Upgrade { get; set; }

    public bool IsEmpty =>
        Construction.IsEmpty &&
        Training.IsEmpty &&
        Upgrade is null &&
        Movements.All(movement => movement.Army.IsEmpty);

    public Command ToCommand() => new()
    {
        Construction = Construction.ToCompound(),
        Training = Training.ToArmy(),
        Upgrade = Upgrade,
        MovementCommands = Movements
            .Where(movement => !movement.Army.IsEmpty)
            .Select(movement => new MovementCommand { Destination = movement.Destination, Army = movement.Army.ToArmy() })
            .ToImmutableArray(),
    };

    /// <summary>Stages a movement; a second order to the same destination overwrites the first.</summary>
    public void UpsertMovement(Hexagon destination, ArmyDraft army)
    {
        var existing = Movements.FirstOrDefault(movement => movement.Destination == destination);
        if (existing is not null)
        {
            Movements.Remove(existing);
        }
        if (!army.IsEmpty)
        {
            Movements.Add(new DraftMovement(destination, army));
        }
    }
}

public sealed class DraftMovement(Hexagon destination, ArmyDraft army)
{
    public Hexagon Destination { get; } = destination;
    public ArmyDraft Army { get; } = army;
}

public sealed class ArmyDraft
{
    public int Triangle { get; set; }
    public int Circle { get; set; }
    public int Square { get; set; }
    public int Dot { get; set; }

    public bool IsEmpty => Triangle == 0 && Circle == 0 && Square == 0 && Dot == 0;

    public int this[Unit unit]
    {
        get => unit switch
        {
            Unit.Triangle => Triangle,
            Unit.Circle => Circle,
            Unit.Square => Square,
            Unit.Dot => Dot,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
        };
        set
        {
            switch (unit)
            {
                case Unit.Triangle: Triangle = value; break;
                case Unit.Circle: Circle = value; break;
                case Unit.Square: Square = value; break;
                case Unit.Dot: Dot = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }
    }

    public Army ToArmy() => new()
    {
        Triangle = Clamp(Triangle),
        Circle = Clamp(Circle),
        Square = Clamp(Square),
        Dot = Clamp(Dot),
    };

    public static ArmyDraft From(Army army) => new()
    {
        Triangle = army.Triangle,
        Circle = army.Circle,
        Square = army.Square,
        Dot = army.Dot,
    };

    private static short Clamp(int value) => (short)Math.Clamp(value, 0, short.MaxValue);
}

public sealed class CompoundDraft
{
    public int Dome { get; set; }
    public int Pyramid { get; set; }
    public int Cube { get; set; }
    public int Plane { get; set; }
    public int Axis { get; set; }

    public bool IsEmpty => Dome == 0 && Pyramid == 0 && Cube == 0 && Plane == 0 && Axis == 0;

    public int this[Building building]
    {
        get => building switch
        {
            Building.Plane => Plane,
            Building.Axis => Axis,
            Building.Dome => Dome,
            Building.Pyramid => Pyramid,
            Building.Cube => Cube,
            _ => throw new ArgumentOutOfRangeException(nameof(building), building, null),
        };
        set
        {
            switch (building)
            {
                case Building.Plane: Plane = value; break;
                case Building.Axis: Axis = value; break;
                case Building.Dome: Dome = value; break;
                case Building.Pyramid: Pyramid = value; break;
                case Building.Cube: Cube = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(building), building, null);
            }
        }
    }

    public Compound ToCompound() => new()
    {
        Dome = Clamp(Dome),
        Pyramid = Clamp(Pyramid),
        Cube = Clamp(Cube),
        Plane = Clamp(Plane),
        Axis = Clamp(Axis),
    };

    private static short Clamp(int value) => (short)Math.Clamp(value, 0, short.MaxValue);
}
