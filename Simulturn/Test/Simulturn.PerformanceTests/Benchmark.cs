using BenchmarkDotNet.Attributes;
using SimulturnDomain.Entities;

namespace Simulturn.PerformanceTests;

[MemoryDiagnoser]
public class Benchmark
{
    [Benchmark]
    public short CastShort()
    {
        short a = 1;
        short b = 2;
        short result = (short)(a + b);
        return result;
    }

    [Benchmark]
    public short ConvertShort()
    {
        short a = 1;
        short b = 2;
        short result = Convert.ToInt16(a + b);
        return result;
    }
}