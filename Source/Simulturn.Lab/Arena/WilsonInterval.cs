namespace Simulturn.Lab.Arena;

/// <summary>
/// The Wilson score interval of a rate: how far the true rate may lie from the measured one.
/// Few games give a wide range, so 55 % after 40 games says nothing.
/// </summary>
public static class WilsonInterval
{
    private const double Z95 = 1.959964;

    public static (double Low, double High) Of(int successes, int trials)
    {
        if (trials == 0)
        {
            return (0, 1);
        }
        double rate = (double)successes / trials;
        double z2 = Z95 * Z95;
        double denominator = 1 + z2 / trials;
        double center = (rate + z2 / (2 * trials)) / denominator;
        double margin = Z95 * Math.Sqrt(rate * (1 - rate) / trials + z2 / (4.0 * trials * trials)) / denominator;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }
}
