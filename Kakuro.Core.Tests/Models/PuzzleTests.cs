using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PuzzleTests
{
    private const string SamplePuzzleString = "kakuro.com|1|Easy|16,2,0,1,0|12,2,0,2,1|3,2,1,0,0|7,2,2,0,0";

    [Fact]
    public void Parse_ValidString_SetsPropertiesAndRuns()
    {
        Puzzle puzzle = Puzzle.Parse(SamplePuzzleString);

        Assert.Equal("kakuro.com", puzzle.Source);
        Assert.Equal("1", puzzle.Number);
        Assert.Equal("Easy", puzzle.Difficulty);
        Assert.Equal(4, puzzle.Runs.Length);
        Assert.Equal(16, puzzle.Runs[0].Total);
        Assert.Equal(12, puzzle.Runs[1].Total);
    }

    [Fact]
    public void Parse_CalculatesRowsAndColumnsDimensions()
    {
        Puzzle puzzle = Puzzle.Parse(SamplePuzzleString);

        Assert.Equal(3, puzzle.Rows);
        Assert.Equal(4, puzzle.Columns);
    }

    [Fact]
    public void ToString_ReturnsFormattedSerializedPuzzle()
    {
        Puzzle puzzle = Puzzle.Parse(SamplePuzzleString);

        Assert.Equal(SamplePuzzleString, puzzle.ToString());
    }

    [Fact]
    public void Parse_RoundTripSerialization_PreservesIntegrity()
    {
        Puzzle initial = Puzzle.Parse(SamplePuzzleString);
        Puzzle parsed = Puzzle.Parse(initial.ToString());

        Assert.Equal(initial.Source, parsed.Source);
        Assert.Equal(initial.Number, parsed.Number);
        Assert.Equal(initial.Difficulty, parsed.Difficulty);
        Assert.Equal(initial.Rows, parsed.Rows);
        Assert.Equal(initial.Columns, parsed.Columns);
        Assert.Equal(initial.Runs.Length, parsed.Runs.Length);

        for (int i = 0; i < initial.Runs.Length; i++)
        {
            Assert.Equal(initial.Runs[i].ToString(), parsed.Runs[i].ToString());
        }
    }
}
