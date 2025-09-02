namespace Simulturn.Core.Console;
internal static class Extensions
{
    internal static short ToShort(this string value)
    {
        return short.TryParse(value, out short result) ? result : (short)0;
    }
}
