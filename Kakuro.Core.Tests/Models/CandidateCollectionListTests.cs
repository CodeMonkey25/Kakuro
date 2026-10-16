using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionListTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionList(candidates);

    [Fact]
    public void Constructor_WithEmptySequence_IsEmpty()
    {
        Assert.Empty(new CandidateCollectionList([]));
    }

    [Fact]
    public void Constructor_CopiesTheSourceSequence()
    {
        List<int> source = [1, 2];
        var collection = new CandidateCollectionList(source);

        source.Add(3);
        collection.Remove(1);

        Assert.Equal([2], collection.ToArray());
        Assert.Equal([1, 2, 3], source);
    }

    [Fact]
    public void Enumeration_PreservesInsertionOrder()
    {
        Assert.Equal([5, 2, 8], Create(5, 2, 8).ToArray());
    }

    [Fact]
    public void Add_AppendsAfterExistingCandidates()
    {
        var collection = Create(5, 2);

        collection.Add(1);

        Assert.Equal([5, 2, 1], collection.ToArray());
    }

    [Fact]
    public void Remove_WithDuplicates_RemovesOnlyFirstOccurrence()
    {
        var collection = Create(3, 3, 4);

        Assert.True(collection.Remove(3));

        Assert.Equal([3, 4], collection.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    [InlineData(33)]
    public void Add_WithOutOfRangeCandidate_Throws(int candidate)
    {
        var collection = Create(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.Add(candidate));
        Assert.Equal([1], collection.ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Add_AcceptsBoundaryCandidates(int candidate)
    {
        var collection = Create();

        collection.Add(candidate);

        Assert.Equal([candidate], collection.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
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
        var collection = Create(1, 9);

        Assert.False(collection.Remove(candidate));
        Assert.Equal([1, 9], collection.ToArray());
    }

    [Fact]
    public void SetState_WithMaskState_Throws()
    {
        var collection = Create(1);

        Assert.Throws<ArgumentException>(() => collection.SetState(new CandidateCollectionMask([1]).GetState()));
    }
}
