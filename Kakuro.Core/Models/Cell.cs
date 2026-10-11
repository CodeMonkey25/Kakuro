namespace Kakuro.Core.Models;

public class Cell
{
    public int Row { get; }
    public int Column { get; }
    public int Value { get; private set; }
    public bool IsSolved { get; private set; }
    
    private HashSet<int> Candidates { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    public HashSet<Cell> LinkedCells { get; } = [];

    public Cell(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public void RemoveCandidate(int candidate)
    {
        Candidates.Remove(candidate);
    }

    public void RemoveCandidates(IEnumerable<int> candidates)
    {
        foreach (int c in candidates)
        {
            Candidates.Remove(c);
        }
    }
    
    public void Bind(IEnumerable<Cell> linkedCells)
    {
        foreach (Cell linkedCell in linkedCells)
        {
            if (linkedCell == this) continue;
            LinkedCells.Add(linkedCell);
        }
    }
    
    public override string ToString()
    {
        return Value == 0 ? " " : Value.ToString();
    }
}