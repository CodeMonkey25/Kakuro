using Kakuro.Core.Interfaces;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Interfaces;

/// <summary>
/// The state contract shared by every <see cref="ICell"/> subtype, exercised polymorphically.
/// </summary>
public class ICellTests
{
    public static TheoryData<string> CellKinds() => ["empty", "playable"];

    private static ICell CreateCell(string kind) => kind == "empty" ? EmptyCell.Instance : new PlayableCell(0, 0);

    private static ICell CreateOtherKind(string kind) => kind == "empty" ? new PlayableCell(0, 0) : EmptyCell.Instance;

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void GetState_IsValidForTheCellItCameFrom(string kind)
    {
        ICell cell = CreateCell(kind);

        Assert.True(cell.IsValidState(cell.GetState()));
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void SetState_WithOwnState_DoesNotThrowOrChangeState(string kind)
    {
        ICell cell = CreateCell(kind);
        ICell.IState before = cell.GetState();

        cell.SetState(before);

        Assert.Equal(before, cell.GetState());
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void IsValidState_RejectsNull(string kind)
    {
        Assert.False(CreateCell(kind).IsValidState(null!));
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void SetState_RejectsNull(string kind)
    {
        Assert.Throws<ArgumentException>(() => CreateCell(kind).SetState(null!));
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void IsValidState_RejectsStateOfTheOtherKind(string kind)
    {
        ICell cell = CreateCell(kind);
        ICell.IState foreign = CreateOtherKind(kind).GetState();

        Assert.False(cell.IsValidState(foreign));
        Assert.Throws<ArgumentException>(() => cell.SetState(foreign));
    }

    [Fact]
    public void PolymorphicCells_CanBeStoredInCellArray()
    {
        ICell[] cells = [EmptyCell.Instance, new PlayableCell(0, 0)];

        Assert.IsType<EmptyCell>(cells[0]);
        Assert.IsType<PlayableCell>(cells[1]);
    }
}
