using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionBuildTests
{
    [Fact]
    public void Build_ReturnsCollectionContainingCandidates()
    {
        var collection = CandidateCollection.Build([2, 4, 6]);

        Assert.Equal([2, 4, 6], collection.ToArray());
    }

    [Fact]
    public void Build_WithNoCandidates_ReturnsEmptyCollection()
    {
        Assert.Empty(CandidateCollection.Build([]));
    }

    [Fact]
    public void Build_DeduplicatesAndSortsLikeTheDefaultImplementation()
    {
        var collection = CandidateCollection.Build([9, 1, 9, 5]);

        Assert.Equal([1, 5, 9], collection.ToArray());
    }

    [Fact]
    public void Build_WithOutOfRangeCandidate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CandidateCollection.Build([1, 10]));
    }

    [Fact]
    public void Build_ReturnsIndependentCollections()
    {
        var a = CandidateCollection.Build([1, 2]);
        var b = CandidateCollection.Build([1, 2]);

        a.Remove(1);

        Assert.Equal([1, 2], b.ToArray());
    }
}
