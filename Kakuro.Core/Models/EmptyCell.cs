namespace Kakuro.Core.Models;

public sealed class EmptyCell : Cell
{
    public static EmptyCell Instance { get; } = new();

    public override string ToString() => "X";
    
    private EmptyCell() { }

    public override State GetState() => EmptyCellState.Instance;

    public override void SetState(State state)
    {
        if (state is EmptyCellState) return;
        throw new ArgumentException("Invalid state");
    }

    public override bool IsValidState(State state) => state is EmptyCellState;

    private sealed record EmptyCellState : Cell.State
    {
        public static EmptyCellState Instance { get; } = new();
    }
}