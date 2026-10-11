using System.Text;

namespace Kakuro.Core.Models;

public class Board
{
    private Puzzle Puzzle { get; }
    private Cell?[][] Cells { get; }
    
    public Board(Puzzle puzzle)
    {
        Puzzle = puzzle;
        
        // prepare Cells array
        Cells = new Cell?[Puzzle.Rows][];
        for (int r = 0; r < Puzzle.Rows; r++)
        {
            Cells[r] = new Cell?[Puzzle.Columns];
        }
        
        // instantiate cells from puzzle runs
        foreach (Run run in Puzzle.Runs)
        {
            List<Cell> runCells = [];
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

    public override string ToString()
    {
        StringBuilder sb = new();
        foreach (Cell?[] row in Cells)
        {
            foreach (Cell? cell in row)
            {
                sb.Append(cell?.ToString() ?? "X");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}