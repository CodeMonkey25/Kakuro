using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionMaskTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionMask(candidates);

    [Fact]
    public void Constructor_DeduplicatesCandidates()
    {
        var mask = Create(3, 3, 5);

        Assert.Equal(2, mask.Count);
        Assert.Equal([3, 5], mask.ToArray());
    }

    [Fact]
    public void Enumeration_IsAscending()
    {
        Assert.Equal([2, 5, 8], Create(8, 2, 5).ToArray());
    }

    [Fact]
    public void Add_Duplicate_DoesNotIncreaseCount()
    {
        var mask = Create(4);

        mask.Add(4);

        Assert.Single(mask);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    [InlineData(33)]
    public void Add_WithOutOfRangeCandidate_Throws(int candidate)
    {
        var mask = Create(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => mask.Add(candidate));
        Assert.Equal([1], mask.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Constructor_WithOutOfRangeCandidate_Throws(int candidate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(1, candidate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    [InlineData(33)]
    public void Remove_WithOutOfRangeCandidate_ReturnsFalse(int candidate)
    {
        var mask = Create(1, 9);

        Assert.False(mask.Remove(candidate));
        Assert.Equal([1, 9], mask.ToArray());
    }

    [Fact]
    public void AllCandidates_RoundTripThroughEnumeration()
    {
        var mask = Create(1, 2, 3, 4, 5, 6, 7, 8, 9);

        Assert.Equal(9, mask.Count);
        Assert.Equal(Enumerable.Range(1, 9), mask.ToArray());
    }
}
