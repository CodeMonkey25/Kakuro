namespace Kakuro.Core.Models;

public class Puzzle
{
    public string Source { get; }
    public string Number { get; }
    public string Difficulty { get; }
    public Run[] Runs { get; }
    
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    
    private Puzzle(string source, string number, string difficulty, Run[] runs)
    {
        Source = source;
        Number = number;
        Difficulty = difficulty;
        Runs = runs;
        
        Rows = Runs
            .Select(r => r.Orientation == Run.RunOrientation.Vertical ? r.Row + r.Length + 1 : r.Row + 1)
            .DefaultIfEmpty(1)
            .Max();
        Columns = Runs
            .Select(r => r.Orientation == Run.RunOrientation.Horizontal ? r.Column + r.Length + 1 : r.Column + 1)
            .DefaultIfEmpty(1)
            .Max();    }
    
    public static Puzzle Parse(string puzzle)
    {
        string[] parts = puzzle.Split('|');
        string source = parts[0];
        string number = parts[1];
        string difficulty = parts[2];
        Run[] runs = parts[3..].Select(Run.Parse).ToArray();
        return new Puzzle(source, number, difficulty, runs);
    }
    
    public override string ToString()
    {
        return $"{Source}|{Number}|{Difficulty}|{string.Join('|', Runs.Select(r => r.ToString()))}";
    }
}