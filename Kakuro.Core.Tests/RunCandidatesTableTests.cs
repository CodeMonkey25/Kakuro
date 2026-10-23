using System.Collections.Immutable;

namespace Kakuro.Core.Tests;

public class RunCandidatesTableTests
{
    private static uint MaskOf(IEnumerable<int> digits) => digits.Aggregate(0u, (mask, digit) => mask | 1u << (digit - 1));

    // every set of `length` distinct digits 1..9 that sums to `total`
    private static List<int[]> DigitSets(int length, int total)
    {
        List<int[]> sets = [];
        for (int bits = 1; bits <= Constants.AllCandidatesMask; bits++)
        {
            int[] digits = Enumerable.Range(1, 9).Where(d => (bits & 1 << (d - 1)) != 0).ToArray();
            if (digits.Length == length && digits.Sum() == total) sets.Add(digits);
        }
        return sets;
    }

    [Fact]
    public void InclusionEntries_EachHasLengthDigitsThatSumToTheTotal()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidatesTable.InclusionEntries)
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
                bool inTable = RunCandidatesTable.InclusionEntries.TryGetValue((length, total), out ImmutableArray<int> digits);

                Assert.Equal(sets.Count == 1, inTable);
                if (inTable) Assert.Equal(sets[0], digits.Order());
            }
        }
    }

    [Fact]
    public void ExclusionEntries_HaveTheSameKeysAsInclusionEntries()
    {
        Assert.Equal(
            RunCandidatesTable.InclusionEntries.Keys.Order(),
            RunCandidatesTable.ExclusionEntries.Keys.Order());
    }

    [Fact]
    public void ExclusionEntries_EachIsTheComplementOfTheInclusionEntryDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidatesTable.InclusionEntries)
        {
            Assert.Equal(Constants.AllCandidates.Except(digits), RunCandidatesTable.ExclusionEntries[(length, total)]);
        }
    }

    [Fact]
    public void GetRunExclusionMask_ForListedRun_IsTheComplementOfItsDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidatesTable.InclusionEntries)
        {
            Assert.Equal(~MaskOf(digits) & Constants.AllCandidatesMask, RunCandidatesTable.GetRunExclusionMask(length, total));
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
        Assert.Equal(MaskOf(excluded), RunCandidatesTable.GetRunExclusionMask(length, total));
    }

    [Theory]
    [InlineData(2, 9)]   // several digit sets: 1+8, 2+7, 3+6, 4+5
    [InlineData(3, 15)]
    [InlineData(2, 2)]   // impossible
    [InlineData(2, 18)]  // impossible
    [InlineData(4, 0)]
    public void GetRunExclusionMask_WhenRunIsNotUnique_ReturnsZero(int length, int total)
    {
        Assert.Equal(0u, RunCandidatesTable.GetRunExclusionMask(length, total));
    }

    [Fact]
    public void GetRunExclusionMask_NineCellRun_ExcludesNothing()
    {
        Assert.Equal(0u, RunCandidatesTable.GetRunExclusionMask(9, 45));
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
        Assert.Equal(0u, RunCandidatesTable.GetRunExclusionMask(length, total));
    }

    [Fact]
    public void GetRunInclusions_ForListedRun_ReturnsItsDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidatesTable.InclusionEntries)
        {
            Assert.Equal(digits, RunCandidatesTable.GetRunInclusions(length, total));
        }
    }

    [Theory]
    [InlineData(2, 3, new[] { 1, 2 })]
    [InlineData(2, 17, new[] { 8, 9 })]
    [InlineData(1, 5, new[] { 5 })]
    [InlineData(3, 24, new[] { 7, 8, 9 })]
    [InlineData(8, 36, new[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
    [InlineData(9, 45, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
    public void GetRunInclusions_KnownRuns_ReturnsTheDigits(int length, int total, int[] expected)
    {
        Assert.Equal(expected, RunCandidatesTable.GetRunInclusions(length, total));
    }

    [Fact]
    public void GetRunExclusions_MatchesTheDigitsOfGetRunExclusionMask_ForListedRuns()
    {
        foreach ((int length, int total) in RunCandidatesTable.InclusionEntries.Keys)
        {
            Assert.Equal(MaskOf(RunCandidatesTable.GetRunExclusions(length, total)), RunCandidatesTable.GetRunExclusionMask(length, total));
        }
    }

    [Theory]
    [InlineData(2, 3, new[] { 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(1, 5, new[] { 1, 2, 3, 4, 6, 7, 8, 9 })]
    [InlineData(8, 36, new[] { 9 })]
    [InlineData(9, 45, new int[0])]
    public void GetRunExclusions_KnownRuns_ReturnsTheOtherDigits(int length, int total, int[] expected)
    {
        Assert.Equal(expected, RunCandidatesTable.GetRunExclusions(length, total));
    }

    [Theory]
    [InlineData(2, 9)]
    [InlineData(0, 3)]
    [InlineData(2, int.MaxValue)]
    public void GetRunExclusions_WhenRunIsNotListed_ReturnsEmptyWithoutThrowing(int length, int total)
    {
        Assert.Empty(RunCandidatesTable.GetRunExclusions(length, total));
    }

    [Fact]
    public void GetRunInclusions_IsTheComplementOfGetRunExclusionMask_ForListedRuns()
    {
        foreach ((int length, int total) in RunCandidatesTable.InclusionEntries.Keys)
        {
            Assert.Equal(~RunCandidatesTable.GetRunExclusionMask(length, total) & Constants.AllCandidatesMask, MaskOf(RunCandidatesTable.GetRunInclusions(length, total)));
        }
    }

    [Theory]
    [InlineData(2, 9)]
    [InlineData(3, 15)]
    [InlineData(2, 2)]
    [InlineData(2, 18)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    [InlineData(10, 45)]
    [InlineData(-1, 3)]
    [InlineData(2, -1)]
    [InlineData(2, 46)]
    [InlineData(2, int.MaxValue)]
    public void GetRunInclusions_WhenRunIsNotListed_ReturnsEmptyWithoutThrowing(int length, int total)
    {
        Assert.Empty(RunCandidatesTable.GetRunInclusions(length, total));
    }
}
