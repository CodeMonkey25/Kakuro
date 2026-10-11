namespace Kakuro.Core.Models;

public class Cell
{
    public int Row { get; }
    public int Column { get; }
    public int Value { get; private set; }
    public bool IsSolved { get; private set; }
    
    private HashSet<int> _candidates = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    public int[] Candidates => _candidates.Order().ToArray();
    public HashSet<Cell> LinkedCells { get; } = [];

    public Cell(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public string RemoveCandidate(int candidate)
    {
        if (IsSolved)
        {
            return Value == candidate ? $"Cell at row {Row}, column {Column} has no candidates.\n" : "";
        }
        
        _candidates.Remove(candidate);
        switch (_candidates.Count)
        {
            case 0:
                return $"Cell at row {Row}, column {Column} has no candidates.\n";
            case 1:
                return Solve(_candidates.First());
        }
        return "";
    }

    private string Solve(int value)
    {
        if (IsSolved)
        {
            return Value != value ? $"Cell at row {Row}, column {Column} is already solved.\n" : "";
        }
        
        IsSolved = true;
        Value = value;
        
        foreach (Cell linkedCell in LinkedCells)
        {
            string error = linkedCell.RemoveCandidate(Value);
            if (error != "") return error;
        }
        
        return "";
    }

    public string RemoveCandidates(IEnumerable<int> candidates)
    {
        string error = "";
        foreach (int c in candidates)
        {
            error = RemoveCandidate(c);
            if (error != "") break;
        }
        return error;
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