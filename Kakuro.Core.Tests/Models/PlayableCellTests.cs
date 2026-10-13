using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PlayableCellTests
{
    [Fact]
    public void Constructor_InitializesStateAndCoordinates()
    {
        PlayableCell cell = new(3, 5);

        Assert.Equal(3, cell.Row);
        Assert.Equal(5, cell.Column);
        Assert.Equal(0, cell.Value);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.Candidates);
        Assert.Empty(cell.LinkedCells);
        Assert.Equal(" ", cell.ToString());
    }

    [Fact]
    public void Bind_AddsPeerCellsExcludingSelfAndAvoidsDuplicates()
    {
        PlayableCell cell1 = new(1, 1);
        PlayableCell cell2 = new(1, 2);
        PlayableCell cell3 = new(1, 3);

        cell1.Bind([cell1, cell2, cell3]);

        Assert.Equal(2, cell1.LinkedCells.Count);
        Assert.Contains(cell2, cell1.LinkedCells);
        Assert.Contains(cell3, cell1.LinkedCells);
        Assert.DoesNotContain(cell1, cell1.LinkedCells);

        // Binding again should not duplicate
        cell1.Bind([cell2, cell3]);
        Assert.Equal(2, cell1.LinkedCells.Count);
    }

    [Fact]
    public void RemoveCandidate_UnsolvedCell_RemovesCandidateWithoutSolving()
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidate(3);

        Assert.Null(error);
        Assert.True(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal(0, cell.Value);
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.Candidates);
        Assert.Equal(" ", cell.ToString());
    }

    [Fact]
    public void RemoveCandidate_WhenCandidateNotPresent_ReturnsFalseAndNoError()
    {
        PlayableCell cell = new(0, 0);
        cell.RemoveCandidate(3);

        (string? error, bool removed) = cell.RemoveCandidate(3);

        Assert.Null(error);
        Assert.False(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidate_DownToOneCandidate_SolvesCellAndUpdatesString()
    {
        string? error = null;
        bool removed = false;
        PlayableCell cell = new(2, 4);

        // Remove candidates 1 through 7, leaving 8 and 9
        for (int i = 1; i <= 7; i++)
        {
            (error, removed) = cell.RemoveCandidate(i);
            Assert.Null(error);
            Assert.True(removed);
            Assert.False(cell.IsSolved);
        }
        Assert.Equal([8, 9], cell.Candidates);

        (error, removed) = cell.RemoveCandidate(8);

        Assert.Null(error);
        Assert.True(removed);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.Candidates);
        Assert.Equal("9", cell.ToString());
    }

    [Fact]
    public void RemoveCandidate_WhenAllCandidatesRemoved_ReturnsError()
    {
        PlayableCell cell = new(1, 2);

        // Remove candidates 1 through 8
        for (int i = 1; i <= 8; i++)
        {
            cell.RemoveCandidate(i);
        }

        // Cell is now solved to 9. Removing 9 should return no candidates error
        (string? error, bool removed) = cell.RemoveCandidate(9);

        Assert.Equal("Cell at row 1, column 2 has no candidates.", error);
        Assert.False(removed);
    }

    [Fact]
    public void RemoveCandidate_WhenAlreadySolved_ReturnsErrorOnlyWhenMatchingValue()
    {
        PlayableCell cell = new(0, 0);
        var (initError, initRemoved) = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]); // Solved to 9
        Assert.Null(initError);
        Assert.True(initRemoved);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);

        // Removing a candidate different from solved Value should return empty string and false
        (string? nonMatchingError, bool nonMatchingRemoved) = cell.RemoveCandidate(4);
        Assert.Null(nonMatchingError);
        Assert.False(nonMatchingRemoved);

        // Removing the candidate matching solved Value returns error and false
        (string? matchingError, bool matchingRemoved) = cell.RemoveCandidate(9);
        Assert.Equal("Cell at row 0, column 0 has no candidates.", matchingError);
        Assert.False(matchingRemoved);
    }

    [Fact]
    public void RemoveCandidates_MultipleValues_AppliesAllRemovals()
    {
        PlayableCell cell = new(1, 1);

        (string? result, bool removed) = cell.RemoveCandidates([2, 3, 4, 5, 6, 7, 8]);

        Assert.Null(result);
        Assert.True(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidates_WhenNoCandidatesRemoved_ReturnsFalseAndNoError()
    {
        PlayableCell cell = new(1, 1);
        cell.RemoveCandidates([2, 3]);

        (string? result, bool removed) = cell.RemoveCandidates([2, 3]);

        Assert.Null(result);
        Assert.False(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 4, 5, 6, 7, 8, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidates_LeavesSingleCandidate_SolvesCell()
    {
        PlayableCell cell = new(1, 1);

        (string? result, bool removed) = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Null(result);
        Assert.True(removed);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.Candidates);
    }

    [Fact]
    public void Candidates_ReturnsRemainingCandidatesInAscendingOrder()
    {
        PlayableCell cell = new(0, 0);
        var (_, removed) = cell.RemoveCandidates([7, 2, 5]);

        Assert.True(removed);
        Assert.Equal([1, 3, 4, 6, 8, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidates_WhenContradictionOccurs_ShortCircuitsAndReturnsError()
    {
        PlayableCell cell = new(3, 3);

        (string? error, bool removed) = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.Equal("Cell at row 3, column 3 has no candidates.", error);
        Assert.True(removed);
    }

    [Fact]
    public void Solve_PropagatesValueRemovalToLinkedCells()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);

        // Link cellA and cellB
        cellA.Bind([cellA, cellB]);
        cellB.Bind([cellA, cellB]);

        // Restrict cellB to candidates {8, 9}
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]);

        // Solve cellA to 9 by eliminating 1..8
        (string? result, bool removed) = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Null(result);
        Assert.True(removed);
        Assert.True(cellA.IsSolved);
        Assert.Equal(9, cellA.Value);

        // cellB should have received candidate 9 removal, reducing it to {8} and auto-solving to 8
        Assert.True(cellB.IsSolved);
        Assert.Equal(8, cellB.Value);
    }

    [Fact]
    public void Solve_PropagatesContradictionToLinkedCell_ReturnsError()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);

        // Directly solve cellB to 9
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.True(cellB.IsSolved);
        Assert.Equal(9, cellB.Value);

        // Bind cellB to cellA so that cellA solving to 9 propagates to already-solved cellB
        cellA.Bind([cellB]);

        // When cellA solves to 9, it tries to remove 9 from cellB, causing conflict
        (string? error, _) = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("Cell at row 0, column 1 has no candidates.", error);
    }

    [Fact]
    public void IsInstanceOfCell()
    {
        PlayableCell cell = new(0, 0);

        Assert.IsAssignableFrom<Cell>(cell);
    }

    [Fact]
    public void RemoveCandidates_WhenEmptyListProvided_ReturnsFalseAndNoError()
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidates([]);

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.Candidates);
    }

    [Fact]
    public void Solve_CascadingChainReaction_PropagatesAcrossMultipleLinkedCells()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);
        PlayableCell cellC = new(0, 2);

        // Link A <-> B and B <-> C
        cellA.Bind([cellB]);
        cellB.Bind([cellA, cellC]);
        cellC.Bind([cellB]);

        // Restrict cellA to {8, 9}, cellB to {7, 8}, cellC to {6, 7}
        cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]);
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 9]);
        cellC.RemoveCandidates([1, 2, 3, 4, 5, 8, 9]);

        // Solve cellA to 8 by eliminating 9
        (string? error, bool removed) = cellA.RemoveCandidate(9);

        Assert.Null(error);
        Assert.True(removed);
        Assert.True(cellA.IsSolved);
        Assert.Equal(8, cellA.Value);

        // cellB should have received 8 removal, auto-solving to 7
        Assert.True(cellB.IsSolved);
        Assert.Equal(7, cellB.Value);

        // cellC should have received 7 removal from cellB, auto-solving to 6
        Assert.True(cellC.IsSolved);
        Assert.Equal(6, cellC.Value);
    }

    [Fact]
    public void Bind_MultiplePeerGroups_MergesWithoutDuplicates()
    {
        PlayableCell mainCell = new(1, 1);
        PlayableCell horizontalPeer = new(1, 2);
        PlayableCell verticalPeer = new(2, 1);

        // Simulate binding horizontal run then vertical run
        mainCell.Bind([mainCell, horizontalPeer]);
        mainCell.Bind([mainCell, verticalPeer, horizontalPeer]);

        Assert.Equal(2, mainCell.LinkedCells.Count);
        Assert.Contains(horizontalPeer, mainCell.LinkedCells);
        Assert.Contains(verticalPeer, mainCell.LinkedCells);
        Assert.DoesNotContain(mainCell, mainCell.LinkedCells);
    }
}
