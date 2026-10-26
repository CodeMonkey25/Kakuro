using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests;

/// <summary>How the engine narrows candidates before and between guesses, observed through its log.</summary>
public class EngineConstraintTests
{
    #region Precomputed constraints

    [Fact]
    public void Solve_LogsPrecomputedConstraintsBeforeInitialConstraints()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        int precomputed = outcome.Log.FindIndex(message => message.Contains("Loading precomputed constraints"));
        int initial = outcome.Log.FindIndex(message => message.Contains("Calculating initial constraints"));
        Assert.True(precomputed >= 0);
        Assert.True(initial > precomputed);
    }

    [Fact]
    public void Solve_LogsEachRunFoundInTheTable()
    {
        // ForcedBlock rows have totals 3 and 17 over two cells (unique digit sets); columns 9 and 11 are not in the table
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        Assert.True(outcome.Solved);
        Assert.Equal(1, outcome.Count("Run (2, 3): candidates removed [3, 4, 5, 6, 7, 8, 9]..."));
        Assert.Equal(1, outcome.Count("Run (2, 17): candidates removed [1, 2, 3, 4, 5, 6, 7]..."));
        Assert.False(outcome.Logged("Run (2, 9): candidates removed"));
        Assert.False(outcome.Logged("Run (2, 11): candidates removed"));
    }

    [Fact]
    public void Solve_WhenNoRunIsInTheTable_LogsNoRemovedCandidates()
    {
        // total 9 over two cells has four digit sets, so nothing is precomputed
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.SingleRun(9, 2));

        Assert.True(outcome.Solved);
        Assert.True(outcome.Logged("Loading precomputed constraints"), "the table should still be consulted");
        Assert.False(outcome.Logged("candidates removed"));
    }

    [Fact]
    public void Solve_WhenPrecomputedConstraintsConflict_StopsBeforeCalculatingConstraints()
    {
        // row 3 limits (1,1) to {1,2} and column 17 limits it to {8,9}; the table alone empties the cell
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Contradictory);

        Assert.False(outcome.Solved);
        Assert.Equal(1, outcome.Count("Constraints conflict detected"));
        Assert.False(outcome.Logged("Calculating initial constraints"));
        Assert.False(outcome.Logged("guessing"));
    }

    #endregion

    #region Propagation (dirty-run tracking)

    [Theory]
    [InlineData(TestPuzzles.ForcedCorner)]
    [InlineData(TestPuzzles.ForcedBlock)]
    [InlineData(TestPuzzles.UniqueBlock)]
    [InlineData(TestPuzzles.TwoSingleCells)]
    public void Solve_WhenPropagationSuffices_NeverGuesses(string puzzleText)
    {
        // a run must be revisited after a neighboring run narrows one of its cells; if it isn't, the solver falls back to guessing
        EngineHarness.Outcome outcome = EngineHarness.Solve(puzzleText);

        Assert.True(outcome.Solved);
        Assert.False(outcome.Logged("guess"));
    }

    [Fact]
    public void Solve_WhenPropagationFindsConflict_LogsConflictAfterCalculatingAndDoesNotGuess()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.PropagationConflict);

        Assert.False(outcome.Solved);
        int initial = outcome.Log.FindIndex(message => message.Contains("Calculating initial constraints"));
        int conflict = outcome.Log.FindIndex(message => message.Contains("Constraints conflict detected"));
        Assert.True(initial >= 0, "the conflict should not be found by the precomputed constraints");
        Assert.True(conflict > initial);
        Assert.Equal(1, outcome.Count("Constraints conflict detected"));
        Assert.False(outcome.Logged("guessing"));
        Assert.Equal(EngineHarness.FailureMessage, outcome.Log[^1]);
    }

    [Fact]
    public void Solve_LogsAtLeastOnePropagationPass()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        Assert.True(outcome.Logged($"{Constants.LogIndent}Pass 1..."));
    }

    #endregion

    #region Guessing

    [Fact]
    public void Solve_WhenOnlyOneGuessIsNeeded_PropagationFinishesTheBoard()
    {
        // a guess marks the runs it touches as changed, so propagation completes the board without a second guess
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.GuessingBlock);

        Assert.True(outcome.Solved);
        Assert.Equal(1, outcome.Count(": guessing "));
    }

    [Fact]
    public void Solve_WhenGuessingFails_RevertsEveryGuessItMakes()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UnsolvableAfterGuessing);

        Assert.False(outcome.Solved);
        Assert.True(outcome.Count(": guessing ") > 0);
        Assert.Equal(outcome.Count(": guessing "), outcome.Count("was bad, reverting"));
        Assert.Contains("Unable to guess a solution", outcome.Log);
    }

    #endregion
}
