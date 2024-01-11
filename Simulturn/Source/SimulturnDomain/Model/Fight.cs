using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;
public record Fight(Army Army, Army Losses)
{
    public Army Surviver => Army - Losses;
}