using System.Collections.Immutable;
using Kakuro.Core.Interfaces;
using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests.Models;

public class BoardStateTests
{
    private static Board CreateBoard(string puzzle = TestPuzzles.Sample) => new(Puzzle.Parse(puzzle));

    #region GetState

    [Fact]
    public void GetState_ReturnsEqualStatesForUnchangedBoard()
    {
        Board board = CreateBoard();

        Assert.Equal(board.GetState(), board.GetState());
        Assert.Equal(board.GetState().GetHashCode(), board.GetState().GetHashCode());
    }

    [Theory]
    [InlineData(TestPuzzles.Sample)]
    [InlineData(TestPuzzles.ForcedCorner)]
    [InlineData(TestPuzzles.TwoSingleCells)]
    public void GetState_HasOneEntryPerPlayableCell(string puzzleText)
    {
        Puzzle puzzle = Puzzle.Parse(puzzleText);
        int playableCells = puzzle.Runs.SelectMany(run => run.GetCellCoordinates()).Distinct().Count();

        Assert.Equal(playableCells, new Board(puzzle).GetState().CellStates.Length);
    }

    [Fact]
    public void States_CompareByContentAcrossBoards()
    {
        Assert.Equal(CreateBoard().GetState(), CreateBoard().GetState());
        Assert.Equal(CreateBoard().GetState().GetHashCode(), CreateBoard().GetState().GetHashCode());
    }

    [Fact]
    public void GetState_DiffersAfterChange()
    {
        Board board = CreateBoard(TestPuzzles.TwoSingleCells);
        Board.State before = board.GetState();

        Assert.True(board.Solve());

        Assert.NotEqual(before, board.GetState());
    }

    [Fact]
    public void GetState_IsASnapshot_NotAffectedByLaterSolving()
    {
        Board board = CreateBoard(TestPuzzles.ForcedCorner);
        Board.State before = board.GetState();

        board.Solve();

        Assert.Equal(CreateBoard(TestPuzzles.ForcedCorner).GetState(), before);
    }

    [Fact]
    public void States_OfDifferentPuzzlesAreNotEqual()
    {
        Assert.NotEqual(CreateBoard(TestPuzzles.Sample).GetState(), CreateBoard(TestPuzzles.TwoSingleCells).GetState());
    }

    [Fact]
    public void States_DifferWhenOnlyOneCellDiffers()
    {
        Board a = CreateBoard(TestPuzzles.ForcedCorner);
        Board b = CreateBoard(TestPuzzles.ForcedCorner);

        b.SetState(TestPuzzles.WithPlayableValues(b, 1, 0, 0));

        Assert.NotEqual(a.GetState(), b.GetState());
    }

    #endregion

    #region SetState

    [Fact]
    public void SetState_RoundTripRestoresBoard()
    {
        Board board = CreateBoard(TestPuzzles.TwoSingleCells);
        Board.State before = board.GetState();

        board.Solve();
        Assert.True(board.IsSolved());
        Assert.NotEqual(before, board.GetState());

        board.SetState(before);

        Assert.False(board.IsSolved());
        Assert.Equal(before, board.GetState());
    }

    [Fact]
    public void SetState_CanRestoreSolvedState()
    {
        Board board = CreateBoard(TestPuzzles.TwoSingleCells);
        Board.State initial = board.GetState();
        board.Solve();
        Board.State solved = board.GetState();

        board.SetState(initial);
        board.SetState(solved);

        Assert.True(board.IsSolved());
        Assert.Equal(solved, board.GetState());
    }

    [Fact]
    public void SetState_AcceptsStateFromAnotherBoardOfTheSamePuzzle()
    {
        Board solved = CreateBoard(TestPuzzles.ForcedCorner);
        solved.Solve();
        Board target = CreateBoard(TestPuzzles.ForcedCorner);

        target.SetState(solved.GetState());

        Assert.True(target.IsSolved());
        Assert.Equal(solved.ToString(), target.ToString());
    }

    [Fact]
    public void SetState_IsRepeatable()
    {
        Board board = CreateBoard(TestPuzzles.ForcedCorner);
        Board.State initial = board.GetState();

        board.SetState(initial);
        board.SetState(initial);

        Assert.Equal(initial, board.GetState());
    }

    [Fact]
    public void SetState_Null_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateBoard().SetState(null!));
    }

    [Fact]
    public void SetState_RejectsDefaultCellStates()
    {
        Assert.Throws<ArgumentException>(() => CreateBoard().SetState(new Board.State(default)));
    }

    [Fact]
    public void SetState_RejectsWrongLength()
    {
        Board board = CreateBoard();
        Board.State other = CreateBoard(TestPuzzles.TwoSingleCells).GetState();

        Assert.Throws<ArgumentException>(() => board.SetState(other));
    }

    [Fact]
    public void SetState_RejectsEmptyStateList()
    {
        Assert.Throws<ArgumentException>(() => CreateBoard().SetState(new Board.State([])));
    }

    [Fact]
    public void SetState_RejectsWrongCellType_AndLeavesBoardUnchanged()
    {
        Board board = CreateBoard();
        Board.State valid = board.GetState();
        board.Solve();
        Board.State current = board.GetState();

        // put an EmptyCell state at the last entry, after earlier entries that would otherwise be applied
        ImmutableArray<ICell.IState> cellStates = valid.CellStates;
        Board.State bad = new(cellStates.SetItem(cellStates.Length - 1, EmptyCell.Instance.GetState()));

        Assert.Throws<ArgumentException>(() => board.SetState(bad));
        Assert.Equal(current, board.GetState());
    }

    #endregion

    #region State equality

    [Fact]
    public void State_DefaultStatesEqualOnlyEachOther()
    {
        Board.State defaultA = new(default);
        Board.State defaultB = new(default);

        Assert.Equal(defaultA, defaultB);
        Assert.Equal(defaultA.GetHashCode(), defaultB.GetHashCode());
        Assert.NotEqual(defaultA, CreateBoard().GetState());
        Assert.NotEqual(CreateBoard().GetState(), defaultA);
    }

    [Fact]
    public void State_IsNotEqualToNull()
    {
        Assert.False(CreateBoard().GetState().Equals(null));
    }

    [Fact]
    public void State_WithDifferentLengths_AreNotEqual()
    {
        Board.State shorter = new([EmptyCell.Instance.GetState()]);
        Board.State longer = new([EmptyCell.Instance.GetState(), EmptyCell.Instance.GetState()]);

        Assert.NotEqual(shorter, longer);
    }

    #endregion
}
