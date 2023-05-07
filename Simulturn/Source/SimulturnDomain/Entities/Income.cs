namespace SimulturnDomain.Entities;
public record Income(ushort Gross, ushort Upkeep)
{
    public ushort Net => (ushort)(Gross - Upkeep);
}