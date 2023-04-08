namespace SimulturnCore.UnitTest.Model;

public class ArmyTest
{
    private static readonly Army[] armies = new Army[2]
    {
        new Army(01,02,03,04,05),
        new Army(21,17,13,11,07)
    };

    [Fact]
    public void PlusOperator()
    {
        var result = armies[0] + armies[1];
        result.Triangle.Should().Be(22);
        result.Square.Should().Be(19);
        result.Circle.Should().Be(16);
        result.Line.Should().Be(15);
        result.Point.Should().Be(12);
    }

    [Fact]
    public void Sum()
    {
        var sum = armies[0].Sum();
        sum.Should().Be(15);
    }
}
