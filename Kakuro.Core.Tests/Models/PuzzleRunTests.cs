using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PuzzleRunTests
{
    [Theory]
    [InlineData("16,2,0,1,0", 16, 2, 0, 1, Puzzle.Run.RunOrientation.Horizontal)]
    [InlineData("12,3,4,5,1", 12, 3, 4, 5, Puzzle.Run.RunOrientation.Vertical)]
    [InlineData("45,9,0,0,0", 45, 9, 0, 0, Puzzle.Run.RunOrientation.Horizontal)]
    public void Parse_ValidString_SetsPropertiesCorrectly(
        string rawRun,
        int expectedTotal,
        int expectedLength,
        int expectedRow,
        int expectedColumn,
        Puzzle.Run.RunOrientation expectedOrientation)
    {
        Puzzle.Run run = Puzzle.Run.Parse(rawRun);

        Assert.Equal(expectedTotal, run.Total);
        Assert.Equal(expectedLength, run.Length);
        Assert.Equal(expectedRow, run.Row);
        Assert.Equal(expectedColumn, run.Column);
        Assert.Equal(expectedOrientation, run.Orientation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("16")]
    [InlineData("16,2,0,1")]
    [InlineData("16,2,0,1,0,0")]
    public void Parse_WithWrongNumberOfParts_ThrowsFormatException(string rawRun)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));

        Assert.Contains("Expected 5", exception.Message);
    }

    [Theory]
    [InlineData("a,2,0,1,0")]
    [InlineData("16,b,0,1,0")]
    [InlineData("16,2,c,1,0")]
    [InlineData("16,2,0,d,0")]
    [InlineData("16,2,0,1,e")]
    [InlineData("16,2.5,0,1,0")]
    [InlineData("16,,0,1,0")]
    public void Parse_WithNonIntegerPart_ThrowsFormatException(string rawRun)
    {
        Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));
    }

    [Theory]
    [InlineData("16,2,0,1,2")]
    [InlineData("16,2,0,1,-1")]
    [InlineData("16,2,0,1,100")]
    public void Parse_WithUndefinedOrientation_ThrowsFormatException(string rawRun)
    {
        FormatException exception = Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));

        Assert.Contains("RunOrientation", exception.Message);
    }

    [Theory]
    [InlineData("0,2,0,1,0")]
    [InlineData("-1,2,0,1,0")]
    [InlineData("46,2,0,1,0")]
    public void Parse_WithTotalOutsideOneToFortyFive_ThrowsFormatException(string rawRun)
    {
        Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));
    }

    [Theory]
    [InlineData("1,1,0,0,0", 1, 1)]
    [InlineData("45,9,0,0,1", 45, 9)]
    public void Parse_AcceptsTotalAndLengthAtTheirLimits(string rawRun, int total, int length)
    {
        Puzzle.Run run = Puzzle.Run.Parse(rawRun);

        Assert.Equal(total, run.Total);
        Assert.Equal(length, run.Length);
    }

    [Theory]
    [InlineData("5,0,0,1,0")]
    [InlineData("5,-1,0,1,0")]
    [InlineData("5,10,0,1,0")]
    public void Parse_WithLengthOutsideOneToNine_ThrowsFormatException(string rawRun)
    {
        Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));
    }

    [Theory]
    [InlineData("5,2,-1,1,0")]
    [InlineData("5,2,0,-1,0")]
    public void Parse_WithNegativeCoordinates_ThrowsFormatException(string rawRun)
    {
        Assert.Throws<FormatException>(() => Puzzle.Run.Parse(rawRun));
    }

    [Fact]
    public void Parse_WithZeroCoordinates_IsAccepted()
    {
        Puzzle.Run run = Puzzle.Run.Parse("5,2,0,0,0");

        Assert.Equal((0, 0), (run.Row, run.Column));
    }

    [Fact]
    public void Parse_WithValueBeyondIntRange_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => Puzzle.Run.Parse("99999999999,2,0,1,0"));
    }

    [Theory]
    [InlineData("16,2,0,1,0")]
    [InlineData("12,3,4,5,1")]
    public void ToString_ReturnsFormattedString(string rawRun)
    {
        Puzzle.Run run = Puzzle.Run.Parse(rawRun);

        Assert.Equal(rawRun, run.ToString());
    }

    [Fact]
    public void Equality_FollowsValues()
    {
        Puzzle.Run a = Puzzle.Run.Parse("16,2,0,1,0");
        Puzzle.Run b = Puzzle.Run.Parse("16,2,0,1,0");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, Puzzle.Run.Parse("17,2,0,1,0"));
        Assert.NotEqual(a, Puzzle.Run.Parse("16,3,0,1,0"));
        Assert.NotEqual(a, Puzzle.Run.Parse("16,2,1,1,0"));
        Assert.NotEqual(a, Puzzle.Run.Parse("16,2,0,2,0"));
        Assert.NotEqual(a, Puzzle.Run.Parse("16,2,0,1,1"));
    }

    [Fact]
    public void CellCoordinates_Horizontal_ReturnsSequentialColumnCoordinates()
    {
        Puzzle.Run run = Puzzle.Run.Parse("16,3,2,4,0");

        Assert.Equal([(2, 5), (2, 6), (2, 7)], run.GetCellCoordinates());
    }

    [Fact]
    public void CellCoordinates_Vertical_ReturnsSequentialRowCoordinates()
    {
        Puzzle.Run run = Puzzle.Run.Parse("16,3,2,4,1");

        Assert.Equal([(3, 4), (4, 4), (5, 4)], run.GetCellCoordinates());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CellCoordinates_WithLengthOne_ReturnsTheCellAfterTheHint(int orientation)
    {
        Puzzle.Run run = Puzzle.Run.Parse($"5,1,3,3,{orientation}");

        (int row, int column) = Assert.Single(run.GetCellCoordinates());

        Assert.Equal(orientation == 0 ? (3, 4) : (4, 3), (row, column));
    }

    [Fact]
    public void CellCoordinates_NeverIncludeTheHintCell()
    {
        foreach (string raw in new[] { "9,3,2,4,0", "9,3,2,4,1" })
        {
            Puzzle.Run run = Puzzle.Run.Parse(raw);

            Assert.DoesNotContain((run.Row, run.Column), run.GetCellCoordinates());
        }
    }

    [Fact]
    public void CellCoordinates_CanBeEnumeratedRepeatedly()
    {
        Puzzle.Run run = Puzzle.Run.Parse("16,3,2,4,0");
        IEnumerable<(int row, int column)> coordinates = run.GetCellCoordinates();

        Assert.Equal(coordinates.ToArray(), coordinates.ToArray());
    }
}
