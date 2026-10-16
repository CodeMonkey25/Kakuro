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

    [Fact]
    public void Add_KeepsAscendingOrder()
    {
        var mask = Create(5);

        mask.Add(9);
        mask.Add(1);

        Assert.Equal([1, 5, 9], mask.ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Add_AcceptsBoundaryCandidates(int candidate)
    {
        var mask = Create();

        mask.Add(candidate);

        Assert.Equal([candidate], mask.ToArray());
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

    [Fact]
    public void SetState_WithListState_Throws()
    {
        var mask = Create(1);

        Assert.Throws<ArgumentException>(() => mask.SetState(new CandidateCollectionList([1]).GetState()));
    }

    [Fact]
    public void EverySubsetOfOneToNine_BehavesConsistently()
    {
        for (int bits = 0; bits < 1 << 9; bits++)
        {
            int[] expected = Enumerable.Range(1, 9).Where(v => (bits & 1 << (v - 1)) != 0).ToArray();
            var mask = Create(expected);

            Assert.Equal(expected, mask.ToArray());
            Assert.Equal(expected.Length, mask.Count);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], mask.Get(i));
            }

            int[] copied = new int[expected.Length];
            mask.CopyTo(copied);
            Assert.Equal(expected, copied);

            var state = mask.GetState();
            Assert.Equal(expected.Length > 0, state.IsStateValid(0));
            if (expected.Length == 1)
            {
                Assert.True(state.IsStateValid(expected[0]));
            }

            foreach (int candidate in expected)
            {
                Assert.True(mask.Remove(candidate));
            }
            Assert.Empty(mask);
        }
    }
}
