using System.Collections.Immutable;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class BoardRunTests
{
    private static Board.Run CreateRun(string rawRun, out PlayableCell[] cells)
    {
        Puzzle.Run puzzleRun = Puzzle.Run.Parse(rawRun);
        cells = puzzleRun.GetCellCoordinates()
            .Select(c => new PlayableCell(c.row, c.column))
            .ToArray();
        return new Board.Run(puzzleRun, cells.ToImmutableArray());
    }

    [Theory]
    [InlineData("16,2,0,1,0", 16, 2)]
    [InlineData("12,3,4,5,1", 12, 3)]
    [InlineData("45,9,0,0,0", 45, 9)]
    public void Constructor_ExposesTotalAndLengthFromPuzzleRun(string rawRun, int expectedTotal, int expectedLength)
    {
        Board.Run run = CreateRun(rawRun, out _);

        Assert.Equal(expectedTotal, run.Total);
        Assert.Equal(expectedLength, run.Length);
    }

    [Fact]
    public void Constructor_ExposesTheSuppliedCellsInOrder()
    {
        Board.Run run = CreateRun("16,3,2,4,0", out PlayableCell[] cells);

        Assert.Equal(3, run.Cells.Length);
        for (int i = 0; i < cells.Length; i++)
        {
            Assert.Same(cells[i], run.Cells[i]);
        }
    }

    [Fact]
    public void IsDirty_InitiallyFalse()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);

        Assert.False(run.IsDirty);
    }

    [Fact]
    public void MarkDirty_SetsIsDirty()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);

        run.MarkDirty();

        Assert.True(run.IsDirty);
    }

    [Fact]
    public void MarkDirty_CalledTwice_RemainsDirty()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);

        run.MarkDirty();
        run.MarkDirty();

        Assert.True(run.IsDirty);
    }

    [Fact]
    public void ClearDirty_ResetsIsDirty()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);
        run.MarkDirty();

        run.ClearDirty();

        Assert.False(run.IsDirty);
    }

    [Fact]
    public void ClearDirty_WhenNotDirty_RemainsClean()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);

        run.ClearDirty();

        Assert.False(run.IsDirty);
    }

    [Fact]
    public void MarkDirty_AfterClearDirty_BecomesDirtyAgain()
    {
        Board.Run run = CreateRun("16,2,0,1,0", out _);
        run.MarkDirty();
        run.ClearDirty();

        run.MarkDirty();

        Assert.True(run.IsDirty);
    }

    [Fact]
    public void DirtyFlag_IsIndependentPerRun()
    {
        Board.Run first = CreateRun("16,2,0,1,0", out _);
        Board.Run second = CreateRun("12,3,4,5,1", out _);

        first.MarkDirty();

        Assert.True(first.IsDirty);
        Assert.False(second.IsDirty);
    }
}
