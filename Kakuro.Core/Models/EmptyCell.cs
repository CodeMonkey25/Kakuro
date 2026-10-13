namespace Kakuro.Core.Models;

public sealed class EmptyCell : Cell
{
    public static EmptyCell Instance { get; } = new();

    public override string ToString() => "X";
    
    private EmptyCell() { }
}