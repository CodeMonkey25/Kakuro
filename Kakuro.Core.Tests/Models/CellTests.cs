using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class CellTests
{
    [Fact]
    public void EmptyCell_InheritsFromCell()
    {
        Cell cell = EmptyCell.Instance;

        Assert.IsAssignableFrom<Cell>(cell);
    }

    [Fact]
    public void PlayableCell_InheritsFromCell()
    {
        Cell cell = new PlayableCell(1, 2);

        Assert.IsAssignableFrom<Cell>(cell);
    }

    [Fact]
    public void PolymorphicCells_CanBeStoredInCellArray()
    {
        Cell[] cells = [EmptyCell.Instance, new PlayableCell(0, 0)];

        Assert.Equal(2, cells.Length);
        Assert.IsType<EmptyCell>(cells[0]);
        Assert.IsType<PlayableCell>(cells[1]);
    }
}
