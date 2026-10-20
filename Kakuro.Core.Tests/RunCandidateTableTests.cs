using System.Collections.Immutable;

namespace Kakuro.Core.Tests;

public class RunCandidateTableTests
{
    private const uint AllMask = 0x1FF;

    private static uint MaskOf(IEnumerable<int> digits) => digits.Aggregate(0u, (mask, digit) => mask | 1u << (digit - 1));

    // every set of `length` distinct digits 1..9 that sums to `total`
    private static List<int[]> DigitSets(int length, int total)
    {
        List<int[]> sets = [];
        for (int bits = 1; bits <= AllMask; bits++)
        {
            int[] digits = Enumerable.Range(1, 9).Where(d => (bits & 1 << (d - 1)) != 0).ToArray();
            if (digits.Length == length && digits.Sum() == total) sets.Add(digits);
        }
        return sets;
    }

    [Fact]
    public void InclusionEntries_EachHasLengthDigitsThatSumToTheTotal()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidateTable.InclusionEntries)
        {
            Assert.Equal(length, digits.Length);
            Assert.Equal(total, digits.Sum());
            Assert.Equal(digits.Length, digits.Distinct().Count());
            Assert.All(digits, d => Assert.InRange(d, Constants.MinCandidate, Constants.MaxCandidate));
        }
    }

    [Fact]
    public void InclusionEntries_CoverExactlyTheRunsWithOneDigitSet()
    {
        for (int length = 1; length <= 9; length++)
        {
            for (int total = 0; total <= 45; total++)
            {
                List<int[]> sets = DigitSets(length, total);
                bool inTable = RunCandidateTable.InclusionEntries.TryGetValue((length, total), out ImmutableArray<int> digits);

                Assert.Equal(sets.Count == 1, inTable);
                if (inTable) Assert.Equal(sets[0], digits.OrderBy(d => d));
            }
        }
    }

    [Fact]
    public void ExclusionEntries_HaveTheSameKeysAsInclusionEntries()
    {
        Assert.Equal(
            RunCandidateTable.InclusionEntries.Keys.OrderBy(k => k),
            RunCandidateTable.ExclusionEntries.Keys.OrderBy(k => k));
    }

    [Fact]
    public void ExclusionEntries_EachIsTheComplementOfTheInclusionEntryDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidateTable.InclusionEntries)
        {
            Assert.Equal(~MaskOf(digits) & AllMask, RunCandidateTable.ExclusionEntries[(length, total)]);
        }
    }

    [Fact]
    public void GetRunExclusionMask_ForListedRun_IsTheComplementOfItsDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidateTable.InclusionEntries)
        {
            Assert.Equal(~MaskOf(digits) & AllMask, RunCandidateTable.GetRunExclusionMask(length, total));
        }
    }

    [Theory]
    [InlineData(2, 3, new[] { 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(2, 17, new[] { 1, 2, 3, 4, 5, 6, 7 })]
    [InlineData(1, 5, new[] { 1, 2, 3, 4, 6, 7, 8, 9 })]
    [InlineData(3, 24, new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(8, 36, new[] { 9 })]
    public void GetRunExclusionMask_KnownRuns_ExcludesTheOtherDigits(int length, int total, int[] excluded)
    {
        Assert.Equal(MaskOf(excluded), RunCandidateTable.GetRunExclusionMask(length, total));
    }

    [Theory]
    [InlineData(2, 9)]   // several digit sets: 1+8, 2+7, 3+6, 4+5
    [InlineData(3, 15)]
    [InlineData(2, 2)]   // impossible
    [InlineData(2, 18)]  // impossible
    [InlineData(4, 0)]
    public void GetRunExclusionMask_WhenRunIsNotUnique_ReturnsZero(int length, int total)
    {
        Assert.Equal(0u, RunCandidateTable.GetRunExclusionMask(length, total));
    }

    [Fact]
    public void GetRunExclusionMask_NineCellRun_ExcludesNothing()
    {
        Assert.Equal(0u, RunCandidateTable.GetRunExclusionMask(9, 45));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(10, 45)]
    [InlineData(-1, 3)]
    [InlineData(2, -1)]
    [InlineData(2, 46)]
    [InlineData(2, int.MaxValue)]
    public void GetRunExclusionMask_OutOfRange_ReturnsZeroWithoutThrowing(int length, int total)
    {
        Assert.Equal(0u, RunCandidateTable.GetRunExclusionMask(length, total));
    }
}
