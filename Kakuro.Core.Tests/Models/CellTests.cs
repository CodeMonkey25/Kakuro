using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CellTests
{
    [Fact]
    public void Constructor_InitializesStateAndCoordinates()
    {
        Cell cell = new(3, 5);

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
        Cell cell1 = new(1, 1);
        Cell cell2 = new(1, 2);
        Cell cell3 = new(1, 3);

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
        Cell cell = new(0, 0);

        string error = cell.RemoveCandidate(3);

        Assert.Equal("", error);
        Assert.False(cell.IsSolved);
        Assert.Equal(0, cell.Value);
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.Candidates);
        Assert.Equal(" ", cell.ToString());
    }

    [Fact]
    public void RemoveCandidate_DownToOneCandidate_SolvesCellAndUpdatesString()
    {
        string error = "";
        Cell cell = new(2, 4);

        // Remove candidates 1 through 8, leaving only 9
        for (int i = 1; i <= 7; i++)
        {
            error = cell.RemoveCandidate(i);
            Assert.Equal("", error);
            Assert.False(cell.IsSolved);
        }
        Assert.Equal([8, 9], cell.Candidates);

        error = cell.RemoveCandidate(8);

        Assert.Equal("", error);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.Candidates);
        Assert.Equal("9", cell.ToString());
    }

    [Fact]
    public void RemoveCandidate_WhenAllCandidatesRemoved_ReturnsError()
    {
        Cell cell = new(1, 2);

        // Remove candidates 1 through 8
        for (int i = 1; i <= 8; i++)
        {
            cell.RemoveCandidate(i);
        }

        // Cell is now solved to 9. Removing 9 should return no candidates error
        string error = cell.RemoveCandidate(9);

        Assert.Equal("Cell at row 1, column 2 has no candidates.\n", error);
    }

    [Fact]
    public void RemoveCandidate_WhenAlreadySolved_ReturnsErrorOnlyWhenMatchingValue()
    {
        Cell cell = new(0, 0);
        cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]); // Solved to 9
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);

        // Removing a candidate different from solved Value should return empty string
        string nonMatching = cell.RemoveCandidate(4);
        Assert.Equal("", nonMatching);

        // Removing the candidate matching solved Value returns error
        string matching = cell.RemoveCandidate(9);
        Assert.Equal("Cell at row 0, column 0 has no candidates.\n", matching);
    }

    [Fact]
    public void RemoveCandidates_MultipleValues_AppliesAllRemovals()
    {
        Cell cell = new(1, 1);

        string result = cell.RemoveCandidates([2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("", result);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidates_LeavesSingleCandidate_SolvesCell()
    {
        Cell cell = new(1, 1);

        string result = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("", result);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.Candidates);
    }

    [Fact]
    public void Candidates_ReturnsRemainingCandidatesInAscendingOrder()
    {
        Cell cell = new(0, 0);
        cell.RemoveCandidates([7, 2, 5]);

        Assert.Equal([1, 3, 4, 6, 8, 9], cell.Candidates);
    }

    [Fact]
    public void RemoveCandidates_WhenContradictionOccurs_ShortCircuitsAndReturnsError()
    {
        Cell cell = new(3, 3);

        string error = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.Equal("Cell at row 3, column 3 has no candidates.\n", error);
    }

    [Fact]
    public void Solve_PropagatesValueRemovalToLinkedCells()
    {
        Cell cellA = new(0, 0);
        Cell cellB = new(0, 1);

        // Link cellA and cellB
        cellA.Bind([cellA, cellB]);
        cellB.Bind([cellA, cellB]);

        // Restrict cellB to candidates {8, 9}
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]);

        // Solve cellA to 9 by eliminating 1..8
        string result = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("", result);
        Assert.True(cellA.IsSolved);
        Assert.Equal(9, cellA.Value);

        // cellB should have received candidate 9 removal, reducing it to {8} and auto-solving to 8
        Assert.True(cellB.IsSolved);
        Assert.Equal(8, cellB.Value);
    }

    [Fact]
    public void Solve_PropagatesContradictionToLinkedCell_ReturnsError()
    {
        Cell cellA = new(0, 0);
        Cell cellB = new(0, 1);

        // Directly solve cellB to 9
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.True(cellB.IsSolved);
        Assert.Equal(9, cellB.Value);

        // Bind cellB to cellA so that cellA solving to 9 propagates to already-solved cellB
        cellA.Bind([cellB]);

        // When cellA solves to 9, it tries to remove 9 from cellB, causing conflict
        string error = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("Cell at row 0, column 1 has no candidates.\n", error);
    }
}
