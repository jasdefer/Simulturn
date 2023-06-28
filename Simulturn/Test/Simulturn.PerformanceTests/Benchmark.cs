using BenchmarkDotNet.Attributes;
using SimulturnDomain.Entities;

namespace Simulturn.PerformanceTests;

[MemoryDiagnoser]
public class Benchmark
{
    [Benchmark]
    public int CombineOperator()
    {
        Structure a = new Structure(1, 1, 1, 1, 1, 1);
        Structure b = new Structure(2, 2, 2, 2, 2, 2);
        Structure result = Structure.Combine(a, b, (short x, short y) => Convert.ToInt16(x + y));
        return result.Plane;
    }

    [Benchmark]
    public int PlusOperator()
    {
        Structure a = new Structure(1, 1, 1, 1, 1, 1);
        Structure b = new Structure(2, 2, 2, 2, 2, 2);
        Structure result = a + b;
        return result.Plane;
    }
}