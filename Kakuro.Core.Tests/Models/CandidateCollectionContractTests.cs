using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

/// <summary>
/// Behavior every <see cref="CandidateCollection"/> implementation must satisfy.
/// </summary>
public abstract class CandidateCollectionContractTests
{
    protected abstract CandidateCollection Create(params int[] candidates);

    #region Count / enumeration / Get

    [Fact]
    public void Count_ReflectsCandidates()
    {
        Assert.Equal(0, Create().Count);
        Assert.Equal(4, Create(1, 3, 7, 9).Count);
    }

    [Fact]
    public void Copy_ReturnsAllCandidates_IncludingBoundaries()
    {
        Assert.Equal([1, 3, 7, 9], Create(1, 3, 7, 9).ToArray());
    }

    [Fact]
    public void Copy_WhenEmpty_ReturnsNothing()
    {
        Assert.Equal(0, Create().Count);
    }

    [Fact]
    public void Copy_CanBeRepeated()
    {
        var collection = Create(2, 4, 6);

        Assert.Equal(collection.ToArray(), collection.ToArray());
    }

    [Fact]
    public void Contains_ReportsMembership()
    {
        var collection = Create(2, 9);

        Assert.True(collection.Contains(2));
        Assert.True(collection.Contains(9));
        Assert.False(collection.Contains(1));
        Assert.False(collection.Contains(8));
    }

    [Fact]
    public void Contains_ReflectsAddRemoveAndClear()
    {
        var collection = Create(2);

        collection.Add(7);
        Assert.True(collection.Contains(7));

        collection.Remove(2);
        Assert.False(collection.Contains(2));

        collection.Clear();
        Assert.False(collection.Contains(7));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(33)]
    public void Contains_WithOutOfRangeCandidate_ReturnsFalse(int candidate)
    {
        Assert.False(Create(1, 9).Contains(candidate));
    }

    [Fact]
    public void Contains_WhenEmpty_IsFalse()
    {
        Assert.False(Create().Contains(1));
    }

    [Fact]
    public void Get_ReturnsCandidateAtIndex_IncludingNine()
    {
        var collection = Create(1, 3, 7, 9);

        Assert.Equal(1, collection.Get(0));
        Assert.Equal(3, collection.Get(1));
        Assert.Equal(7, collection.Get(2));
        Assert.Equal(9, collection.Get(3));
    }

    [Fact]
    public void Get_WithSingleCandidate_ReturnsIt()
    {
        Assert.Equal(9, Create(9).Get(0));
    }

    [Fact]
    public void Get_AfterRemove_ReflectsRemoval()
    {
        var collection = Create(2, 4, 6);
        collection.Remove(2);

        Assert.Equal(4, collection.Get(0));
        Assert.Equal(6, collection.Get(1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Get_WithOutOfRangeIndex_Throws(int index)
    {
        var collection = Create(1, 5, 9);

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.Get(index));
    }

    [Fact]
    public void Get_WhenEmpty_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create().Get(0));
    }

