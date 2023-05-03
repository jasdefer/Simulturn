namespace SimulturnDomain.Entities;
public record Income(Coordinates Coordinates, ushort Gross, ushort Upkeep)
{
    public ushort Nett => (ushort)(Gross - Upkeep);
}