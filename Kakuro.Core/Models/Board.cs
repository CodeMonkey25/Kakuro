namespace Kakuro.Core.Models;

public class Board
{
    private Puzzle Puzzle { get; }
    private Cell?[][] Cells { get; }
    
    public Board(Puzzle puzzle)
    {
        Puzzle = puzzle;
        
        // prepare Cells array
        Cells = Enumerable.Range(0, Puzzle.Rows)
            .Select(row => Enumerable.Range(0, Puzzle.Columns)
                .Select(column => (Cell?)null)
                .ToArray()
            )
            .ToArray();
        
        // instantiate cells from puzzle runs
        foreach (Run run in Puzzle.Runs)
        {
            List<Cell> runCells = new();
            foreach (var (row, column) in run.GetIndices())
            {
                Cells[row][column] ??= new Cell(row, column);
                runCells.Add(Cells[row][column]!);
            }
            foreach (Cell cell in runCells)
            {
                cell.Bind(runCells);
            }
        }
    }
    
    public bool IsSolved() => Cells.All(row => row.All(cell => cell is null || cell.IsSolved));
}