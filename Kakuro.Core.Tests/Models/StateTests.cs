using System.Collections.Immutable;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class StateTests
{
    private const string SamplePuzzleString = "kakuro.com|1|Easy|16,2,0,1,0|12,2,0,2,1|3,2,1,0,0|7,2,2,0,0";
    private const string SingleCellPuzzleString = "test|1|Easy|9,1,1,1,0|9,1,1,1,1";

    private static Board CreateBoard(string puzzle = SamplePuzzleString) => new(Puzzle.Parse(puzzle));

    [Fact]
    public void EmptyCell_GetState_ReturnsEqualStates()
    {
        Assert.Equal(EmptyCell.Instance.GetState(), EmptyCell.Instance.GetState());
    }

    [Fact]
    public void EmptyCell_SetState_RejectsPlayableCellState()
    {
        Cell.State playableState = new PlayableCell(0, 0).GetState();

        Assert.False(EmptyCell.Instance.IsValidState(playableState));
        Assert.Throws<ArgumentException>(() => EmptyCell.Instance.SetState(playableState));
    }

    [Fact]
    public void PlayableCell_SetState_RejectsEmptyCellState()
    {
        PlayableCell cell = new(0, 0);
        Cell.State emptyState = EmptyCell.Instance.GetState();

        Assert.False(cell.IsValidState(emptyState));
        Assert.Throws<ArgumentException>(() => cell.SetState(emptyState));
    }

    [Fact]
    public void PlayableCell_States_CompareByContent()
    {
        PlayableCell a = new(0, 0);
        PlayableCell b = new(1, 1);

        Assert.Equal(a.GetState(), b.GetState());
        Assert.Equal(a.GetState().GetHashCode(), b.GetState().GetHashCode());

        b.RemoveCandidate(5);
        Assert.NotEqual(a.GetState(), b.GetState());
    }

    [Fact]
    public void PlayableCell_SetState_RestoresCandidates()
    {
        PlayableCell cell = new(0, 0);
        Cell.State original = cell.GetState();

        cell.RemoveCandidates([1, 2, 3]);
        Assert.NotEqual(original, cell.GetState());

        cell.SetState(original);

        Assert.Equal(original, cell.GetState());
        Assert.Equal(9, cell.Candidates.Count);
        Assert.False(cell.IsSolved);
    }

    [Fact]
    public void PlayableCell_SetState_RestoresSolvedValue()
    {
        PlayableCell cell = new(0, 0);
        Cell.State original = cell.GetState();

        cell.RemoveCandidates([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.True(cell.IsSolved);
        Assert.Equal(9, cell.Value);

        cell.SetState(original);

        Assert.False(cell.IsSolved);
        Assert.Equal(0, cell.Value);
        Assert.Equal(9, cell.Candidates.Count);
    }

    [Fact]
    public void Board_GetState_ReturnsEqualStatesForUnchangedBoard()
    {
        Board board = CreateBoard();

        Assert.Equal(board.GetState(), board.GetState());
        Assert.Equal(board.GetState().GetHashCode(), board.GetState().GetHashCode());
    }

    [Fact]
    public void Board_States_CompareByContentAcrossBoards()
    {
        Assert.Equal(CreateBoard().GetState(), CreateBoard().GetState());
    }

    [Fact]
    public void Board_GetState_DiffersAfterChange()
    {
        Board board = CreateBoard(SingleCellPuzzleString);
        Board.State before = board.GetState();

        Assert.True(board.Solve(null));

        Assert.NotEqual(before, board.GetState());
    }

    [Fact]
    public void Board_SetState_RoundTripRestoresBoard()
    {
        Board board = CreateBoard(SingleCellPuzzleString);
        Board.State before = board.GetState();

        board.Solve(null);
        Assert.True(board.IsSolved());
        Assert.NotEqual(before, board.GetState());

        board.SetState(before);

        Assert.False(board.IsSolved());
        Assert.Equal(before, board.GetState());
    }

    [Fact]
    public void Board_SetState_CanRestoreSolvedState()
    {
        Board board = CreateBoard(SingleCellPuzzleString);
        Board.State initial = board.GetState();
        board.Solve(null);
        Board.State solved = board.GetState();

        board.SetState(initial);
        board.SetState(solved);

        Assert.True(board.IsSolved());
        Assert.Equal(solved, board.GetState());
    }

    [Fact]
    public void Board_SetState_RejectsDefaultState()
    {
        Board board = CreateBoard();

        Assert.Throws<ArgumentException>(() => board.SetState(default(Board.State)!));
        Assert.Throws<ArgumentException>(() => board.SetState(new Board.State(default)));
    }

    [Fact]
    public void Board_SetState_RejectsWrongLength()
    {
        Board board = CreateBoard();
        Board.State other = CreateBoard(SingleCellPuzzleString).GetState();

        Assert.Throws<ArgumentException>(() => board.SetState(other));
    }

    [Fact]
    public void Board_SetState_RejectsWrongCellType_AndLeavesBoardUnchanged()
    {
        Board board = CreateBoard();
        Board.State valid = board.GetState();
        board.Solve(null);
        Board.State current = board.GetState();

        // put an EmptyCell state in the last position, where there is a playable cell,
        // after a valid-looking earlier entry that would otherwise be applied
        ImmutableArray<Cell.State> cellStates = valid.CellStates;
        int playableIndex = cellStates.Select((s, i) => (s, i))
            .Last(t => !t.s.Equals(EmptyCell.Instance.GetState())).i;
        Board.State bad = new(cellStates.SetItem(playableIndex, EmptyCell.Instance.GetState()));

        Assert.Throws<ArgumentException>(() => board.SetState(bad));
        Assert.Equal(current, board.GetState());
    }

    [Fact]
    public void Board_State_DefaultStatesEqualOnlyEachOther()
    {
        Board.State defaultA = new(default);
        Board.State defaultB = new(default);

        Assert.Equal(defaultA, defaultB);
        Assert.Equal(defaultA.GetHashCode(), defaultB.GetHashCode());
        Assert.NotEqual(defaultA, CreateBoard().GetState());
    }
}
