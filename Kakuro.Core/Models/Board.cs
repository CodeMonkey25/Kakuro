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
        string rowDivider = new('-', Puzzle.Columns * 2 + 1);
        StringBuilder sb = new();
        foreach (Cell?[] row in Cells)
        {
            sb.AppendLine(rowDivider);
            foreach (Cell? cell in row)
            {
                sb.Append('|');
                sb.Append(cell?.ToString() ?? "X");
            }
            sb.Append('|');
            sb.AppendLine();
        }
        sb.AppendLine(rowDivider);
        return sb.ToString();
    }

    public (string error, bool changed) UpdateCellConstraints(Span<int> numberBuffer, Span<HashSet<int>> updatedCandidatesBuffer, Span<int[]> currentCandidatesBuffer)
    {
        string error = "";
        bool boardWasUpdated = false;
        
        foreach (Run run in Puzzle.Runs)
        {
            ReadOnlySpan<Cell> cells = GetCells(run);
            Span<int[]> currentCandidates = currentCandidatesBuffer.Slice(0, cells.Length);
            Span<HashSet<int>> updatedCandidates = updatedCandidatesBuffer.Slice(0, cells.Length);
            Span<int> numbers = numberBuffer.Slice(0, cells.Length);

            for (int i = 0; i < cells.Length; i++)
            {
                currentCandidates[i] = cells[i].Candidates;
            }
            
            GetUpdatedCandidates(0, run.Total, 0, currentCandidates, updatedCandidates, numbers);
            
            for (int i = 0; i < cells.Length; i++)
            {
                Cell cell = cells[i];
                int[] current = currentCandidates[i];
                HashSet<int> updated = updatedCandidates[i];
                
                (error, bool cellWasUpdated) = cell.RemoveCandidates(current.Except(updated));
                if (error != "") return (error, false);
                if (cellWasUpdated) boardWasUpdated = true;
                
                currentCandidates[i] = [];
                updatedCandidates[i].Clear();
            }
        }
        return (error, boardWasUpdated);
    }

    private void GetUpdatedCandidates(int depth, int total, int runningTotal, Span<int[]> currentCandidates, Span<HashSet<int>> updatedCandidates, Span<int> numbers)
    {
        if (depth >= numbers.Length)
        {
            if (runningTotal == total)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    updatedCandidates[i].Add(numbers[i]);
                }
            }
            return;
        }
        
        for (int i = 0; i < currentCandidates[depth].Length; i++)
        {
            int current = currentCandidates[depth][i];
            if (numbers[..depth].Contains(current)) continue;
            if (runningTotal + current > total) continue;

            numbers[depth] = current;
            GetUpdatedCandidates(depth + 1, total, runningTotal + current, currentCandidates, updatedCandidates, numbers);
            numbers[depth] = 0;
        }
    }
    
    private ReadOnlySpan<Cell> GetCells(Run run)
    {
        if (run.Orientation == Run.eOrientation.Horizontal)
        {
            return Cells[run.Row].AsSpan(run.Column + 1, run.Length)!;
        }
        
        // must be vertical...
        Cell[] cells = new Cell[run.Length];
        int index = 0;
        foreach ((int row, int column) in run.GetIndices())
        {
            cells[index++] = Cells[row][column]!;
        }
        return cells;
    }
}