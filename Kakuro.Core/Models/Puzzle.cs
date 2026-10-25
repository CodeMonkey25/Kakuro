using System.Collections.Immutable;

namespace Kakuro.Core.Models;

public sealed record Puzzle
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
        if (parts.Length < 3) throw new FormatException("Invalid puzzle format");

        string source = parts[0];
        string number = parts[1];
        string difficulty = parts[2];
        ImmutableArray<Run> runs = parts[3..].Select(Run.Parse).ToImmutableArray();

        return new Puzzle(source, number, difficulty, runs);
    }
    
    // ImmutableArray<T> compares by reference, so the synthesized record equality would not compare runs by value
    public bool Equals(Puzzle? other)
    {
        return other is not null
               && Source == other.Source
               && Number == other.Number
               && Difficulty == other.Difficulty
               && Runs.AsSpan().SequenceEqual(other.Runs.AsSpan());
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Source);
        hash.Add(Number);
        hash.Add(Difficulty);
        foreach (Run run in Runs)
        {
            hash.Add(run);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        string header = $"{Source}|{Number}|{Difficulty}";
        return Runs.IsEmpty ? header : $"{header}|{string.Join('|', Runs)}";
    }
    
    public sealed record Run
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
            if (total < Constants.MinRunTotal || total > Constants.MaxRunTotal)
            {
                throw new FormatException($"Invalid run total '{total}'. Total must be greater than or equal to {Constants.MinRunTotal} and less than or equal to {Constants.MaxRunTotal}.");
            }

            int length = int.Parse(parts[1]);
            if (length <= 0)
            {
                throw new FormatException($"Invalid run length '{length}'. Length must be greater than 0.");
            }

            if (length > Constants.MaxRunLength)
            {
                throw new FormatException($"Invalid run length '{length}'. Length must be less than or equal to {Constants.MaxRunLength}.");
            }

            int row = int.Parse(parts[2]);
            int column = int.Parse(parts[3]);
            if (row < 0 || column < 0)
            {
                throw new FormatException($"Invalid run coordinates '{row},{column}'. Row and column must be greater than or equal to 0.");
            }

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

        public IEnumerable<(int row, int column)> GetCellCoordinates() =>
            Orientation switch
            {
                RunOrientation.Horizontal => Enumerable.Range(Column + 1, Length).Select(c => (Row, c)),
                RunOrientation.Vertical => Enumerable.Range(Row + 1, Length).Select(r => (r, Column)),
                _ => throw new InvalidOperationException("Invalid orientation")
            };

        public enum RunOrientation
        {
            Horizontal,
            Vertical,
        }
    }
}