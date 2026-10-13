using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class EmptyCellTests
{
    [Fact]
    public void Instance_ReturnsSameSingletonInstance()
    {
        EmptyCell first = EmptyCell.Instance;
        EmptyCell second = EmptyCell.Instance;

        Assert.Same(first, second);
    }

    [Fact]
    public void ToString_ReturnsX()
    {
        EmptyCell cell = EmptyCell.Instance;

        Assert.Equal("X", cell.ToString());
    }

    [Fact]
    public void IsInstanceOfCell()
    {
        EmptyCell cell = EmptyCell.Instance;

        Assert.IsAssignableFrom<Cell>(cell);
    }
}
