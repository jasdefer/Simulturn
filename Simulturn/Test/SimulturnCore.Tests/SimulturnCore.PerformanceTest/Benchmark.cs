using BenchmarkDotNet.Attributes;

namespace SimulturnCore.PerformanceTest;

[MemoryDiagnoser]
public class Benchmark
{
    public struct ValuesContainer
    {
        public ValuesContainer(int a, int b, int c, int d, int e)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
        }

        public int A { get; set; } = 1;
        public int B { get; set; } = 2;
        public int C { get; set; } = 3;
        public int D { get; set; } = 4;
        public int E { get; set; } = 5;

    }
    [Benchmark]
    public int WithStruct()
    {
        var jo = new ValuesContainer(1, 2, 3, 4, 5);
        var sum = jo.A + jo.B + jo.C + jo.D + jo.E;

        return sum;
    }

    [Benchmark]
    public int Array()
    {
        var array = new int[] { 1, 2, 3, 4, 5 };
        var sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }
        return sum;
    }

    [Benchmark]
    public int Span()
    {
        Span<int> span = stackalloc[] { 1, 2, 3, 4, 5 };
        var sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }
        return sum;
    }
}