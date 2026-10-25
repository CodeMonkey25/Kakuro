using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests;

public class EngineTests
{
    #region Puzzle solving & Lifecycle

    [Fact]
    public void Solve_WhenSolutionIsValid_ReturnsTrueAndLogsBoardThenValidMessage()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedCorner);
        List<string> log = [];
        Engine engine = new(log.Add);

        Assert.True(engine.Solve(puzzle));

        int boardIndex = log.IndexOf(TestPuzzles.Grid("XXX", "X12", "X3X"));
        int validIndex = log.IndexOf("Solution is valid");
        Assert.True(boardIndex >= 0, "the solved board was not logged");
        Assert.True(validIndex > boardIndex, "validity message should follow the board");
        Assert.DoesNotContain("Unable to solve puzzle", log);
        Assert.DoesNotContain("Solution is invalid", log);
    }

    [Fact]
    public void Solve_LogsStartMessageFirst()
    {
        List<string> log = [];

        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.ForcedCorner));

        Assert.Equal("Solving puzzle...", log[0]);
    }

    [Fact]
    public void Solve_WhenPuzzleIsUnsolvable_ReturnsFalseAndLogsFailureWithoutValidityMessage()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Contradictory);
        List<string> log = [];
        Engine engine = new(log.Add);

        Assert.False(engine.Solve(puzzle));

        Assert.Equal("Unable to solve puzzle", log[^1]);
        Assert.DoesNotContain("Solution is valid", log);
        Assert.DoesNotContain("Solution is invalid", log);
    }

    [Fact]
    public void Solve_WhenPuzzleIsUnsolvable_LogsOnlyTheInitialBoard()
    {
        List<string> log = [];

        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.Contradictory));

        List<string> boards = log.Where(message => message.Contains('|') && message.Contains('-')).ToList();
        Assert.Single(boards);
        Assert.Equal(log[1], boards[0]);
    }

    [Fact]
    public void Solve_ForwardsTheBoardsDiagnosticsToTheLogger()
    {
        List<string> log = [];

        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.Contradictory));
        Assert.Contains(log, message => message.Contains("Constraints conflict detected"));

        log.Clear();
        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.GuessingBlock));
        Assert.Contains(log, message => message.Contains("guessing"));
    }

    [Fact]
    public void Solve_WithoutLogger_DoesNotThrow()
    {
        Assert.True(new Engine().Solve(Puzzle.Parse(TestPuzzles.ForcedCorner)));
        Assert.False(new Engine().Solve(Puzzle.Parse(TestPuzzles.Contradictory)));
    }

    [Fact]
    public void Solve_CanBeCalledRepeatedlyOnTheSameEngineAndPuzzle()
    {
        Engine engine = new();
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);

        Assert.True(engine.Solve(puzzle));
        Assert.True(engine.Solve(puzzle));
    }

    [Fact]
    public void Solve_DoesNotModifyThePuzzle()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Sample);
        string before = puzzle.ToString();

        new Engine().Solve(puzzle);

        Assert.Equal(before, puzzle.ToString());
    }

    [Theory]
    [MemberData(nameof(PuzzleFile))]
    public void Solve_EveryPuzzleInTheDataset_ReportsAValidSolution(string puzzleText)
    {
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(Puzzle.Parse(puzzleText)));

        Assert.Contains("Solution is valid", log);
    }

    public static TheoryData<string> PuzzleFile() => TestPuzzles.FromFile();

    #endregion

    #region Solutions

    [Fact]
    public void Solve_WhenRunForcesNakedSingle_SolvesCell()
    {
        // total 3 over 2 cells -> {1,2}; total 4 over 2 cells -> {1,3}; they intersect only at 1
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedCorner);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains(TestPuzzles.Grid("XXX", "X12", "X3X"), log);
    }

    [Fact]
    public void Solve_SingleCellRuns_SolveToTheirTotals()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.TwoSingleCells);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
        Assert.Contains(TestPuzzles.Grid("XXX", "XX9", "X9X"), log);
    }

    [Fact]
    public void Solve_WhenSolutionNeedsNineAndExcludesNineElsewhere_SolvesUniquely()
    {
        // rows: 3 -> {1,2}, 17 -> {8,9}; columns: 9 -> 1+8, 11 -> 2+9
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
        Assert.Contains(TestPuzzles.Grid("XXX", "X12", "X89"), log);
    }

    [Fact]
    public void Solve_UniqueBlock_FindsTheOnlySolution()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UniqueBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
        Assert.Contains(TestPuzzles.Grid("XXX", "X97", "X89"), log);
    }

    [Fact]
    public void Solve_WhenPuzzleRequiresGuessing_FindsValidSolution()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
    }

    [Fact]
    public void Solve_OfficialPuzzle_SolvesAndValidates()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Official41);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
    }

    [Fact]
    public void Solve_WithNoRuns_ReturnsTrue()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy");

        Assert.True(new Engine().Solve(puzzle));
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
        // a run must be revisited after a neighboring run narrows one of its cells; if it isn't, the solver falls back to guessing
        Puzzle puzzle = Puzzle.Parse(puzzleText);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
        Assert.DoesNotContain(log, message => message.Contains("guess"));
    }

    [Fact]
    public void Solve_WhenOnlyOneGuessIsNeeded_PropagationFinishesTheBoard()
    {
        // a guess marks the runs it touches as changed, so propagation completes the board without a second guess
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Single(log, message => message.Contains(": guessing "));
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
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.SingleRun(total, length));
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains("Solution is valid", log);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(2, 2)] // below the minimum of 3
    [InlineData(18, 2)] // above the maximum of 17
    [InlineData(5, 3)]
    [InlineData(25, 3)]
    [InlineData(44, 9)]
    public void Solve_RunTotalOutsideItsLimits_ReturnsFalse(int total, int length)
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.SingleRun(total, length));

        Assert.False(new Engine().Solve(puzzle));
    }

    [Fact]
    public void Solve_NineCellRun_UsesEachDigitOnce()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.SingleRun(45, 9));
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        // any ordering is acceptable; what matters is that 1..9 each appear exactly once
        string boardString = log.Where(m => m.Contains('|') && m.Contains('-')).Last();
        string row = boardString.Split(Environment.NewLine)[1];
        string digits = new(row.Where(char.IsDigit).Order().ToArray());
        Assert.Equal("123456789", digits);
    }

    #endregion

    #region Failure handling

    [Fact]
    public void Solve_ContradictoryIntersectingRuns_ReturnsFalse()
    {
        // (1,1) must be in {1,2} for the row and {8,9} for the column
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Contradictory);

        Assert.False(new Engine().Solve(puzzle));
    }

    [Fact]
    public void Solve_WhenUnsolvableOnlyAfterGuessing_ReturnsFalse()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing);

        Assert.False(new Engine().Solve(puzzle));
    }

    [Fact]
    public void Solve_AfterAFailedSolve_FailsTheSameWayAgain()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing);
        Engine engine = new();

        Assert.False(engine.Solve(puzzle));
        Assert.False(engine.Solve(puzzle));
    }

    #endregion

    #region Logging

    [Fact]
    public void Solve_WhenPropagationFindsConflict_LogsConflictAndDoesNotGuess()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Contradictory);
        List<string> log = [];

        Assert.False(new Engine(log.Add).Solve(puzzle));

        Assert.Single(log, message => message.Contains("Constraints conflict detected"));
        Assert.DoesNotContain(log, message => message.Contains("guessing"));
    }

    [Fact]
    public void Solve_LogsPrecomputedConstraintsBeforeInitialConstraints()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        int precomputed = log.FindIndex(message => message.Contains("Loading precomputed constraints"));
        int initial = log.FindIndex(message => message.Contains("Calculating initial constraints"));
        Assert.True(precomputed >= 0);
        Assert.True(initial > precomputed);
    }

    [Fact]
    public void Solve_LogsEachRunFoundInTheTable()
    {
        // ForcedBlock rows have totals 3 and 17 over two cells (unique digit sets); columns 9 and 11 are not in the table
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains($"{Constants.LogIndent}Run (2, 3): candidates removed [3, 4, 5, 6, 7, 8, 9]...", log);
        Assert.Contains($"{Constants.LogIndent}Run (2, 17): candidates removed [1, 2, 3, 4, 5, 6, 7]...", log);
        Assert.Single(log, message => message.Contains("Run (2, 3): candidates removed"));
        Assert.Single(log, message => message.Contains("Run (2, 17): candidates removed"));
        Assert.DoesNotContain(log, message => message.Contains("Run (2, 9): candidates removed") || message.Contains("Run (2, 11): candidates removed"));
    }

    [Fact]
    public void Solve_WhenNoRunIsInTheTable_LogsNoFoundRuns()
    {
        // total 9 over two cells has four digit sets, so nothing is precomputed
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.SingleRun(9, 2));
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.DoesNotContain(log, message => message.Contains("candidates reduced to"));
    }

    [Fact]
    public void Solve_WhenPropagationSuffices_LogsOnlyPropagationProgress()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.NotEmpty(log);
        Assert.DoesNotContain(log, message => message.Contains("guess"));
        Assert.DoesNotContain(log, message => message.Contains("conflict"));
    }

    [Fact]
    public void Solve_WhenGuessing_LogsEachGuessWithItsCell()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains(log, message => message.Contains("Cell (1, 1): guessing "));
    }

    [Fact]
    public void Solve_WhenGuessing_LogsGuessCountOnSuccess()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains(log, message => message.StartsWith("Solved in 1 guesses, not including 0 bad guesses."));
    }

    [Fact]
    public void Solve_WhenPropagationSuffices_DoesNotLogGuessCount()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.ForcedBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.DoesNotContain(log, message => message.StartsWith("Solved in"));
    }

    [Fact]
    public void Solve_WhenGuessingFails_DoesNotLogGuessCount()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing);
        List<string> log = [];

        Assert.False(new Engine(log.Add).Solve(puzzle));

        Assert.DoesNotContain(log, message => message.StartsWith("Solved in"));
    }

    [Fact]
    public void Solve_IndentsRunPassAndFirstLevelGuessMessagesByLogIndent()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.GuessingBlock);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        Assert.Contains(log, message => message.StartsWith($"{Constants.LogIndent}Pass 1..."));
        Assert.Contains(log, message => message.StartsWith($"{Constants.LogIndent}Cell (1, 1): guessing "));
    }

    [Fact]
    public void Solve_WhenGuessesAreNested_IndentsEachLevelByOneMoreLogIndent()
    {
        // the 20th line of puzzles.txt needs nested guessing
        string puzzleText = PuzzleFile().Cast<string>().ElementAt(19);
        Puzzle puzzle = Puzzle.Parse(puzzleText);
        List<string> log = [];

        Assert.True(new Engine(log.Add).Solve(puzzle));

        string level1 = Constants.LogIndent;
        string level2 = level1 + Constants.LogIndent;
        string level3 = level2 + Constants.LogIndent;

        Assert.Contains(log, message => message.StartsWith($"{level1}Cell ") && message.Contains(": guessing "));
        Assert.Contains(log, message => message.StartsWith($"{level2}Cell ") && !message.StartsWith(level3) && message.Contains(": guessing "));
    }

    [Fact]
    public void Solve_WhenGuessFails_LogsRevert()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing);
        List<string> log = [];

        Assert.False(new Engine(log.Add).Solve(puzzle));

        Assert.Contains(log, message => message.Contains("reverting"));
    }

    [Fact]
    public void Solve_WhenEveryGuessFails_LogsOneRevertPerGuess()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing);
        List<string> log = [];

        Assert.False(new Engine(log.Add).Solve(puzzle));

        int guesses = log.Count(message => message.Contains(": guessing "));
        int reverts = log.Count(message => message.Contains("was bad, reverting"));
        Assert.True(guesses > 0);
        Assert.Equal(guesses, reverts);
    }

    #endregion
}
