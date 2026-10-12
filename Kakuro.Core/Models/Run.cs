namespace Kakuro.Core.Models;

public class Run
{
    public int Index { get; }
    public int Total { get; }
    public int Length { get; }
    
    // the row and column are the coordinates of the hint cell for the run
    public int Row { get; } 
    public int Column { get; }
    public (int row, int column)[] CellCoordinates { get; }
    
    public RunOrientation Orientation { get; }
    
    private Run(int index, int total, int length, int row, int column, RunOrientation orientation)
    {
        Index = index;
        Total = total;
        Length = length;
        Row = row;
        Column = column;
        Orientation = orientation;
        CellCoordinates = Orientation switch
        {
            RunOrientation.Horizontal => Enumerable.Range(Column + 1, Length).Select(c => (Row, c)).ToArray(),
            RunOrientation.Vertical => Enumerable.Range(Row + 1, Length).Select(r => (r, Column)).ToArray(),
            _ => throw new InvalidOperationException("Invalid orientation"),
        };
    }
    
    public static Run Parse(int index, string run)
    {
        string[] parts = run.Split(',');
        int total = int.Parse(parts[0]);
        int length = int.Parse(parts[1]);
        int row = int.Parse(parts[2]);
        int column = int.Parse(parts[3]);
        RunOrientation orientation = (RunOrientation)int.Parse(parts[4]);
        return new Run(index, total, length, row, column, orientation);
    }
    
    public override string ToString()
    {
        return $"{Total},{Length},{Row},{Column},{(int)Orientation}";
    }

    public enum RunOrientation
    {
        Horizontal,
        Vertical,
    }
}