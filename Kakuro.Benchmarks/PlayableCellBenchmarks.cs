using BenchmarkDotNet.Attributes;
using Kakuro.Core.Models;

namespace Kakuro.Benchmarks;

[MemoryDiagnoser]
public class PlayableCellBenchmarks
{
    private static readonly int[] Few = [2, 5, 8];
    private static readonly int[] Many = [1, 2, 3, 4, 5, 6, 7, 8];
    private const uint FewMask = (1u << 1) | (1u << 4) | (1u << 7);
    private const uint ManyMask = 0xFF;

    [Benchmark] public (string?, bool) RemoveEnumerable_Few() => new PlayableCell(0, 0).RemoveCandidates(Few);
    [Benchmark] public (string?, bool) RemoveEnumerable_Many() => new PlayableCell(0, 0).RemoveCandidates(Many);
    [Benchmark] public (string?, bool) RemoveMask_Few() => new PlayableCell(0, 0).RemoveCandidates(FewMask);
    [Benchmark] public (string?, bool) RemoveMask_Many() => new PlayableCell(0, 0).RemoveCandidates(ManyMask);
    [Benchmark] public (string?, bool) RemoveSingle() => new PlayableCell(0, 0).RemoveCandidate(5);
}
