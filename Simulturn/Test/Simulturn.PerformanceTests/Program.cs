using BenchmarkDotNet.Running;
using Simulturn.PerformanceTests;

var summary = BenchmarkRunner.Run<Benchmark>();
