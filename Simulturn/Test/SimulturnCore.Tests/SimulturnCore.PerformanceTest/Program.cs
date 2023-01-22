using BenchmarkDotNet.Running;
using SimulturnCore.PerformanceTest;

var summary = BenchmarkRunner.Run<Benchmark>();