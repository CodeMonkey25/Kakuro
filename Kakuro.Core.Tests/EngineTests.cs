using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests;

public class EngineTests
{
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
    public void Solve_WhenPuzzleIsUnsolvable_DoesNotLogABoard()
    {
        List<string> log = [];

        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.Contradictory));

        Assert.DoesNotContain(log, message => message.Contains('|') && message.Contains('-'));
    }

    [Fact]
    public void Solve_ForwardsTheBoardsDiagnosticsToTheLogger()
    {
        List<string> log = [];

        new Engine(log.Add).Solve(Puzzle.Parse(TestPuzzles.Contradictory));
        Assert.Contains(log, message => message.Contains("constraints conflict detected"));

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
}
