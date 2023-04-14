namespace SimulturnDomain.Entities;
public record Income(ushort Gross, ushort Upkeep)
{
    public ushort Nett => (ushort)(Gross - Upkeep);
}