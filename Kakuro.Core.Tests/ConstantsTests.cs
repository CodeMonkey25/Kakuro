namespace Kakuro.Core.Tests;

public class ConstantsTests
{
    [Fact]
    public void CandidateBounds_AreOneThroughNine_AndUnsolvedIsOutsideThem()
    {
        Assert.Equal(1, Constants.MinCandidate);
        Assert.Equal(9, Constants.MaxCandidate);
        Assert.NotInRange(Constants.UnsolvedCell, Constants.MinCandidate, Constants.MaxCandidate);
    }

    [Fact]
    public void AllCandidates_ContainsEveryDigitFromMinToMax()
    {
        Assert.Equal(Enumerable.Range(Constants.MinCandidate, Constants.MaxRunLength), Constants.AllCandidates);
    }

    [Fact]
    public void MinPossibleSum_IsTheSumOfTheSmallestDistinctDigits()
    {
        Assert.Equal(Constants.MaxRunLength + 1, Constants.MinPossibleSum.Length);
        for (int length = 0; length <= Constants.MaxRunLength; length++)
        {
            Assert.Equal(Enumerable.Range(1, length).Sum(), Constants.MinPossibleSum[length]);
        }
    }

    [Fact]
    public void MaxPossibleSum_IsTheSumOfTheLargestDistinctDigits()
    {
        Assert.Equal(Constants.MaxRunLength + 1, Constants.MaxPossibleSum.Length);
        for (int length = 0; length <= Constants.MaxRunLength; length++)
        {
            Assert.Equal(Enumerable.Range(10 - length, length).Sum(), Constants.MaxPossibleSum[length]);
        }
    }
}
