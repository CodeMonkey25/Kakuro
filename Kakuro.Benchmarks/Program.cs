using BenchmarkDotNet.Running;
using Kakuro.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(CandidateCollectionBenchmarks).Assembly).Run(args);
