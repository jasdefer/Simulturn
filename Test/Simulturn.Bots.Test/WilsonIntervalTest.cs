using Simulturn.Lab.Arena;

namespace Simulturn.Bots.Test;

public sealed class WilsonIntervalTest
{
    private const double Tolerance = 0.001;

    [Test]
    public void NoGames_SayNothing()
    {
        WilsonInterval.Of(0, 0).ShouldBe((0, 1));
    }

    [Test]
    public void HalfOfHundredGames_LiesBetweenFortyAndSixtyPercent()
    {
        (double low, double high) = WilsonInterval.Of(50, 100);

        low.ShouldBe(0.404, Tolerance);
        high.ShouldBe(0.596, Tolerance);
    }

    [Test]
    public void MoreGames_NarrowTheRange()
    {
        (double low, double high) = WilsonInterval.Of(5000, 10000);

        low.ShouldBe(0.490, Tolerance);
        high.ShouldBe(0.510, Tolerance);
    }

    [Test]
    public void AllGamesWon_StillLeavesDoubtBelow()
    {
        (double low, double high) = WilsonInterval.Of(10, 10);

        low.ShouldBe(0.722, Tolerance);
        high.ShouldBe(1, Tolerance);
    }

    [Test]
    public void NoGameWon_StillLeavesDoubtAbove()
    {
        (double low, double high) = WilsonInterval.Of(0, 3);

        low.ShouldBe(0, Tolerance);
        high.ShouldBe(0.562, Tolerance);
    }
}
