using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests.Models;

public class BoardSolveTests
{
    #region Solutions

    [Fact]
    public void Solve_WhenRunForcesNakedSingle_SolvesCell()
    {
        // total 3 over 2 cells -> {1,2}; total 4 over 2 cells -> {1,3}; they intersect only at 1
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedCorner));

        Assert.True(board.Solve());

        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X3X"), board.ToString());
    }

    [Fact]
    public void Solve_SingleCellRuns_SolveToTheirTotals()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.TwoSingleCells));

        Assert.True(board.Solve());

        Assert.True(board.IsSolutionValid());
        Assert.Equal(TestPuzzles.Grid("XXX", "XX9", "X9X"), board.ToString());
    }

    [Fact]
    public void Solve_WhenSolutionNeedsNineAndExcludesNineElsewhere_SolvesUniquely()
    {
        // rows: 3 -> {1,2}, 17 -> {8,9}; columns: 9 -> 1+8, 11 -> 2+9
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedBlock));

        Assert.True(board.Solve());

        Assert.True(board.IsSolutionValid());
        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X89"), board.ToString());
    }

    [Fact]
    public void Solve_UniqueBlock_FindsTheOnlySolution()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UniqueBlock));

        Assert.True(board.Solve());

        Assert.True(board.IsSolutionValid());
        Assert.Equal(TestPuzzles.Grid("XXX", "X97", "X89"), board.ToString());
    }

    [Fact]
    public void Solve_WhenPuzzleRequiresGuessing_FindsValidSolution()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));

        Assert.True(board.Solve());

        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    [Fact]
    public void Solve_OfficialPuzzle_SolvesAndValidates()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.Official41));

        Assert.True(board.Solve());

        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    [Theory]
    [MemberData(nameof(PuzzleFile))]
    public void Solve_EveryPuzzleInTheDataset_SolvesAndValidates(string puzzleText)
    {
        Board board = new(Puzzle.Parse(puzzleText));

        Assert.True(board.Solve());

        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    public static TheoryData<string> PuzzleFile() => TestPuzzles.FromFile();

    [Fact]
    public void Solve_WithNoRuns_ReturnsTrue()
    {
        Board board = new(Puzzle.Parse("test|1|Easy"));

        Assert.True(board.Solve());
    }

    #endregion

    #region Constraint propagation (dirty-run tracking)

    [Theory]
    [InlineData(TestPuzzles.ForcedCorner)]
    [InlineData(TestPuzzles.ForcedBlock)]
    [InlineData(TestPuzzles.UniqueBlock)]
    [InlineData(TestPuzzles.TwoSingleCells)]
    public void Solve_WhenPropagationSuffices_NeverGuesses(string puzzleText)
    {
        // a run must be revisited after a neighbouring run narrows one of its cells; if it isn't, the solver falls back to guessing
        Board board = new(Puzzle.Parse(puzzleText));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.True(board.IsSolutionValid());
        Assert.DoesNotContain(log, message => message.Contains("guess"));
    }

    [Fact]
    public void Solve_FromAPartiallySolvedState_PropagatesWithoutGuessing()
    {
        // a freshly built board has no run marked as changed, so Solve must seed every run itself
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        board.SetState(TestPuzzles.WithPlayableValues(board, 3, 0, 0, 0));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.Equal(TestPuzzles.Grid("XXX", "X37", "X73"), board.ToString());
        Assert.DoesNotContain(log, message => message.Contains("guess"));
    }

    [Fact]
    public void Solve_AfterAnEarlierSolve_ReseedsEveryRunFromTheNewState()
    {
        // the first solve leaves every run clean; a later solve from a different state must not trust that
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        Board.State initial = board.GetState();
        Assert.True(board.Solve());

        board.SetState(initial);
        board.SetState(TestPuzzles.WithPlayableValues(board, 3, 0, 0, 0));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.Equal(TestPuzzles.Grid("XXX", "X37", "X73"), board.ToString());
        Assert.DoesNotContain(log, message => message.Contains("guess"));
    }

    [Fact]
    public void Solve_AfterAFailedSolve_SolvesFromAnotherStateOfTheSameBoard()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedBlock));
        Board.State initial = board.GetState();
        board.SetState(TestPuzzles.WithPlayableValues(board, 9, 0, 0, 0));
        Assert.False(board.Solve());

        board.SetState(initial);

        Assert.True(board.Solve());
        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X89"), board.ToString());
    }

    [Fact]
    public void Solve_WhenOnlyOneGuessIsNeeded_PropagationFinishesTheBoard()
    {
        // a guess marks the runs it touches as changed, so propagation completes the board without a second guess
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.True(board.IsSolutionValid());
        Assert.Single(log, message => message.Contains(": guessing "));
    }

    [Fact]
    public void Solve_ResultDoesNotDependOnHowOftenItIsRepeated()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.Official41));
        Board.State initial = board.GetState();
        Assert.True(board.Solve());
        Board.State first = board.GetState();

        for (int i = 0; i < 3; i++)
        {
            board.SetState(initial);
            Assert.True(board.Solve());
            Assert.Equal(first, board.GetState());
        }
    }

    #endregion

    #region Run total boundaries

    [Theory]
    [InlineData(1, 1)]
    [InlineData(9, 1)]
    [InlineData(3, 2)] // minimum for two cells: 1 + 2
    [InlineData(17, 2)] // maximum for two cells: 8 + 9
    [InlineData(6, 3)]
    [InlineData(24, 3)]
    [InlineData(45, 9)] // the only nine-cell run: 1..9
    public void Solve_RunTotalAtItsLimits_IsSolvable(int total, int length)
    {
        Board board = new(Puzzle.Parse(TestPuzzles.SingleRun(total, length)));

        Assert.True(board.Solve());

        Assert.True(board.IsSolutionValid());
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(2, 2)] // below the minimum of 3
    [InlineData(18, 2)] // above the maximum of 17
    [InlineData(5, 3)]
    [InlineData(25, 3)]
    [InlineData(44, 9)]
    public void Solve_RunTotalOutsideItsLimits_ReturnsFalseAndRestoresBoard(int total, int length)
    {
        Board board = new(Puzzle.Parse(TestPuzzles.SingleRun(total, length)));
        Board.State initial = board.GetState();

        Assert.False(board.Solve());

        Assert.False(board.IsSolved());
        Assert.Equal(initial, board.GetState());
    }

    [Fact]
    public void Solve_NineCellRun_UsesEachDigitOnce()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.SingleRun(45, 9)));

        Assert.True(board.Solve());

        // any ordering is acceptable; what matters is that 1..9 each appear exactly once
        string row = board.ToString().Split(Environment.NewLine)[1];
        string digits = new(row.Where(char.IsDigit).OrderBy(c => c).ToArray());
        Assert.Equal("123456789", digits);
    }

    #endregion

    #region Failure handling

    [Fact]
    public void Solve_ContradictoryIntersectingRuns_ReturnsFalseAndRestoresBoard()
    {
        // (1,1) must be in {1,2} for the row and {8,9} for the column
        Board board = new(Puzzle.Parse(TestPuzzles.Contradictory));
        Board.State initial = board.GetState();

        Assert.False(board.Solve());

        Assert.False(board.IsSolved());
        Assert.Equal(initial, board.GetState());
    }

    [Fact]
    public void Solve_WhenUnsolvableOnlyAfterGuessing_ReturnsFalseAndRestoresBoard()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing));
        Board.State initial = board.GetState();

        Assert.False(board.Solve());

        Assert.False(board.IsSolved());
        Assert.Equal(initial, board.GetState());
    }

    [Fact]
    public void Solve_AfterAFailedSolve_FailsTheSameWayAgain()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing));

        Assert.False(board.Solve());
        Assert.False(board.Solve());
    }

    [Fact]
    public void Solve_AfterRestoringInitialState_ReachesTheSameSolution()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        Board.State initial = board.GetState();
        Assert.True(board.Solve());
        Board.State firstSolution = board.GetState();

        board.SetState(initial);
        Assert.True(board.Solve());

        Assert.Equal(firstSolution, board.GetState());
    }

    [Fact]
    public void Solve_WhenAlreadySolved_ReturnsTrueAndLeavesStateUnchanged()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedCorner));
        Assert.True(board.Solve());
        Board.State solved = board.GetState();

        Assert.True(board.Solve());

        Assert.Equal(solved, board.GetState());
    }

    [Fact]
    public void Solve_FromAPartiallySolvedState_RespectsTheExistingValue()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        board.SetState(TestPuzzles.WithPlayableValues(board, 3, 0, 0, 0)); // (1,1) = 3

        Assert.True(board.Solve());

        Assert.True(board.IsSolutionValid());
        Assert.Equal(TestPuzzles.Grid("XXX", "X37", "X73"), board.ToString());
    }

    [Fact]
    public void Solve_FromAnImpossiblePartialState_ReturnsFalseAndRestoresThatState()
    {
        // (1,1) = 9 can never be in a run of total 3 -> contradiction found by propagation
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedBlock));
        board.SetState(TestPuzzles.WithPlayableValues(board, 9, 0, 0, 0));
        Board.State start = board.GetState();

        Assert.False(board.Solve());

        Assert.Equal(start, board.GetState());
    }

    #endregion

    #region Logging

    [Fact]
    public void Solve_WithoutLogger_DoesNotThrow()
    {
        Assert.True(new Board(Puzzle.Parse(TestPuzzles.GuessingBlock)).Solve(null));
    }

    [Fact]
    public void Solve_WhenPropagationFindsConflict_LogsConflictAndDoesNotGuess()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.Contradictory));
        List<string> log = [];

        Assert.False(board.Solve(log.Add));

        Assert.Single(log, message => message.Contains("constraints conflict detected"));
        Assert.DoesNotContain(log, message => message.Contains("guessing"));
    }

    [Fact]
    public void Solve_WhenPropagationSuffices_LogsOnlyPropagationProgress()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.ForcedBlock));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.NotEmpty(log);
        Assert.DoesNotContain(log, message => message.Contains("guess"));
        Assert.DoesNotContain(log, message => message.Contains("conflict"));
    }

    [Fact]
    public void Solve_WhenGuessing_LogsEachGuessWithItsCell()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.GuessingBlock));
        List<string> log = [];

        Assert.True(board.Solve(log.Add));

        Assert.Contains(log, message => message.StartsWith("Cell (1, 1): guessing "));
    }

    [Fact]
    public void Solve_WhenGuessFails_LogsRevert()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing));
        List<string> log = [];

        Assert.False(board.Solve(log.Add));

        Assert.Contains(log, message => message.Contains("reverting"));
    }

    [Fact]
    public void Solve_WhenEveryGuessFails_LogsOneRevertPerGuess()
    {
        Board board = new(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing));
        List<string> log = [];

        Assert.False(board.Solve(log.Add));

        int guesses = log.Count(message => message.Contains(": guessing "));
        int reverts = log.Count(message => message.Contains("was bad, reverting"));
        Assert.True(guesses > 0);
        Assert.Equal(guesses, reverts);
    }

    #endregion
}
