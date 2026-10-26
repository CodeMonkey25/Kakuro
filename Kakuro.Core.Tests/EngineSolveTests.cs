using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests;

/// <summary>What <see cref="Engine.Solve"/> returns and the grid it arrives at. Constraint mechanics are in <see cref="EngineConstraintTests"/>; log output is in <see cref="EngineLoggingTests"/>.</summary>
public class EngineSolveTests
{
    #region Solutions

    [Fact]
    public void Solve_WhenRunForcesNakedSingle_SolvesCell()
    {
        // total 3 over 2 cells -> {1,2}; total 4 over 2 cells -> {1,3}; they intersect only at 1
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedCorner);

        Assert.True(outcome.Solved);
        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X3X"), outcome.SolvedBoard);
    }

    [Fact]
    public void Solve_SingleCellRuns_SolveToTheirTotals()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.TwoSingleCells);

        Assert.True(outcome.Solved);
        Assert.Equal(TestPuzzles.Grid("XXX", "XX9", "X9X"), outcome.SolvedBoard);
    }

    [Fact]
    public void Solve_WhenSolutionNeedsNineAndExcludesNineElsewhere_SolvesUniquely()
    {
        // rows: 3 -> {1,2}, 17 -> {8,9}; columns: 9 -> 1+8, 11 -> 2+9
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        Assert.True(outcome.Solved);
        Assert.Equal(TestPuzzles.Grid("XXX", "X12", "X89"), outcome.SolvedBoard);
    }

    [Fact]
    public void Solve_UniqueBlock_FindsTheOnlySolution()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UniqueBlock);

        Assert.True(outcome.Solved);
        Assert.Equal(TestPuzzles.Grid("XXX", "X97", "X89"), outcome.SolvedBoard);
    }

    [Fact]
    public void Solve_WhenPuzzleRequiresGuessing_FindsASolutionThatSatisfiesEveryRun()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.GuessingBlock);

        outcome.AssertSolvedGridSatisfiesPuzzle();
    }

    [Fact]
    public void Solve_OfficialPuzzle_SatisfiesEveryRun()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Official41);

        outcome.AssertSolvedGridSatisfiesPuzzle();
    }

    [Fact]
    public void Solve_WithNoRuns_ReturnsTrue()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy");

        Assert.True(new Engine().Solve(puzzle));
    }

    [Theory]
    [MemberData(nameof(PuzzleFile))]
    public void Solve_EveryPuzzleInTheDataset_SatisfiesEveryRun(string puzzleText)
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(puzzleText);

        Assert.Contains(EngineHarness.ValidMessage, outcome.Log);
        outcome.AssertSolvedGridSatisfiesPuzzle();
    }

    public static TheoryData<string> PuzzleFile() => TestPuzzles.FromFile();

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
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.SingleRun(total, length));

        outcome.AssertSolvedGridSatisfiesPuzzle();
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
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.SingleRun(45, 9));

        Assert.True(outcome.Solved);

        // any ordering is acceptable; what matters is that 1..9 each appear exactly once
        string row = outcome.SolvedBoard.Split(Environment.NewLine)[1];
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
    public void Solve_WhenConflictOnlyAppearsDuringPropagation_ReturnsFalse()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.PropagationConflict);

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

    [Fact]
    public void Solve_AfterAFailedSolve_TheSameEngineStillSolvesAnotherPuzzle()
    {
        Engine engine = new();

        Assert.False(engine.Solve(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing)));
        Assert.False(engine.Solve(Puzzle.Parse(TestPuzzles.PropagationConflict)));
        Assert.True(engine.Solve(Puzzle.Parse(TestPuzzles.GuessingBlock)));
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void Solve_WithoutLogger_DoesNotThrow()
    {
        Assert.True(new Engine().Solve(Puzzle.Parse(TestPuzzles.ForcedCorner)));
        Assert.True(new Engine().Solve(Puzzle.Parse(TestPuzzles.GuessingBlock)));
        Assert.False(new Engine().Solve(Puzzle.Parse(TestPuzzles.Contradictory)));
        Assert.False(new Engine().Solve(Puzzle.Parse(TestPuzzles.UnsolvableAfterGuessing)));
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
    public void Solve_OnTheSamePuzzle_IsDeterministic()
    {
        // GuessingBlock has several solutions; the engine must pick the same one every time
        string first = EngineHarness.Solve(TestPuzzles.GuessingBlock).SolvedBoard;
        string second = EngineHarness.Solve(TestPuzzles.GuessingBlock).SolvedBoard;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Solve_EngineInstancesAreIndependent()
    {
        // a fresh Board is built per Solve, so a result must not depend on what an engine solved before
        Engine used = new();
        used.Solve(Puzzle.Parse(TestPuzzles.Official41));
        List<string> usedLog = [];
        List<string> freshLog = [];

        bool usedSolved = new Engine(usedLog.Add).Solve(Puzzle.Parse(TestPuzzles.UniqueBlock));
        bool freshSolved = new Engine(freshLog.Add).Solve(Puzzle.Parse(TestPuzzles.UniqueBlock));

        Assert.Equal(freshSolved, usedSolved);
        Assert.Equal(freshLog, usedLog);
    }

    [Fact]
    public void Solve_DoesNotModifyThePuzzle()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Sample);
        Puzzle copy = Puzzle.Parse(TestPuzzles.Sample);

        new Engine().Solve(puzzle);

        Assert.Equal(copy, puzzle);
        Assert.Equal(copy.ToString(), puzzle.ToString());
    }

    #endregion
}
