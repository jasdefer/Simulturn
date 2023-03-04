namespace SimulturnCore.Model.Change;
public class Income
{
    public ushort Gross { get; set; }
    public ushort Upkeep { get; set; }
    public ushort Nett => (ushort)(Gross - Upkeep);
}