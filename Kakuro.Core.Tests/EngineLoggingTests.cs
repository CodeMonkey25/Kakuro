using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests;

/// <summary>The messages <see cref="Engine"/> sends to its logger: their order, content and indentation.</summary>
public class EngineLoggingTests
{
    #region Lifecycle messages

    [Fact]
    public void Solve_WhenSolutionIsValid_LogsBoardThenValidMessage()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedCorner);

        Assert.True(outcome.Solved);
        int boardIndex = outcome.Log.IndexOf(TestPuzzles.Grid("XXX", "X12", "X3X"));
        int validIndex = outcome.Log.IndexOf(EngineHarness.ValidMessage);
        Assert.True(boardIndex >= 0, "the solved board was not logged");
        Assert.True(validIndex > boardIndex, "validity message should follow the board");
        Assert.Equal(EngineHarness.ValidMessage, outcome.Log[^1]);
        Assert.DoesNotContain(EngineHarness.FailureMessage, outcome.Log);
        Assert.DoesNotContain(EngineHarness.InvalidMessage, outcome.Log);
    }

    [Fact]
    public void Solve_LogsStartMessageThenInitialBoardFirst()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedCorner);

        Assert.Equal("Solving puzzle...", outcome.Log[0]);
        Assert.Equal(TestPuzzles.Grid("XXX", "X  ", "X X"), outcome.Log[1]);
    }

    [Fact]
    public void Solve_WhenPuzzleIsUnsolvable_LogsFailureWithoutValidityMessage()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Contradictory);

        Assert.False(outcome.Solved);
        Assert.Equal(EngineHarness.FailureMessage, outcome.Log[^1]);
        Assert.DoesNotContain(EngineHarness.ValidMessage, outcome.Log);
        Assert.DoesNotContain(EngineHarness.InvalidMessage, outcome.Log);
    }

    [Fact]
    public void Solve_WhenPuzzleIsUnsolvable_LogsOnlyTheInitialBoard()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Contradictory);

        List<string> boards = outcome.Log.Where(message => message.Contains('|') && message.Contains('-')).ToList();
        Assert.Single(boards);
        Assert.Equal(outcome.Log[1], boards[0]);
    }

    [Fact]
    public void Solve_WhenGuessingFails_LogsOnlyTheInitialBoard()
    {
        // the board is rewound on failure, so no partially filled board is ever reported as a result
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UnsolvableAfterGuessing);

        Assert.False(outcome.Solved);
        Assert.Equal(EngineHarness.FailureMessage, outcome.Log[^1]);
        Assert.Single(outcome.Log, message => message.Contains('|') && message.Contains('-'));
    }

    [Fact]
    public void Solve_ForwardsEngineDiagnosticsToTheLogger()
    {
        Assert.True(EngineHarness.Solve(TestPuzzles.Contradictory).Logged("Constraints conflict detected"));
        Assert.True(EngineHarness.Solve(TestPuzzles.GuessingBlock).Logged("guessing"));
    }

    [Fact]
    public void Solve_WithLoggerThatThrows_PropagatesTheException()
    {
        // the engine does not swallow logger failures
        Engine engine = new(_ => throw new InvalidOperationException("log failed"));

        Assert.Throws<InvalidOperationException>(() => engine.Solve(Puzzle.Parse(TestPuzzles.ForcedCorner)));
    }

    #endregion

    #region Progress messages

    [Fact]
    public void Solve_WhenPropagationSuffices_LogsOnlyPropagationProgress()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        Assert.True(outcome.Solved);
        Assert.NotEmpty(outcome.Log);
        Assert.False(outcome.Logged("guess"));
        Assert.False(outcome.Logged("conflict"));
    }

    [Fact]
    public void Solve_WhenGuessing_LogsEachGuessWithItsCell()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.GuessingBlock);

        Assert.True(outcome.Solved);
        Assert.True(outcome.Logged("Cell (1, 1): guessing "));
    }

    [Fact]
    public void Solve_WhenGuessing_LogsGuessCountOnSuccess()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.GuessingBlock);

        Assert.True(outcome.Solved);
        Assert.Contains(outcome.Log, message => message.StartsWith("Solved in 1 guesses, not including 0 bad guesses."));
    }

    [Fact]
    public void Solve_WhenGuessesWereReverted_CountsThemAsBadGuesses()
    {
        // line 20 of puzzles.txt needs nested guessing, so some guesses along the way must be discarded
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Lines()[19]);

        Assert.True(outcome.Solved);
        int badGuesses = outcome.Count("was bad, reverting");
        string summary = Assert.Single(outcome.Log, message => message.StartsWith("Solved in "));
        Assert.EndsWith($"not including {badGuesses} bad guesses.", summary);
    }

    [Fact]
    public void Solve_WhenPropagationSuffices_DoesNotLogGuessCount()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.ForcedBlock);

        Assert.True(outcome.Solved);
        Assert.False(outcome.Logged("Solved in"));
    }

    [Fact]
    public void Solve_WhenGuessingFails_DoesNotLogGuessCount()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UnsolvableAfterGuessing);

        Assert.False(outcome.Solved);
        Assert.False(outcome.Logged("Solved in"));
    }

    [Fact]
    public void Solve_WhenGuessFails_LogsRevert()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UnsolvableAfterGuessing);

        Assert.False(outcome.Solved);
        Assert.True(outcome.Logged("reverting"));
    }

    [Fact]
    public void Solve_WhenEveryGuessFails_LogsOneRevertPerGuess()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.UnsolvableAfterGuessing);

        int guesses = outcome.Count(": guessing ");
        Assert.False(outcome.Solved);
        Assert.True(guesses > 0);
        Assert.Equal(guesses, outcome.Count("was bad, reverting"));
    }

    #endregion

    #region Indentation

    [Fact]
    public void Solve_IndentsRunPassAndFirstLevelGuessMessagesByLogIndent()
    {
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.GuessingBlock);

        Assert.True(outcome.Solved);
        Assert.Contains(outcome.Log, message => message.StartsWith($"{Constants.LogIndent}Pass 1..."));
        Assert.Contains(outcome.Log, message => message.StartsWith($"{Constants.LogIndent}Cell (1, 1): guessing "));
    }

    [Fact]
    public void Solve_WhenGuessesAreNested_IndentsEachLevelByOneMoreLogIndent()
    {
        // line 20 of puzzles.txt needs nested guessing
        EngineHarness.Outcome outcome = EngineHarness.Solve(TestPuzzles.Lines()[19]);

        string level1 = Constants.LogIndent;
        string level2 = level1 + Constants.LogIndent;
        string level3 = level2 + Constants.LogIndent;

        Assert.True(outcome.Solved);
        Assert.Contains(outcome.Log, message => message.StartsWith($"{level1}Cell ") && message.Contains(": guessing "));
        Assert.Contains(outcome.Log, message => message.StartsWith($"{level2}Cell ") && !message.StartsWith(level3) && message.Contains(": guessing "));
    }

    #endregion
}
