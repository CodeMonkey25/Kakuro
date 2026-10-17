namespace Kakuro.Core.Models;

public interface ICell
{
    public ICell.State GetState();
    public void SetState(ICell.State state);
    public bool IsValidState(ICell.State state);

    public abstract record State
    {
        private protected State() { }
    }
}