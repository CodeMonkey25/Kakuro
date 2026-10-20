using Kakuro.Core.Interfaces;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PlayableCellStateTests
{
    [Fact]
    public void States_CompareByContent()
    {
        PlayableCell a = new(0, 0);
        PlayableCell b = new(1, 1);

        Assert.Equal(a.GetState(), b.GetState());
        Assert.Equal(a.GetState().GetHashCode(), b.GetState().GetHashCode());

        b.RemoveCandidate(5);
        Assert.NotEqual(a.GetState(), b.GetState());
    }

    [Fact]
    public void States_OfSolvedCells_CompareByValue()
    {
        PlayableCell four = new(0, 0);
        PlayableCell alsoFour = new(1, 1);
        PlayableCell five = new(2, 2);
        four.Solve(4);
        alsoFour.Solve(4);
        five.Solve(5);

        Assert.Equal(four.GetState(), alsoFour.GetState());
        Assert.Equal(four.GetState().GetHashCode(), alsoFour.GetState().GetHashCode());
        Assert.NotEqual(four.GetState(), five.GetState());
    }

    [Fact]
    public void GetState_IsASnapshot_NotAffectedByLaterChanges()
    {
        PlayableCell cell = new(0, 0);
        ICell.IState snapshot = cell.GetState();

        cell.RemoveCandidates([1, 2, 3]);

        Assert.Equal(new PlayableCell(9, 9).GetState(), snapshot);
    }

    [Fact]
    public void SetState_RestoresCandidates()
    {
        PlayableCell cell = new(0, 0);
        ICell.IState original = cell.GetState();

        cell.RemoveCandidates([1, 2, 3]);
        Assert.NotEqual(original, cell.GetState());

        cell.SetState(original);

        Assert.Equal(original, cell.GetState());
        Assert.Equal(Constants.AllCandidates.Length, cell.CandidateCount);
        Assert.False(cell.IsSolved);
    }

    [Fact]
    public void SetState_RestoresUnsolvedValue()
    {
        PlayableCell cell = new(0, 0);
        ICell.IState original = cell.GetState();

        cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);

        cell.SetState(original);

        Assert.False(cell.IsSolved);
        Assert.Equal(Constants.UnsolvedCell, cell.Value);
        Assert.Equal(Constants.AllCandidates.Length, cell.CandidateCount);
    }

    [Fact]
    public void SetState_RestoresSolvedValue()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(6);
        ICell.IState solved = cell.GetState();

        cell.SetState(new PlayableCell(0, 0).GetState());
        Assert.False(cell.IsSolved);

        cell.SetState(solved);

        Assert.True(cell.IsSolved);
        Assert.Equal(6, cell.Value);
        Assert.Equal([6], cell.CandidatesArray());
        Assert.Equal("6", cell.ToString());
    }

    [Fact]
    public void SetState_AcceptsStateTakenFromAnotherCell()
    {
        PlayableCell source = new(0, 0);
        source.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]);
        PlayableCell target = new(5, 5);

        target.SetState(source.GetState());

        Assert.Equal([8, 9], target.CandidatesArray());
        Assert.Equal(5, target.Row);
        Assert.Equal(5, target.Column);
    }

    [Fact]
    public void SetState_DoesNotAlterLinks()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        cell.BindCells([peer]);
        ICell.IState original = cell.GetState();

        cell.RemoveCandidate(1);
        cell.SetState(original);

        Assert.Equal([peer], cell.LinkedCells);
    }

    [Fact]
    public void SetState_DoesNotPropagateToLinkedCells()
    {
        PlayableCell solvedSource = new(0, 0);
        solvedSource.Solve(4);
        PlayableCell cell = new(0, 1);
        PlayableCell peer = new(0, 2);
        cell.BindCells([peer]);

        cell.SetState(solvedSource.GetState());

        Assert.True(cell.IsSolved);
        Assert.False(peer.IsSolved);
        Assert.Contains(4, peer.CandidatesArray());
    }

    [Fact]
    public void IsValidState_AcceptsSolvedAndPartiallyReducedStates()
    {
        PlayableCell solved = new(0, 0);
        solved.Solve(3);
        PlayableCell reduced = new(0, 0);
        reduced.RemoveCandidates([1, 2]);
        PlayableCell target = new(1, 1);

        Assert.True(target.IsValidState(solved.GetState()));
        Assert.True(target.IsValidState(reduced.GetState()));
    }

    [Fact]
    public void SetState_WithEmptyCellState_ThrowsAndLeavesCellUnchanged()
    {
        PlayableCell cell = new(0, 0);
        cell.RemoveCandidate(2);
        ICell.IState before = cell.GetState();

        Assert.Throws<ArgumentException>(() => cell.SetState(EmptyCell.Instance.GetState()));
        Assert.Equal(before, cell.GetState());
    }
}
