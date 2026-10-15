using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

/// <summary>
/// Behavior every <see cref="CandidateCollection"/> implementation must satisfy.
/// </summary>
public abstract class CandidateCollectionContractTests
{
    protected abstract CandidateCollection Create(params int[] candidates);

    [Fact]
    public void Count_ReflectsCandidates()
    {
        Assert.Empty(Create());
        Assert.Equal(4, Create(1, 3, 7, 9).Count);
    }

    [Fact]
    public void Enumeration_ReturnsAllCandidates_IncludingNine()
    {
        var collection = Create(1, 3, 7, 9);

        Assert.Equal([1, 3, 7, 9], collection.ToArray());
    }

    [Fact]
    public void Enumeration_WhenEmpty_ReturnsNothing()
    {
        Assert.Empty(Create());
    }

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
    public void Add_AddsCandidate()
    {
        var collection = Create(2);

        collection.Add(9);

        Assert.Equal(2, collection.Count);
        Assert.Contains(9, collection);
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
    public void Clear_RemovesAllCandidates()
    {
        var collection = Create(1, 2, 3);

        collection.Clear();

        Assert.Empty(collection);
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
    public void GetState_IsNotAffectedByLaterMutation()
    {
        var collection = Create(1, 2);
        var state = collection.GetState();

        collection.Clear();

        Assert.True(state.Equals(Create(1, 2).GetState()));
    }

    [Fact]
    public void SetState_WithForeignStateType_Throws()
    {
        var collection = Create(1);
        var foreign = new ForeignState();

        Assert.Throws<ArgumentException>(() => collection.SetState(foreign));
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

    private sealed class ForeignState : CandidateCollection.CandidateCollectionState
    {
        public override bool IsStateValid(int value) => true;
    }
}

public class CandidateCollectionListTests : CandidateCollectionContractTests
{
    protected override CandidateCollection Create(params int[] candidates) => new CandidateCollectionList(candidates);

    [Fact]
    public void Constructor_WithNoArguments_IsEmpty()
    {
        Assert.Empty(new CandidateCollectionList());
    }

    [Fact]
    public void Enumeration_PreservesInsertionOrder()
    {
        Assert.Equal([5, 2, 8], Create(5, 2, 8).ToArray());
    }

    [Fact]
    public void Remove_WithDuplicates_RemovesOnlyFirstOccurrence()
    {
        var collection = Create(3, 3, 4);

        Assert.True(collection.Remove(3));

        Assert.Equal([3, 4], collection.ToArray());
    }

    [Fact]
    public void IsStateValid_WithZeroAndOutOfRangeCandidate_ReturnsFalse()
    {
        var state = Create(1, 12).GetState();

        Assert.False(state.IsStateValid(0));
    }
}

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
}
