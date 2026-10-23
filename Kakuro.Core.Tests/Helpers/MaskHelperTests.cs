using Kakuro.Core.Helpers;

namespace Kakuro.Core.Tests.Helpers;

public class MaskHelperTests
{
    #region BitFor

    [Theory]
    [InlineData(0, 0b1u)]
    [InlineData(1, 0b10u)]
    [InlineData(8, 0b1_0000_0000u)]
    [InlineData(31, 0x8000_0000u)]
    public void BitFor_ReturnsSingleBitAtPosition(int value, uint expected)
    {
        Assert.Equal(expected, MaskHelper.BitFor(value));
    }

    #endregion

    #region BitForCandidate

    [Theory]
    [InlineData(1, 0b1u)]
    [InlineData(2, 0b10u)]
    [InlineData(9, 0b1_0000_0000u)]
    public void BitForCandidate_MapsCandidateToZeroBasedBit(int candidate, uint expected)
    {
        Assert.Equal(expected, MaskHelper.BitForCandidate(candidate));
    }

    [Fact]
    public void BitForCandidate_EveryCandidate_ReturnsDistinctSingleBit()
    {
        uint seen = 0;
        foreach (int candidate in Constants.AllCandidates)
        {
            uint bit = MaskHelper.BitForCandidate(candidate);
            Assert.Equal(1u, uint.PopCount(bit));
            Assert.Equal(0u, seen & bit);
            seen |= bit;
        }
        Assert.Equal(Constants.AllCandidatesMask, seen);
    }

    [Theory]
    [InlineData(Constants.MinCandidate - 1)]
    [InlineData(Constants.MaxCandidate + 1)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void BitForCandidate_OutOfRange_Throws(int candidate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskHelper.BitForCandidate(candidate));
    }

    #endregion

    #region MaskFor

    [Fact]
    public void MaskFor_Empty_ReturnsZero()
    {
        Assert.Equal(0u, MaskHelper.MaskFor([]));
    }

    [Fact]
    public void MaskFor_CombinesBitsAtGivenPositions()
    {
        Assert.Equal(0b101u | (1u << 31), MaskHelper.MaskFor([0, 2, 31]));
    }

    [Fact]
    public void MaskFor_DuplicateValues_AreIdempotent()
    {
        Assert.Equal(0b110u, MaskHelper.MaskFor([1, 2, 1, 2]));
    }

    #endregion

    #region MaskForCandidates

    [Fact]
    public void MaskForCandidates_Empty_ReturnsZero()
    {
        Assert.Equal(0u, MaskHelper.MaskForCandidates([]));
    }

    [Fact]
    public void MaskForCandidates_CombinesCandidateBits()
    {
        Assert.Equal(0b1_0000_0101u, MaskHelper.MaskForCandidates([1, 3, 9]));
    }

    [Fact]
    public void MaskForCandidates_DuplicateCandidates_AreIdempotent()
    {
        Assert.Equal(MaskHelper.MaskForCandidates([2, 5]), MaskHelper.MaskForCandidates([5, 2, 5, 2]));
    }

    [Fact]
    public void MaskForCandidates_AllCandidates_EqualsAllCandidatesMask()
    {
        Assert.Equal(Constants.AllCandidatesMask, MaskHelper.MaskForCandidates(Constants.AllCandidates));
    }

    [Fact]
    public void MaskForCandidates_AcceptsLazyEnumerable()
    {
        Assert.Equal(0b111u, MaskHelper.MaskForCandidates(Enumerable.Range(1, 3)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void MaskForCandidates_AnyOutOfRangeCandidate_Throws(int bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskHelper.MaskForCandidates([1, bad, 3]));
    }

    #endregion
}
