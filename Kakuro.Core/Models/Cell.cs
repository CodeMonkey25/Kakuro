namespace Kakuro.Core.Models;

public abstract class Cell
{
    public abstract Cell.State GetState();
    public abstract void SetState(Cell.State state);
    public abstract bool IsValidState(Cell.State state);

    public abstract record State
    {
        private protected State() { }
    }
}