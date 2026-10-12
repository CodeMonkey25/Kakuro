namespace Kakuro.Core.Models;

public class Run
{
    public int Total { get; }
    public int Length { get; }
    
    // the row and column are the coordinates of the hint cell for the run
    public int Row { get; } 
    public int Column { get; }
    
    public eOrientation Orientation { get; }
    
    private Run(int total, int length, int row, int column, eOrientation orientation)
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
        int total = int.Parse(parts[0]);
        int length = int.Parse(parts[1]);
        int row = int.Parse(parts[2]);
        int column = int.Parse(parts[3]);
        eOrientation orientation = (eOrientation)int.Parse(parts[4]);
        return new Run(total, length, row, column, orientation);
    }
    
    public override string ToString()
    {
        return $"{Total},{Length},{Row},{Column},{(int)Orientation}";
    }

    public (int row, int column)[] GetIndices()
    {
        return Orientation switch
        {
            eOrientation.Horizontal => Enumerable.Range(Column + 1, Length).Select(c => (Row, c)).ToArray(),
            eOrientation.Vertical => Enumerable.Range(Row + 1, Length).Select(r => (r, Column)).ToArray(),
            _ => throw new InvalidOperationException("Invalid orientation"),
        };
    }

    public enum eOrientation
    {
        Horizontal,
        Vertical,
    }
}