using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class RunTests
{
    [Theory]
    [InlineData("16,2,0,1,0", 16, 2, 0, 1, Run.eOrientation.Horizontal)]
    [InlineData("12,3,4,5,1", 12, 3, 4, 5, Run.eOrientation.Vertical)]
    public void Parse_ValidString_SetsPropertiesCorrectly(
        string rawRun,
        int expectedTotal,
        int expectedLength,
        int expectedRow,
        int expectedColumn,
        Run.eOrientation expectedOrientation)
    {
        Run run = Run.Parse(rawRun);

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
        Run run = Run.Parse(rawRun);

        Assert.Equal(rawRun, run.ToString());
    }

    [Fact]
    public void GetIndices_Horizontal_ReturnsSequentialColumnCoordinates()
    {
        Run run = Run.Parse("16,3,2,4,0");

        (int row, int column)[] indices = run.GetIndices();

        Assert.Equal(3, indices.Length);
        Assert.Equal([(2, 5), (2, 6), (2, 7)], indices);
    }

    [Fact]
    public void GetIndices_Vertical_ReturnsSequentialRowCoordinates()
    {
        Run run = Run.Parse("16,3,2,4,1");

        (int row, int column)[] indices = run.GetIndices();

        Assert.Equal(3, indices.Length);
        Assert.Equal([(3, 4), (4, 4), (5, 4)], indices);
    }

    [Fact]
    public void GetIndices_InvalidOrientation_ThrowsInvalidOperationException()
    {
        // Constructing a run string with an undefined orientation value
        Run run = Run.Parse("16,2,0,0,99");

        Assert.Throws<InvalidOperationException>(() => run.GetIndices());
    }
}
