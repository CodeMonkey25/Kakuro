using Kakuro.Core.Interfaces;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PlayableCellTests
{
    private const string NoCandidatesAt00 = "Cell at row 0, column 0 has no candidates.";

    private static uint MaskOf(params int[] values) => values.Aggregate(0u, (m, v) => m | 1u << (v - 1));

    #region Construction

    [Fact]
    public void Constructor_InitializesStateAndCoordinates()
    {
        PlayableCell cell = new(3, 5);

        Assert.Equal(3, cell.Row);
        Assert.Equal(5, cell.Column);
        Assert.Equal(0, cell.Value);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
        Assert.Empty(cell.LinkedCells);
        Assert.Equal(" ", cell.ToString());
    }

    [Fact]
    public void IsInstanceOfCell()
    {
        Assert.IsAssignableFrom<ICell>(new PlayableCell(0, 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public void ToString_WhenSolved_ReturnsTheDigit(int value)
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(value);

        Assert.Equal(value.ToString(), cell.ToString());
    }

    [Fact]
    public void CandidateCount_ReflectsRemovals()
    {
        PlayableCell cell = new(0, 0);

        cell.RemoveCandidate(9);

        Assert.Equal(8, cell.CandidateCount);
        Assert.DoesNotContain(9, cell.CandidatesArray());
    }

    [Fact]
    public void Candidates_AreInAscendingOrderAfterRemovals()
    {
        PlayableCell cell = new(0, 0);
        var (_, removed) = cell.RemoveCandidates([7, 2, 5]);

        Assert.True(removed);
        Assert.Equal([1, 3, 4, 6, 8, 9], cell.CandidatesArray());
    }

    #endregion

    #region Bind

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
    public void Bind_MultiplePeerGroups_MergesWithoutDuplicates()
    {
        PlayableCell mainCell = new(1, 1);
        PlayableCell horizontalPeer = new(1, 2);
        PlayableCell verticalPeer = new(2, 1);

        // Simulate binding horizontal run then vertical run
        mainCell.Bind([mainCell, horizontalPeer]);
        mainCell.Bind([mainCell, verticalPeer, horizontalPeer]);

        Assert.Equal([horizontalPeer, verticalPeer], mainCell.LinkedCells);
    }

    [Fact]
    public void Bind_WithNothingOrOnlySelf_LinksNothing()
    {
        PlayableCell cell = new(0, 0);

        cell.Bind([]);
        cell.Bind([cell]);

        Assert.Empty(cell.LinkedCells);
    }

    [Fact]
    public void Bind_IsOneDirectional()
    {
        PlayableCell a = new(0, 0);
        PlayableCell b = new(0, 1);

        a.Bind([b]);

        Assert.Contains(b, a.LinkedCells);
        Assert.Empty(b.LinkedCells);
    }

    #endregion

    #region RemoveCandidate

    [Fact]
    public void RemoveCandidate_UnsolvedCell_RemovesCandidateWithoutSolving()
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidate(3);

        Assert.Null(error);
        Assert.True(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal(0, cell.Value);
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
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
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(32)]
    public void RemoveCandidate_OutOfRangeValue_IsANoOp(int candidate)
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidate(candidate);

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidate_DownToOneCandidate_SolvesCellAndUpdatesString()
    {
        PlayableCell cell = new(2, 4);

        // Remove candidates 1 through 7, leaving 8 and 9
        for (int i = 1; i <= 7; i++)
        {
            (string? error, bool removed) = cell.RemoveCandidate(i);
            Assert.Null(error);
            Assert.True(removed);
            Assert.False(cell.IsSolved);
        }
        Assert.Equal([8, 9], cell.CandidatesArray());

        (string? finalError, bool finalRemoved) = cell.RemoveCandidate(8);

        Assert.Null(finalError);
        Assert.True(finalRemoved);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.CandidatesArray());
        Assert.Equal("9", cell.ToString());
    }

    [Fact]
    public void RemoveCandidate_DownToOneCandidate_CanSolveToTheLowestDigit()
    {
        PlayableCell cell = new(0, 0);
        cell.RemoveCandidates([3, 4, 5, 6, 7, 8, 9]);

        (string? error, bool removed) = cell.RemoveCandidate(2);

        Assert.Null(error);
        Assert.True(removed);
        Assert.Equal(1, cell.Value);
    }

    [Fact]
    public void RemoveCandidate_WhenSolvedCellsOnlyCandidateIsRemoved_ReturnsError()
    {
        PlayableCell cell = new(1, 2);
        for (int i = 1; i <= 8; i++)
        {
            cell.RemoveCandidate(i);
        }

        // Cell is now solved to 9. Removing 9 should return no candidates error
        (string? error, bool removed) = cell.RemoveCandidate(9);

        Assert.Equal("Cell at row 1, column 2 has no candidates.", error);
        Assert.False(removed);
        Assert.Equal(9, cell.Value);
    }

    [Fact]
    public void RemoveCandidate_WhenAlreadySolved_ReturnsErrorOnlyWhenMatchingValue()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(9);

        (string? nonMatchingError, bool nonMatchingRemoved) = cell.RemoveCandidate(4);
        Assert.Null(nonMatchingError);
        Assert.False(nonMatchingRemoved);

        (string? matchingError, bool matchingRemoved) = cell.RemoveCandidate(9);
        Assert.Equal(NoCandidatesAt00, matchingError);
        Assert.False(matchingRemoved);
    }

    [Fact]
    public void RemoveCandidate_WhenItSolvesTheCellAgainstASolvedPeer_ReturnsErrorAndReportsRemoval()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        peer.Solve(9);
        cell.Bind([peer]);
        cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]); // {8, 9}

        // leaves {9}: the cell solves itself to 9, which the already-solved peer cannot give up
        (string? error, bool removed) = cell.RemoveCandidate(8);

        Assert.Equal("Cell at row 0, column 1 has no candidates.", error);
        Assert.True(removed);
    }

    #endregion

    #region RemoveCandidates (enumerable)

    [Fact]
    public void RemoveCandidates_MultipleValues_AppliesAllRemovals()
    {
        PlayableCell cell = new(1, 1);

        (string? result, bool removed) = cell.RemoveCandidates([2, 3, 4, 5, 6, 7, 8]);

        Assert.Null(result);
        Assert.True(removed);
        Assert.False(cell.IsSolved);
        Assert.Equal([1, 9], cell.CandidatesArray());
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
        Assert.Equal([1, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_WhenEmptyListProvided_ReturnsFalseAndNoError()
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidates([]);

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_WithRepeatedValues_RemovesOnceWithoutError()
    {
        PlayableCell cell = new(0, 0);

        (string? error, bool removed) = cell.RemoveCandidates([3, 3, 3]);

        Assert.Null(error);
        Assert.True(removed);
        Assert.Equal([1, 2, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
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
        Assert.Equal([9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_WhenContradictionOccurs_ReturnsErrorAndReportsRemoval()
    {
        PlayableCell cell = new(3, 3);

        (string? error, bool removed) = cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.Equal("Cell at row 3, column 3 has no candidates.", error);
        Assert.True(removed);
    }

    [Fact]
    public void RemoveCandidates_OnSolvedCell_WhenValueIsNotListed_ReturnsNoErrorAndChangesNothing()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(9);

        (string? error, bool removed) = cell.RemoveCandidates([1, 2, 3]);

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal(9, cell.Value);
        Assert.Equal([9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_OnSolvedCell_WhenValueIsListed_ReturnsErrorAndKeepsValue()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(9);

        (string? error, bool removed) = cell.RemoveCandidates([8, 9, 7]);

        Assert.Equal(NoCandidatesAt00, error);
        Assert.False(removed);
        Assert.Equal(9, cell.Value);
    }

    #endregion

    #region RemoveCandidates (mask)

    [Fact]
    public void RemoveCandidates_Mask_OnSolvedCell_ReportsErrorOnlyWhenValueIsMasked()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(4);

        Assert.Equal((null, false), cell.RemoveCandidates(MaskOf(1, 2, 3, 9)));
        Assert.Equal((NoCandidatesAt00, false), cell.RemoveCandidates(MaskOf(4)));
        Assert.Equal(4, cell.Value);
    }

    [Fact]
    public void RemoveCandidates_Mask_RemovesAllMaskedValues_IncludingNine()
    {
        PlayableCell cell = new(1, 1);

        (string? error, bool removed) = cell.RemoveCandidates(MaskOf(2, 3, 4, 5, 6, 7, 8, 9));

        Assert.Null(error);
        Assert.True(removed);
        Assert.True(cell.IsSolved);
        Assert.Equal(1, cell.Value);
    }

    [Fact]
    public void RemoveCandidates_Mask_WithEmptyMask_ChangesNothing()
    {
        PlayableCell cell = new(1, 1);

        (string? error, bool removed) = cell.RemoveCandidates(0u);

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_Mask_WhenNothingToRemove_ReturnsFalseAndNoError()
    {
        PlayableCell cell = new(1, 1);
        cell.RemoveCandidates(MaskOf(2, 3));

        (string? error, bool removed) = cell.RemoveCandidates(MaskOf(2, 3));

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_Mask_BitsAboveNine_AreIgnored()
    {
        PlayableCell cell = new(1, 1);

        (string? error, bool removed) = cell.RemoveCandidates((1u << 9) | (1u << 20) | (1u << 31));

        Assert.Null(error);
        Assert.False(removed);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_Mask_LeavesSingleCandidate_SolvesCell()
    {
        PlayableCell cell = new(1, 1);

        (string? error, bool removed) = cell.RemoveCandidates(MaskOf(1, 2, 3, 4, 5, 6, 7, 8));

        Assert.Null(error);
        Assert.True(removed);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);
    }

    [Fact]
    public void RemoveCandidates_Mask_WhenAllRemoved_ReturnsErrorAndReportsRemoval()
    {
        PlayableCell cell = new(3, 3);

        (string? error, bool removed) = cell.RemoveCandidates(MaskOf(1, 2, 3, 4, 5, 6, 7, 8, 9));

        Assert.Equal("Cell at row 3, column 3 has no candidates.", error);
        Assert.True(removed);
    }

    [Fact]
    public void RemoveCandidates_Mask_WhenSolved_ErrorsOnlyIfMaskContainsValue()
    {
        PlayableCell cell = new(0, 0);
        cell.Solve(9);

        Assert.Null(cell.RemoveCandidates(MaskOf(1, 2, 3)).error);
        Assert.Equal(NoCandidatesAt00, cell.RemoveCandidates(MaskOf(3, 9)).error);
    }

    [Fact]
    public void RemoveCandidates_Mask_MatchesEnumerableOverload()
    {
        int[] values = [1, 4, 9];
        PlayableCell viaMask = new(0, 0);
        PlayableCell viaEnumerable = new(0, 0);

        viaMask.RemoveCandidates(MaskOf(values));
        viaEnumerable.RemoveCandidates(values);

        Assert.Equal(viaEnumerable.CandidatesArray(), viaMask.CandidatesArray());
    }

    [Fact]
    public void RemoveCandidates_Mask_EverySingleCandidate_MatchesRemoveCandidate()
    {
        for (int candidate = 1; candidate <= 9; candidate++)
        {
            PlayableCell viaMask = new(0, 0);
            PlayableCell viaSingle = new(0, 0);

            var maskResult = viaMask.RemoveCandidates(MaskOf(candidate));
            var singleResult = viaSingle.RemoveCandidate(candidate);

            Assert.Equal(singleResult, maskResult);
            Assert.Equal(viaSingle.CandidatesArray(), viaMask.CandidatesArray());
        }
    }

    #endregion

    #region Solve

    [Fact]
    public void Solve_Directly_SetsValueAndCollapsesCandidates()
    {
        PlayableCell cell = new(0, 0);

        string? error = cell.Solve(4);

        Assert.Null(error);
        Assert.True(cell.IsSolved);
        Assert.Equal(4, cell.Value);
        Assert.Equal([4], cell.CandidatesArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Solve_AcceptsBoundaryValues(int value)
    {
        PlayableCell cell = new(0, 0);

        Assert.Null(cell.Solve(value));
        Assert.Equal(value, cell.Value);
    }

    [Fact]
    public void Solve_RemovesValueFromLinkedCells()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        cell.Bind([cell, peer]);
        peer.Bind([cell, peer]);

        string? error = cell.Solve(4);

        Assert.Null(error);
        Assert.DoesNotContain(4, peer.CandidatesArray());
        Assert.Equal(8, peer.CandidateCount);
    }

    [Fact]
    public void Solve_DoesNotTouchUnlinkedCells()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell stranger = new(5, 5);

        cell.Solve(4);

        Assert.Equal(9, stranger.CandidateCount);
    }

    [Fact]
    public void Solve_WhenAlreadySolved_ReturnsErrorOnlyForDifferentValue()
    {
        PlayableCell cell = new(2, 3);
        cell.Solve(4);

        Assert.Null(cell.Solve(4));
        Assert.Equal("Cell at row 2, column 3 is already solved.", cell.Solve(5));
        Assert.Equal(4, cell.Value);
        Assert.Equal([4], cell.CandidatesArray());
    }

    [Fact]
    public void Solve_WhenAlreadySolved_ReportsAlreadySolvedRatherThanMissingCandidate()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        cell.Bind([peer]);
        cell.Solve(4);
        ICell.IState before = cell.GetState();

        // 7 is not a candidate (the cell collapsed to {4}), but the solved check takes precedence
        Assert.Equal("Cell at row 0, column 0 is already solved.", cell.Solve(7));
        Assert.Equal(before, cell.GetState());
    }

    [Fact]
    public void Solve_PropagatesValueRemovalToLinkedCells()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);
        cellA.Bind([cellA, cellB]);
        cellB.Bind([cellA, cellB]);
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7]); // {8, 9}

        // Solve cellA to 9 by eliminating 1..8
        (string? result, bool removed) = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Null(result);
        Assert.True(removed);
        Assert.Equal(9, cellA.Value);

        // cellB lost 9, leaving {8}, so it auto-solved
        Assert.True(cellB.IsSolved);
        Assert.Equal(8, cellB.Value);
    }

    [Fact]
    public void Solve_PropagatesContradictionToLinkedCell_ReturnsError()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);
        cellB.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.Equal(9, cellB.Value);
        cellA.Bind([cellB]);

        (string? error, _) = cellA.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);

        Assert.Equal("Cell at row 0, column 1 has no candidates.", error);
    }

    [Fact]
    public void Solve_WhenLinkedCellIsAlreadySolvedToTheSameValue_ReturnsError()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        peer.Solve(4);
        cell.Bind([peer]);

        Assert.Equal("Cell at row 0, column 1 has no candidates.", cell.Solve(4));
    }

    [Fact]
    public void Solve_CascadingChainReaction_PropagatesAcrossMultipleLinkedCells()
    {
        PlayableCell cellA = new(0, 0);
        PlayableCell cellB = new(0, 1);
        PlayableCell cellC = new(0, 2);
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
        Assert.Equal(8, cellA.Value);
        Assert.Equal(7, cellB.Value); // lost 8
        Assert.Equal(6, cellC.Value); // lost 7
    }

    [Fact]
    public void Solve_WithValueThatIsNoLongerACandidate_ReturnsErrorAndLeavesCellUnchanged()
    {
        PlayableCell cell = new(2, 3);
        cell.RemoveCandidate(5);
        ICell.IState before = cell.GetState();

        string? error = cell.Solve(5);

        Assert.Equal("Cell at row 2, column 3 does not contain candidate 5.", error);
        Assert.Equal(before, cell.GetState());
        Assert.False(cell.IsSolved);
    }

    [Fact]
    public void Solve_WithValueThatIsNoLongerACandidate_DoesNotTouchLinkedCells()
    {
        PlayableCell cell = new(0, 0);
        PlayableCell peer = new(0, 1);
        cell.Bind([peer]);
        cell.RemoveCandidate(5);

        cell.Solve(5);

        Assert.Equal(9, peer.CandidateCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public void Solve_WithOutOfRangeValue_ThrowsAndLeavesCellUnchanged(int value)
    {
        PlayableCell cell = new(0, 0);
        ICell.IState before = cell.GetState();

        Assert.Throws<ArgumentOutOfRangeException>(() => cell.Solve(value));

        Assert.Equal(before, cell.GetState());
        Assert.False(cell.IsSolved);
    }

    #endregion
}
