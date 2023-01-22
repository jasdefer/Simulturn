using BenchmarkDotNet.Attributes;

namespace SimulturnCore.PerformanceTest;

[MemoryDiagnoser]
public class Benchmark
{
    [Benchmark]
    public int AddUshort()
    {
        ushort x = 2;
        ushort y = 7;
        return x + y;
    }

    [Benchmark]
    public int AddInt()
    {
        int x = 2;
        int y = 7;
        return x + y;
    }

    [Benchmark]
    public ushort AddAndCastUShort()
    {
        ushort x = 2;
        ushort y = 7;
        return (ushort)(x + y);
    }



    [Benchmark]
    public ushort AddUshortConvert()
    {
        ushort x = 2;
        ushort y = 7;
        return Convert.ToUInt16(x + y);
    }
}
