using System.Collections.Immutable;

namespace Kakuro.Core.Models;

public sealed class Puzzle
{
    public string Source { get; }
    public string Number { get; }
    public string Difficulty { get; }
    public ImmutableArray<Run> Runs { get; }
    
    public int Rows { get; }
    public int Columns { get; }
    
    private Puzzle(string source, string number, string difficulty, ImmutableArray<Run> runs)
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
            .Max();
    }
    
    public static Puzzle Parse(string puzzle)
    {
        string[] parts = puzzle.Split('|');
        string source = parts[0];
        string number = parts[1];
        string difficulty = parts[2];
        ImmutableArray<Run> runs = parts[3..].Select(Run.Parse).ToImmutableArray();
        return new Puzzle(source, number, difficulty, runs);
    }
    
    public override string ToString()
    {
        return $"{Source}|{Number}|{Difficulty}|{string.Join('|', Runs)}";
    }
    
    public readonly record struct Run
    {
        public int Total { get; }
        public int Length { get; }
    
        // the row and column are the coordinates of the hint cell for the run
        public int Row { get; } 
        public int Column { get; }
    
        public RunOrientation Orientation { get; }
    
        private Run(int total, int length, int row, int column, RunOrientation orientation)
        {
            Total = total;
            Length = length;
            Row = row;
            Column = column;
            Orientation = orientation;
        }
    
        public static Run Parse(string run)
        {
            string[] parts = run.Split(',');
            if (parts.Length != 5)
            {
                throw new FormatException($"Invalid run string format: '{run}'. Expected 5 comma-separated values.");
            }

            int total = int.Parse(parts[0]);
            int length = int.Parse(parts[1]);
            int row = int.Parse(parts[2]);
            int column = int.Parse(parts[3]);
            int orientationValue = int.Parse(parts[4]);

            if (!Enum.IsDefined(typeof(RunOrientation), orientationValue))
            {
                throw new FormatException($"Invalid RunOrientation value '{orientationValue}'.");
            }

            return new Run(total, length, row, column, (RunOrientation)orientationValue);
        }
    
        public override string ToString()
        {
            return $"{Total},{Length},{Row},{Column},{(int)Orientation}";
        }

        public IEnumerable<(int row, int column)> GetCellCoordinates()
        {
            switch (Orientation)
            {
                case RunOrientation.Horizontal:
                {
                    for (int c = Column + 1; c <= Column + Length; c++)
                        yield return (Row, c);
                    break;
                }
                case RunOrientation.Vertical:
                {
                    for (int r = Row + 1; r <= Row + Length; r++)
                        yield return (r, Column);
                    break;
                }
                default:
                    throw new InvalidOperationException("Invalid orientation");
            }
        }

        public enum RunOrientation
        {
            Horizontal,
            Vertical,
        }
    }
}