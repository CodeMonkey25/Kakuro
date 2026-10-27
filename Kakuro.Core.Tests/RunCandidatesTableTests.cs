using System.Collections.Immutable;
using Kakuro.Core.Helpers;

namespace Kakuro.Core.Tests;

public class RunCandidatesTableTests
{
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
    public void GetRunExclusions_ForListedRun_IsTheComplementOfItsDigits()
    {
        foreach (((int length, int total), ImmutableArray<int> digits) in RunCandidatesTable.InclusionEntries)
        {
            Assert.Equal(~MaskHelper.MaskForCandidates(digits) & Constants.AllCandidatesMask, RunCandidatesTable.GetRunExclusions(length, total).mask);
        }
    }

    [Theory]
    [InlineData(2, 3, new[] { 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(2, 17, new[] { 1, 2, 3, 4, 5, 6, 7 })]
    [InlineData(1, 5, new[] { 1, 2, 3, 4, 6, 7, 8, 9 })]
    [InlineData(3, 24, new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(8, 36, new[] { 9 })]
    public void GetRunExclusions_KnownRuns_ExcludesTheOtherDigits(int length, int total, int[] excluded)
    {
        Assert.Equal(MaskHelper.MaskForCandidates(excluded), RunCandidatesTable.GetRunExclusions(length, total).mask);
    }

    [Theory]
    [InlineData(2, 9)]   // several digit sets: 1+8, 2+7, 3+6, 4+5
    [InlineData(3, 15)]
    [InlineData(2, 2)]   // impossible
    [InlineData(2, 18)]  // impossible
    [InlineData(4, 0)]
    public void GetRunExclusions_WhenRunIsNotUnique_ReturnsDefault(int length, int total)
    {
        Assert.Equal(default, RunCandidatesTable.GetRunExclusions(length, total));
    }

    [Fact]
    public void GetRunExclusions_NineCellRun_ExcludesNothing()
    {
        Assert.Equal(0u, RunCandidatesTable.GetRunExclusions(9, 45).mask);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(10, 45)]
    [InlineData(-1, 3)]
    [InlineData(2, -1)]
    [InlineData(2, 46)]
    [InlineData(2, int.MaxValue)]
    public void GetRunExclusions_OutOfRange_ReturnsZeroWithoutThrowing(int length, int total)
    {
        Assert.Equal(0u, RunCandidatesTable.GetRunExclusions(length, total).mask);
    }
}
