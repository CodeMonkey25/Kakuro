using Kakuro.Core.Models;
using Kakuro.Core.Tests.TestData;

namespace Kakuro.Core.Tests.Models;

public class PuzzleTests
{
    [Fact]
    public void Parse_ValidString_SetsPropertiesAndRuns()
    {
        Puzzle puzzle = Puzzle.Parse(TestPuzzles.Sample);

        Assert.Equal("kakuro.com", puzzle.Source);
        Assert.Equal("1", puzzle.Number);
        Assert.Equal("Easy", puzzle.Difficulty);
        Assert.Equal(4, puzzle.Runs.Length);
        Assert.Equal(16, puzzle.Runs[0].Total);
        Assert.Equal(12, puzzle.Runs[1].Total);
        Assert.Equal(3, puzzle.Runs[2].Total);
        Assert.Equal(7, puzzle.Runs[3].Total);
    }

    [Fact]
    public void Parse_PreservesRunOrder()
    {
        Puzzle puzzle = Puzzle.Parse("s|n|d|1,1,0,0,0|2,1,0,1,1|3,1,1,0,0");

        Assert.Equal([1, 2, 3], puzzle.Runs.Select(r => r.Total));
    }

    [Fact]
    public void Parse_HeaderFieldsAreKeptAsText()
    {
        Puzzle puzzle = Puzzle.Parse("The Original Kakuro 2006-2007 Calendar|2006-10-04|easy|3,2,0,1,1");

        Assert.Equal("The Original Kakuro 2006-2007 Calendar", puzzle.Source);
        Assert.Equal("2006-10-04", puzzle.Number);
        Assert.Equal("easy", puzzle.Difficulty);
    }

    [Fact]
    public void Parse_WithNoRuns_ProducesEmptyPuzzleOfOneByOne()
    {
        Puzzle puzzle = Puzzle.Parse("source|1|Easy");

        Assert.Empty(puzzle.Runs);
        Assert.Equal(1, puzzle.Rows);
        Assert.Equal(1, puzzle.Columns);
    }

    [Theory]
    [InlineData("")]
    [InlineData("source")]
    [InlineData("source|1")]
    public void Parse_WithMissingHeaderFields_Throws(string text)
    {
        // currently surfaces as IndexOutOfRangeException; callers should only rely on it failing
        Assert.ThrowsAny<Exception>(() => Puzzle.Parse(text));
    }

    [Fact]
    public void Parse_WithTrailingSeparator_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Puzzle.Parse(TestPuzzles.Sample + "|"));
    }

    [Theory]
    [InlineData("s|1|e|3,2,1,0")]
    [InlineData("s|1|e|3,2,1,0,0,0")]
    [InlineData("s|1|e|a,2,1,0,0")]
    [InlineData("s|1|e|3,2,1,0,5")]
    [InlineData("s|1|e|3,2,1,0,0|bad")]
    [InlineData("s|1|e|0,2,1,0,0")] // total out of range
    [InlineData("s|1|e|46,2,1,0,0")]
    [InlineData("s|1|e|3,0,1,0,0")] // length out of range
    [InlineData("s|1|e|3,10,1,0,0")]
    [InlineData("s|1|e|3,2,-1,0,0")] // negative coordinates
    [InlineData("s|1|e|3,2,1,-1,0")]
    [InlineData("s|1|e|3,2,1,0,0|99,2,1,0,0")] // a later run is invalid
    public void Parse_WithMalformedRun_ThrowsFormatException(string text)
    {
        Assert.Throws<FormatException>(() => Puzzle.Parse(text));
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Puzzle.Parse(null!));
    }

    [Theory]
    [InlineData("s|1|e", 1, 1)]
    [InlineData("s|1|e|45,9,0,0,0", 1, 10)] // one wide horizontal run
    [InlineData("s|1|e|45,9,0,0,1", 10, 1)] // one tall vertical run
    [InlineData("s|1|e|4,2,2,0,0", 3, 3)] // only horizontal runs: columns come from the run extent
    [InlineData("s|1|e|4,2,0,2,1", 3, 3)] // only vertical runs: rows come from the run extent
    [InlineData("s|1|e|3,2,1,0,0|4,2,0,1,1", 3, 3)]
    [InlineData("s|1|e|3,2,0,1,1|4,2,4,0,0", 5, 3)] // a horizontal run on row 4 stretches the grid past the vertical run
    [InlineData(TestPuzzles.Sample, 3, 4)]
    public void Parse_CalculatesRowsAndColumnsDimensions(string text, int rows, int columns)
    {
        Puzzle puzzle = Puzzle.Parse(text);

        Assert.Equal(rows, puzzle.Rows);
        Assert.Equal(columns, puzzle.Columns);
    }

    [Theory]
    [InlineData(TestPuzzles.Sample)]
    [InlineData(TestPuzzles.Official41)]
    [InlineData(TestPuzzles.TwoSingleCells)]
    public void ToString_ReturnsFormattedSerializedPuzzle(string text)
    {
        Assert.Equal(text, Puzzle.Parse(text).ToString());
    }

    [Fact]
    public void Parse_RoundTripSerialization_PreservesIntegrity()
    {
        Puzzle initial = Puzzle.Parse(TestPuzzles.Sample);
        Puzzle parsed = Puzzle.Parse(initial.ToString());

        Assert.Equal(initial.Source, parsed.Source);
        Assert.Equal(initial.Number, parsed.Number);
        Assert.Equal(initial.Difficulty, parsed.Difficulty);
        Assert.Equal(initial.Rows, parsed.Rows);
        Assert.Equal(initial.Columns, parsed.Columns);
        Assert.Equal(initial.Runs.AsEnumerable(), parsed.Runs.AsEnumerable());
    }

    [Theory]
    [MemberData(nameof(PuzzleFile))]
    public void Parse_EveryPuzzleInTheDataset_RoundTrips(string text)
    {
        Assert.Equal(text, Puzzle.Parse(text).ToString());
    }

    public static TheoryData<string> PuzzleFile() => TestPuzzles.FromFile();

    [Fact]
    public void Parse_RoundTrip_WithNoRuns()
    {
        Puzzle initial = Puzzle.Parse("source|1|Easy");

        Puzzle parsed = Puzzle.Parse(initial.ToString());

        Assert.Empty(parsed.Runs);
        Assert.Equal("source|1|Easy", initial.ToString());
    }
}
