using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class PuzzleRunTests
{
    [Theory]
    [InlineData("16,2,0,1,0", 16, 2, 0, 1, Puzzle.Run.RunOrientation.Horizontal)]
    [InlineData("12,3,4,5,1", 12, 3, 4, 5, Puzzle.Run.RunOrientation.Vertical)]
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
    [InlineData("16,2,0,1,0")]
    [InlineData("12,3,4,5,1")]
    public void ToString_ReturnsFormattedString(string rawRun)
    {
        Puzzle.Run run = Puzzle.Run.Parse(rawRun);

        Assert.Equal(rawRun, run.ToString());
    }

    [Fact]
    public void CellCoordinates_Horizontal_ReturnsSequentialColumnCoordinates()
    {
        Puzzle.Run run = Puzzle.Run.Parse("16,3,2,4,0");

        var coordinates = run.GetCellCoordinates();

        Assert.Equal(3, coordinates.Count());
        Assert.Equal([(2, 5), (2, 6), (2, 7)], coordinates);
    }

    [Fact]
    public void CellCoordinates_Vertical_ReturnsSequentialRowCoordinates()
    {
        Puzzle.Run run = Puzzle.Run.Parse("16,3,2,4,1");

        var coordinates = run.GetCellCoordinates();

        Assert.Equal(3, coordinates.Count());
        Assert.Equal([(3, 4), (4, 4), (5, 4)], coordinates);
    }
}
