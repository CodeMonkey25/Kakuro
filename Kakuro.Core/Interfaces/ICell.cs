namespace Kakuro.Core.Interfaces;

public interface ICell
{
    public ICell.IState GetState();
    public void SetState(ICell.IState state);
    public bool IsValidState(ICell.IState state);

    public interface IState { }
}