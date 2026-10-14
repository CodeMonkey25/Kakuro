using Kakuro.Core.Models;

namespace Kakuro.Core.Tests;

public class EngineTests
{
    [Fact]
    public void Solve_WhenSolutionIsValid_LogsBoardThenValidMessage()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,0,1,1");
        List<string> log = [];
        Engine engine = new(log.Add);

        Assert.True(engine.Solve(puzzle));

        int boardIndex = log.FindIndex(message => message.Contains("|1|"));
        int validIndex = log.IndexOf("Solution is valid");
        Assert.True(boardIndex >= 0, "board was not logged");
        Assert.True(validIndex > boardIndex, "validity message should follow the board");
        Assert.DoesNotContain("Unable to solve puzzle", log);
        Assert.DoesNotContain("Solution is invalid", log);
    }

    [Fact]
    public void Solve_WhenPuzzleIsUnsolvable_LogsFailureWithoutValidityMessage()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|17,2,0,1,1");
        List<string> log = [];
        Engine engine = new(log.Add);

        Assert.False(engine.Solve(puzzle));

        Assert.Contains("Unable to solve puzzle", log);
        Assert.DoesNotContain("Solution is valid", log);
        Assert.DoesNotContain("Solution is invalid", log);
    }

    [Fact]
    public void Solve_WithoutLogger_DoesNotThrow()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,0,1,1");

        Assert.True(new Engine().Solve(puzzle));
    }
}
