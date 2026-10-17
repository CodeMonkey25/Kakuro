using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CandidateCollectionArrayTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionArray(candidates);

    [Fact]
    public void Constructor_DeduplicatesCandidates()
    {
        var array = Create(3, 3, 5);

        Assert.Equal(2, array.Count);
        Assert.Equal([3, 5], array.ToArray());
    }

    [Fact]
    public void Candidates_AreAscending()
    {
        Assert.Equal([2, 5, 8], Create(8, 2, 5).ToArray());
    }

    [Fact]
    public void Add_Duplicate_DoesNotIncreaseCount()
    {
        var array = Create(4);

        array.Add(4);

        Assert.Equal(1, array.Count);
    }

    [Fact]
    public void Add_KeepsAscendingOrder()
    {
        var array = Create(5);

        array.Add(9);
        array.Add(1);

        Assert.Equal([1, 5, 9], array.ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Add_AcceptsBoundaryCandidates(int candidate)
    {
        var array = Create();

        array.Add(candidate);

        Assert.Equal([candidate], array.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    [InlineData(33)]
    public void Add_WithOutOfRangeCandidate_Throws(int candidate)
    {
        var array = Create(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => array.Add(candidate));
        Assert.Equal([1], array.ToArray());
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
        var array = Create(1, 9);

        Assert.False(array.Remove(candidate));
        Assert.Equal([1, 9], array.ToArray());
    }

    [Fact]
    public void AllCandidates_RoundTripThroughCopy()
    {
        var array = Create(1, 2, 3, 4, 5, 6, 7, 8, 9);

        Assert.Equal(9, array.Count);
        Assert.Equal(Enumerable.Range(1, 9), array.ToArray());
    }

    [Fact]
    public void SetState_WithListState_Throws()
    {
        var array = Create(1);

        Assert.Throws<ArgumentException>(() => array.SetState(new CandidateCollectionList([1]).GetState()));
    }

    [Fact]
    public void SetState_WithMaskState_Throws()
    {
        var array = Create(1);

        Assert.Throws<ArgumentException>(() => array.SetState(new CandidateCollectionMask([1]).GetState()));
    }

    [Fact]
    public void SetState_DoesNotAliasTheCapturedState()
    {
        var array = Create(2, 4);
        var state = array.GetState();
        var other = Create();
        other.SetState(state);

        other.Add(7);

        Assert.Equal([2, 4, 7], other.ToArray());
        Assert.Equal([2, 4], array.ToArray());
        var restored = Create();
        restored.SetState(state);
        Assert.Equal([2, 4], restored.ToArray());
    }

    [Fact]
    public void EverySubsetOfOneToNine_BehavesConsistently()
    {
        for (int bits = 0; bits < 1 << 9; bits++)
        {
            int[] expected = Enumerable.Range(1, 9).Where(v => (bits & 1 << (v - 1)) != 0).ToArray();
            var array = Create(expected);

            Assert.Equal(expected, array.ToArray());
            Assert.Equal(expected.Length, array.Count);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], array.Get(i));
            }

            int[] copied = new int[expected.Length];
            array.CopyTo(copied);
            Assert.Equal(expected, copied);

            var state = array.GetState();
            Assert.Equal(expected.Length > 0, state.IsStateValid(0));
            if (expected.Length == 1)
            {
                Assert.True(state.IsStateValid(expected[0]));
            }

            foreach (int candidate in expected)
            {
                Assert.True(array.Remove(candidate));
            }
            Assert.Equal(0, array.Count);
        }
    }
}
