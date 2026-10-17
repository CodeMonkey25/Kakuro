namespace Kakuro.Core.Models;

public sealed class EmptyCell : ICell
{
    public static EmptyCell Instance { get; } = new();

    public override string ToString() => "X";
    
    private EmptyCell() { }

    public ICell.State GetState() => EmptyCellState.Instance;

    public void SetState(ICell.State state)
    {
        if (state is EmptyCellState) return;
        throw new ArgumentException("Invalid state");
    }

    public bool IsValidState(ICell.State state) => state is EmptyCellState;

    private sealed record EmptyCellState : ICell.State
    {
        public static EmptyCellState Instance { get; } = new();
    }
}