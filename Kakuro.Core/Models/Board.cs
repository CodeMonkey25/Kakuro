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

    public (string error, bool changed) UpdateCellConstraints()
    {
        string error = "";
        bool changed = false;
        foreach (Run run in Puzzle.Runs)
        {
            Cell[] cells = GetCells(run).ToArray();
            int[][] currentCandidates = cells.Select(c => c.Candidates).ToArray();
            HashSet<int>[] updatedCandidates = cells.Select(_ => new HashSet<int>()).ToArray();
            Span<int> numbers = new int[cells.Length];
            
            GetUpdatedCandidates(run.Length, 0, run.Total, 0, currentCandidates, updatedCandidates, numbers);
            
            for (int i = 0; i < cells.Length; i++)
            {
                Cell cell = cells[i];
                int[] current = currentCandidates[i];
                HashSet<int> updated = updatedCandidates[i];
                (error, bool wasUpdated) = cell.RemoveCandidates(current.Except(updated));
                if (error != "") return (error, false);
                if (wasUpdated) changed = true;
            }
        }
        return (error, changed);
    }

    private void GetUpdatedCandidates(int length, int depth, int total, int runningTotal, int[][] currentCandidates, HashSet<int>[] updatedCandidates, Span<int> numbers)
    {
        if (depth >= length)
        {
            if (runningTotal == total)
            {
                for (int i = 0; i < length; i++)
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
            GetUpdatedCandidates(length, depth + 1, total, runningTotal + current, currentCandidates, updatedCandidates, numbers);
            numbers[depth] = 0;
        }
    }
    
    private IEnumerable<Cell> GetCells(Run run)
    {
        foreach (var (row, column) in run.GetIndices())
        {
            if (Cells[row][column] is null)
            {
                throw new InvalidOperationException($"Cell at row {row}, column {column} is null.");
            }
            yield return Cells[row][column]!;
        }
    }
}