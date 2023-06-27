using BenchmarkDotNet.Attributes;

namespace SimulturnCore.PerformanceTest;

[MemoryDiagnoser]
public class Benchmark
{
    
    [Benchmark]
    public int Method01()
    {
        return 0;
    }

    [Benchmark]
    public int Method02()
    {
        return 1;
    }
}