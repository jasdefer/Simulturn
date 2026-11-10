using Simulturn.Core.Model;

namespace Simulturn.Core.Test.Model;

/// <summary>
/// The geometry of <see cref="Hexagon"/>: neighbors, rotation, rings and discs around the origin.
/// </summary>
public class HexagonTest
{
    [Test]
    public void Neighbors_AreTheSixHexagonsAtDistanceOne()
    {
        Hexagon hexagon = new(2, -1);

        var neighbors = hexagon.Neighbors().ToList();

        neighbors.Distinct().Count().ShouldBe(6);
        neighbors.ShouldAllBe(x => x.DistanceTo(hexagon) == 1);
    }

    [Test]
    public void Rotate_ByOneSixth_MovesEveryDirectionToTheNextOne()
    {
        for (int i = 0; i < 6; i++)
        {
            Hexagon.Directions[i].Rotate(1).ShouldBe(Hexagon.Directions[(i + 1) % 6]);
        }
    }

    [Test]
    public void Rotate_ByThreeSixths_MirrorsTheHexagonAtTheOrigin()
    {
        new Hexagon(3, -1).Rotate(3).ShouldBe(new Hexagon(-3, 1));
    }

    [Test]
    public void Rotate_BySixSixths_IsAFullTurn()
    {
        Hexagon hexagon = new(3, -1);

        hexagon.Rotate(6).ShouldBe(hexagon);
        hexagon.Rotate(0).ShouldBe(hexagon);
    }

    [Test]
    public void Rotate_ByANegativeValue_UndoesTheRotation()
    {
        Hexagon hexagon = new(3, -1);

        hexagon.Rotate(2).Rotate(-2).ShouldBe(hexagon);
        hexagon.Rotate(-1).ShouldBe(hexagon.Rotate(5));
    }

    [Test]
    public void Rotate_KeepsTheDistanceToTheOrigin()
    {
        Hexagon hexagon = new(3, -1);

        for (int sixths = 1; sixths < 6; sixths++)
        {
            hexagon.Rotate(sixths).DistanceTo(Hexagon.Origin).ShouldBe(3);
        }
    }

    [Test]
    public void Ring_OfRadiusZero_IsTheOrigin()
    {
        Hexagon.Ring(0).ShouldBe([Hexagon.Origin]);
    }

    [Test]
    public void Ring_WalksAroundAllHexagonsAtTheRadius()
    {
        for (int radius = 1; radius <= 4; radius++)
        {
            var ring = Hexagon.Ring(radius).ToList();

            ring.Distinct().Count().ShouldBe(6 * radius);
            ring.ShouldAllBe(x => x.DistanceTo(Hexagon.Origin) == radius);
            for (int i = 0; i < ring.Count; i++)
            {
                ring[i].DistanceTo(ring[(i + 1) % ring.Count]).ShouldBe(1, $"Radius {radius}, step {i}.");
            }
        }
    }

    [Test]
    public void Disc_ContainsExactlyTheHexagonsWithinTheRadius()
    {
        int[] expectedCounts = [1, 7, 19, 37];
        for (int radius = 0; radius < expectedCounts.Length; radius++)
        {
            var disc = Hexagon.Disc(radius).ToList();

            disc.Distinct().Count().ShouldBe(expectedCounts[radius]);
            disc.ShouldAllBe(x => x.DistanceTo(Hexagon.Origin) <= radius);
        }
    }

    [Test]
    public void RingAndDisc_WithNegativeRadius_Throw()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Hexagon.Ring(-1).ToList());
        Should.Throw<ArgumentOutOfRangeException>(() => Hexagon.Disc(-1).ToList());
    }
}
