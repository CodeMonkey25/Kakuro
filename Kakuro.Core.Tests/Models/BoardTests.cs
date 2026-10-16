using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests.Models;

/// <summary>Construction, rendering and the IsSolved / IsSolutionValid queries. Solving lives in <see cref="BoardSolveTests"/>.</summary>
public class BoardTests
{
    #region Construction / ToString

    [Fact]
    public void Constructor_InstantiatesGridAndLeavesItUnsolved()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.Sample));

        Assert.False(board.IsSolved());
        Assert.Contains("-------", board.ToString());
        Assert.Contains("|X|", board.ToString());
    }

    [Fact]
    public void Constructor_DoesNotSolveAnything_EvenWhenPropagationCould()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedCorner));

        Assert.Equal(TestPuzzles.Grid("XXX", "X  ", "X X"), board.ToString());
    }

    [Fact]
    public void ToString_UnsolvedCellsAreBlank()
    {
        Board board = new(Puzzle.Parse("test|1|Easy|4,1,1,0,0|4,1,0,1,1"));

        Assert.Equal(TestPuzzles.Grid("XX", "X "), board.ToString());
    }

    [Fact]
    public void ToString_HasOneRowPerPuzzleRowAndDividersBetweenThem()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Sample);
        Board board = new(puzzle);

        string[] lines = board.ToString().Split(Environment.NewLine);

        Assert.Equal(puzzle.Rows * 2 + 1, lines.Length);
        Assert.All(lines.Where((_, i) => i % 2 == 0), line => Assert.Equal(new string('-', puzzle.Columns * 2 + 1), line));
        Assert.All(lines.Where((_, i) => i % 2 == 1), line => Assert.Equal(puzzle.Columns * 2 + 1, line.Length));
    }

    [Fact]
    public void ToString_WithNoRuns_RendersSingleEmptyCell()
    {
        Board board = new(Puzzle.Parse("test|1|Easy"));

        Assert.Equal(TestPuzzles.Grid("X"), board.ToString());
    }

    [Theory]
    [InlineData("test|1|Easy|3,2,0,1,1|4,2,4,0,0")] // horizontal run on row 4 exceeds the vertical run's extent
    [InlineData("test|2|Easy|4,2,0,0,0|3,2,0,4,1")] // vertical run on column 4 exceeds the horizontal run's extent
    [InlineData("test|3|Easy|4,2,2,0,0")] // only horizontal runs
    [InlineData("test|4|Easy|4,2,0,2,1")] // only vertical runs
    public void Constructor_WhenRunCoordinatesExceedOppositeOrientationDimensions_InstantiatesBoardSuccessfully(string puzzleString)
    {
        Puzzle puzzle = Puzzle.Parse(puzzleString);

        // dimensions must account for both orientations, or cell instantiation would index out of range
        Board board = new(puzzle);

        string[] lines = board.ToString().Split(Environment.NewLine);
        Assert.Equal(puzzle.Rows * 2 + 1, lines.Length);
    }

    [Fact]
    public void Constructor_RunsSharingACell_ShareTheSameCellInstance()
    {
        // (1,1) belongs to both runs; solving it through one run must be visible through the other
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedCorner));

        Assert.True(board.Solve());

        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X3X"), board.ToString());
    }

    #endregion

    #region IsSolved

    [Fact]
    public void IsSolved_FreshBoard_IsFalse()
    {
        Assert.False(new Board(Puzzle.Parse(TestPuzzles.TwoSingleCells)).IsSolved());
    }

    [Fact]
    public void IsSolved_AfterSolve_IsTrue()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.TwoSingleCells));

        Assert.True(board.Solve());

        Assert.True(board.IsSolved());
        Assert.Equal(TestPuzzles.Grid("XXX", "XX9", "X9X"), board.ToString());
    }

    [Fact]
    public void IsSolved_WithSomeCellsSolved_IsFalse()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.TwoSingleCells));

        board.SetState(TestPuzzles.WithPlayableValues(board, 9, 0));

        Assert.False(board.IsSolved());
    }

    [Fact]
    public void IsSolved_WithNoPlayableCells_IsTrue()
    {
        Assert.True(new Board(Puzzle.Parse("test|1|Easy")).IsSolved());
    }

    #endregion

    #region IsSolutionValid

    [Fact]
    public void IsSolutionValid_WhenUnsolved_ReturnsFalse()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.Sample));

        Assert.False(board.IsSolved());
        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenSolvedWithCorrectTotals_ReturnsTrue()
    {
        Board board = new(Puzzle.Parse("test|1|Easy|3,2,1,0,0"));

        board.SetState(TestPuzzles.WithPlayableValues(board, 1, 2));

        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenRunHasDuplicateDigits_ReturnsFalse()
    {
        // 1 + 1 equals the run total of 2, so only the distinct-digits rule can reject it
        Board board = new(Puzzle.Parse("test|1|Easy|2,2,1,0,0"));

        board.SetState(TestPuzzles.WithPlayableValues(board, 1, 1));

        Assert.True(board.IsSolved());
        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenRunSumIsWrong_ReturnsFalse()
    {
        Board board = new(Puzzle.Parse("test|1|Easy|3,2,1,0,0"));

        board.SetState(TestPuzzles.WithPlayableValues(board, 1, 3));

        Assert.True(board.IsSolved());
        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_ChecksEveryRun_NotJustTheFirst()
    {
        // first run (1+2=3) is right, second run (1+1 vs 4) is wrong
        Board board = new(Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,2,0,0"));

        board.SetState(TestPuzzles.WithPlayableValues(board, 1, 2, 1, 1));

        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenOnlySomeRunsAreSolved_ReturnsFalse()
    {
        Board board = new(Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,2,0,0"));

        board.SetState(TestPuzzles.WithPlayableValues(board, 1, 2, 0, 0));

        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenSolvedByTheSolver_ReturnsTrue()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UniqueBlock));
        board.Solve();

        Assert.True(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WithNoRuns_IsVacuouslyTrue()
    {
        Assert.True(new Board(Puzzle.Parse("test|1|Easy")).IsSolutionValid());
    }

    #endregion
}
