using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionListTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionList(candidates);

    [Fact]
    public void Constructor_WithEmptySequence_IsEmpty()
    {
        Assert.Equal(0, new CandidateCollectionList([]).Count);
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
}