    [Fact]
    public void Get_AfterClear_Throws()
    {
        var collection = Create(1, 2);
        collection.Clear();

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.Get(0));
    }

    #endregion

    #region CopyTo

    [Fact]
    public void CopyTo_WritesCandidatesInOrder()
    {
        var collection = Create(3, 5, 9);
        Span<int> buffer = stackalloc int[collection.Count];

        collection.CopyTo(buffer);

        Assert.Equal([3, 5, 9], buffer.ToArray());
    }

    [Fact]
    public void CopyTo_WhenEmpty_WritesNothing()
    {
        var collection = Create();
        Span<int> buffer = [];

        collection.CopyTo(buffer);

        Assert.Empty(buffer.ToArray());
    }

    [Fact]
    public void CopyTo_AfterRemove_ReflectsRemoval()
    {
        var collection = Create(1, 2, 3, 9);
        collection.Remove(2);
        Span<int> buffer = stackalloc int[collection.Count];

        collection.CopyTo(buffer);

        Assert.Equal([1, 3, 9], buffer.ToArray());
    }

    [Fact]
    public void CopyTo_IntoLargerBuffer_LeavesTrailingElementsUntouched()
    {
        // Board relies on this: it reuses a 9-element buffer and slices it by Count.
        var collection = Create(4, 6);
        int[] buffer = [-1, -1, -1, -1];

        collection.CopyTo(buffer);

        Assert.Equal([4, 6, -1, -1], buffer);
    }

    [Fact]
    public void CopyTo_IntoTooSmallBuffer_Throws()
    {
        var collection = Create(1, 2, 3);

        // implementations differ in the exception type (IndexOutOfRange vs Argument), but both must refuse
        Assert.ThrowsAny<Exception>(() => collection.CopyTo(new int[2]));
    }

    #endregion

    #region Add / Remove / Clear

    [Fact]
    public void Add_AddsCandidate()
    {
        var collection = Create(2);

        collection.Add(9);

        Assert.Equal(2, collection.Count);
        Assert.True(collection.Contains(9));
    }

    [Fact]
    public void Remove_WhenPresent_RemovesAndReturnsTrue()
    {
        var collection = Create(2, 5, 9);

        Assert.True(collection.Remove(5));

        Assert.Equal([2, 9], collection.ToArray());
    }

    [Fact]
    public void Remove_WhenAbsent_ReturnsFalseAndLeavesCollectionUnchanged()
    {
        var collection = Create(2, 5);

        Assert.False(collection.Remove(7));

        Assert.Equal([2, 5], collection.ToArray());
    }

    [Fact]
    public void Remove_SameCandidateTwice_SecondCallReturnsFalse()
    {
        var collection = Create(2, 5);

        Assert.True(collection.Remove(2));
        Assert.False(collection.Remove(2));
        Assert.Equal([5], collection.ToArray());
    }

    [Fact]
    public void Remove_FromEmptyCollection_ReturnsFalse()
    {
        Assert.False(Create().Remove(1));
    }

    [Fact]
    public void Remove_EveryCandidate_LeavesEmptyCollection()
    {
        var collection = Create(1, 5, 9);

        Assert.True(collection.Remove(9));
        Assert.True(collection.Remove(1));
        Assert.True(collection.Remove(5));

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void RemoveAll_Mask_RemovesOnlyPresentCandidatesAndReturnsTrue()
    {
        var collection = Create(1, 3, 5, 9);

        Assert.True(collection.RemoveAll((1u << 0) | (1u << 1) | (1u << 8))); // 1, 2 (absent), 9

        Assert.Equal([3, 5], collection.ToArray());
    }

    [Fact]
    public void RemoveAll_Mask_WhenNothingPresent_ReturnsFalseAndLeavesCollectionUnchanged()
    {
        var collection = Create(1, 3);

        Assert.False(collection.RemoveAll(1u << 4));
        Assert.False(collection.RemoveAll(0u));

        Assert.Equal([1, 3], collection.ToArray());
    }

    [Fact]
    public void RemoveAll_Mask_IgnoresBitsAboveNine()
    {
        var collection = Create(1, 9);

        Assert.False(collection.RemoveAll((1u << 9) | (1u << 20) | (1u << 31)));

        Assert.Equal([1, 9], collection.ToArray());
    }

    [Fact]
    public void RemoveAll_Mask_EveryCandidate_LeavesEmptyCollection()
    {
        var collection = Create(2, 4, 6);

        Assert.True(collection.RemoveAll(0x1FFu));

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void RemoveAll_Enumerable_RemovesOnlyPresentCandidatesAndReturnsTrue()
    {
        var collection = Create(1, 3, 5, 9);

        Assert.True(collection.RemoveAll([9, 2, 1, 1]));

        Assert.Equal([3, 5], collection.ToArray());
    }

    [Fact]
    public void RemoveAll_Enumerable_WhenNothingPresent_ReturnsFalse()
    {
        var collection = Create(1, 3);

        Assert.False(collection.RemoveAll([2, 4, 0, 10, -1]));
        Assert.False(collection.RemoveAll(Array.Empty<int>()));

        Assert.Equal([1, 3], collection.ToArray());
    }

    [Fact]
    public void Clear_RemovesAllCandidates()
    {
        var collection = Create(1, 2, 3);

        collection.Clear();

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Clear_WhenAlreadyEmpty_IsHarmless()
    {
        var collection = Create();

        collection.Clear();

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Clear_ThenAdd_StartsFromScratch()
    {
        var collection = Create(1, 2, 3);

        collection.Clear();
        collection.Add(7);

        Assert.Equal([7], collection.ToArray());
    }

    #endregion

    #region Ordering / validation

    [Fact]
    public void Constructor_DeduplicatesCandidates()
    {
        var collection = Create(3, 3, 5);

        Assert.Equal(2, collection.Count);
        Assert.Equal([3, 5], collection.ToArray());
    }

    [Fact]
    public void Candidates_AreAscending()
    {
        Assert.Equal([2, 5, 8], Create(8, 2, 5).ToArray());
    }

    [Fact]
    public void Add_Duplicate_DoesNotIncreaseCount()
    {
        var collection = Create(4);

        collection.Add(4);

        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void Add_KeepsAscendingOrder()
    {
        var collection = Create(5);

        collection.Add(9);
        collection.Add(1);

        Assert.Equal([1, 5, 9], collection.ToArray());
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
    [InlineData(33)]
    public void Add_WithOutOfRangeCandidate_Throws(int candidate)
    {
        var collection = Create(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => collection.Add(candidate));
        Assert.Equal([1], collection.ToArray());
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
    public void AllCandidates_RoundTripThroughCopy()
    {
        var collection = Create(1, 2, 3, 4, 5, 6, 7, 8, 9);

        Assert.Equal(9, collection.Count);
        Assert.Equal(Enumerable.Range(1, 9), collection.ToArray());
    }

    [Fact]
    public void EverySubsetOfOneToNine_BehavesConsistently()
    {
        for (int bits = 0; bits < 1 << 9; bits++)
        {
            int[] expected = Enumerable.Range(1, 9).Where(v => (bits & 1 << (v - 1)) != 0).ToArray();
            var collection = Create(expected);

            Assert.Equal(expected, collection.ToArray());
            Assert.Equal(expected.Length, collection.Count);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], collection.Get(i));
            }

            int[] copied = new int[expected.Length];
            collection.CopyTo(copied);
            Assert.Equal(expected, copied);

            var state = collection.GetState();
            Assert.Equal(expected.Length > 0, state.IsStateValid(0));
            if (expected.Length == 1)
            {
                Assert.True(state.IsStateValid(expected[0]));
            }

            foreach (int candidate in expected)
            {
                Assert.True(collection.Remove(candidate));
            }
            Assert.Equal(0, collection.Count);
        }
    }

    #endregion

    #region State

    [Fact]
    public void SetState_WithStateFromAnotherImplementation_Throws()
    {
        var collection = Create(1);
        var ownType = collection.GetState().GetType();
        CandidateCollection[] others =
        [
            new CandidateCollectionArray([1]),
            new CandidateCollectionList([1]),
            new CandidateCollectionMask([1]),
        ];

        foreach (var other in others.Where(o => o.GetState().GetType() != ownType))
        {
            Assert.Throws<ArgumentException>(() => collection.SetState(other.GetState()));
        }
        Assert.Equal([1], collection.ToArray());
    }

    [Fact]
    public void SetState_DoesNotAliasTheCapturedState()
    {
        var original = Create(2, 4);
        var state = original.GetState();
        var other = Create();
        other.SetState(state);

        other.Add(7);

        Assert.Equal([2, 4, 7], other.ToArray());
        Assert.Equal([2, 4], original.ToArray());
        var restored = Create();
        restored.SetState(state);
        Assert.Equal([2, 4], restored.ToArray());
    }

    [Fact]
    public void SetState_RestoresStateCapturedByGetState()
    {
        var collection = Create(1, 4, 9);
        var state = collection.GetState();

        collection.Remove(4);
        collection.Add(6);
        collection.SetState(state);

        Assert.Equal([1, 4, 9], collection.ToArray());
    }

    [Fact]
    public void SetState_CanRestoreAnEmptyCollection()
    {
        var collection = Create();
        var state = collection.GetState();

        collection.Add(3);
        collection.SetState(state);

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void SetState_CanBeAppliedToADifferentInstance()
    {
        var source = Create(2, 3, 8);
        var target = Create(1);

        target.SetState(source.GetState());

        Assert.Equal([2, 3, 8], target.ToArray());
    }

    [Fact]
    public void SetState_CanBeAppliedRepeatedly()
    {
        var collection = Create(1, 2, 3);
        var state = collection.GetState();

        collection.Clear();
        collection.SetState(state);
        collection.Remove(1);
        collection.SetState(state);

        Assert.Equal([1, 2, 3], collection.ToArray());
    }

    [Fact]
    public void GetState_IsNotAffectedByLaterMutation()
    {
        var collection = Create(1, 2);
        var state = collection.GetState();

        collection.Clear();

        Assert.True(state.Equals(Create(1, 2).GetState()));
    }

    [Fact]
    public void GetState_IsNotAffectedByMutationAfterSetState()
    {
        var collection = Create(1, 2);
        var state = collection.GetState();

        collection.SetState(state);
        collection.Remove(1);

        Assert.True(state.Equals(Create(1, 2).GetState()));
    }

    [Fact]
    public void SetState_WithForeignStateType_Throws()
    {
        var collection = Create(1);
        var foreign = new ForeignState();

        Assert.Throws<ArgumentException>(() => collection.SetState(foreign));
        Assert.Equal([1], collection.ToArray());
    }

    [Fact]
    public void State_EqualityFollowsContents()
    {
        var a = Create(1, 2, 3).GetState();
        var b = Create(1, 2, 3).GetState();
        var c = Create(1, 2).GetState();

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void State_OfClearedCollectionEqualsStateOfEmptyCollection()
    {
        var cleared = Create(1, 2, 3);
        cleared.Clear();

        Assert.Equal(Create().GetState(), cleared.GetState());
    }

    [Fact]
    public void State_IsNotEqualToNullOrOtherTypes()
    {
        var state = Create(1).GetState();

        Assert.False(state.Equals(null));
        Assert.False(state.Equals("1"));
        Assert.True(state.Equals((object)state));
    }

    [Fact]
    public void IsStateValid_WithSingleCandidate_OnlyMatchesThatValue()
    {
        var state = Create(4).GetState();

        Assert.True(state.IsStateValid(4));
        Assert.False(state.IsStateValid(5));
        Assert.True(state.IsStateValid(0));
    }

    [Fact]
    public void IsStateValid_WithMultipleCandidates_RejectsNonZeroValue()
    {
        var state = Create(3, 4).GetState();

        Assert.False(state.IsStateValid(3));
        Assert.False(state.IsStateValid(4));
        Assert.True(state.IsStateValid(0));
    }

    [Fact]
    public void IsStateValid_WhenEmpty_IsAlwaysFalse()
    {
        var state = Create().GetState();

        Assert.False(state.IsStateValid(0));
        Assert.False(state.IsStateValid(1));
    }

    [Fact]
    public void IsStateValid_WithOutOfRangeValue_ReturnsFalse()
    {
        var state = Create(1, 2).GetState();

        Assert.False(state.IsStateValid(-1));
        Assert.False(state.IsStateValid(10));
        Assert.False(state.IsStateValid(33));
    }

    [Fact]
    public void IsStateValid_WithAllNineCandidates_IsValidOnlyForUnsolved()
    {
        var state = Create(1, 2, 3, 4, 5, 6, 7, 8, 9).GetState();

        Assert.True(state.IsStateValid(0));
        for (int value = 1; value <= 9; value++)
        {
            Assert.False(state.IsStateValid(value));
        }
    }

    private sealed record ForeignState : CandidateCollection.CandidateCollectionState
    {
        public override bool IsStateValid(int value) => true;
    }

    #endregion
}
