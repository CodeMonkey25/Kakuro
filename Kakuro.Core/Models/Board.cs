using System.Text;

namespace Kakuro.Core.Models;

public class Board
{
    private static readonly int[] MinPossibleSum = [0, 1, 3, 6, 10, 15, 21, 28, 36, 45];
    private static readonly int[] MaxPossibleSum = [0, 9, 17, 24, 30, 35, 39, 42, 44, 45];

    private Puzzle Puzzle { get; }
    private Cell?[][] Cells { get; }
    private Cell[][] Runs { get; }
    
    private readonly int[] _numberBuffer = new int[9];
    private readonly bool[] _usedNumbersBuffer = new bool[10]; // indices 1..9 for digits
    private readonly HashSet<int>[] _excludedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
    private readonly IReadOnlyList<int>[] _currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => (IReadOnlyList<int>)Array.Empty<int>()).ToArray();
    
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
        // instantiate Runs
        // bind cells together
        Runs = new Cell[Puzzle.Runs.Length][];
        int i = 0;
        foreach (Run run in Puzzle.Runs)
        {
            Cell[] runCells = new Cell[run.Length];
            int j = 0;
            foreach ((int row, int column) in run.CellCoordinates)
            {
                Cells[row][column] ??= new Cell(row, column);
                runCells[j++] = Cells[row][column]!;
            }
            Runs[i++] = runCells;
            foreach (Cell cell in runCells)
            {
                cell.Bind(runCells);
            }
        }
    }
    
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

    public bool IsSolved()
    {
        foreach (Cell?[] row in Cells)
        {
            foreach (Cell? cell in row)
            {
                if (cell is null) continue;
                if (!cell.IsSolved) return false;
            }
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        for (int i = 0; i < Puzzle.Runs.Length; i++)
        {
            Run run = Puzzle.Runs[i];
            Cell[] cells = Runs[i];
            
            if (cells.Any(c => !c.IsSolved)) return false;
            if (cells.Sum(c => c.Value) != run.Total) return false;
            if (cells.DistinctBy(c => c.Value).Count() != cells.Length) return false;
        }

        return true;
    }
    
    public (string? error, bool changed) UpdateCellConstraints()
    {
        bool boardWasUpdated = false;

        for (int i = 0; i < Puzzle.Runs.Length; i++)
        {
            Run run = Puzzle.Runs[i];
            ReadOnlySpan<Cell> cells = Runs[i];
            Span<IReadOnlyList<int>> currentCandidates = _currentCandidatesBuffer.AsSpan(0, cells.Length);
            ReadOnlySpan<HashSet<int>> excludedCandidates = _excludedCandidatesBuffer.AsSpan(0, cells.Length);
            Span<int> numbers = _numberBuffer.AsSpan(0, cells.Length);
            Span<bool> usedNumbers = _usedNumbersBuffer.AsSpan();

            for (int j = 0; j < cells.Length; j++)
            {
                currentCandidates[j] = cells[j].Candidates;
                excludedCandidates[j].Clear();
                excludedCandidates[j].UnionWith(currentCandidates[j]);
            }
            usedNumbers.Clear();

            GetExcludedCandidates(0, run.Total, 0, currentCandidates, excludedCandidates, numbers, usedNumbers);

            for (int j = 0; j < cells.Length; j++)
            {
                Cell cell = cells[j];
                HashSet<int> excluded = excludedCandidates[j];

                (string? error, bool cellWasUpdated) = cell.RemoveCandidates(excluded);
                if (error != null) return (error, false);
                if (cellWasUpdated) boardWasUpdated = true;

                currentCandidates[j] = Array.Empty<int>();
            }
        }

        return (null, boardWasUpdated);
    }

    private void GetExcludedCandidates(int depth, int total, int runningTotal, Span<IReadOnlyList<int>> currentCandidates, ReadOnlySpan<HashSet<int>> excludedCandidates, Span<int> numbers, Span<bool> usedNumbers)
    {
        if (depth >= numbers.Length)
        {
            if (runningTotal == total)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    excludedCandidates[i].Remove(numbers[i]);
                }
            }
            return;
        }

        int remainingCellsAfterThis = numbers.Length - depth - 1;
        IReadOnlyList<int> currentCandidate = currentCandidates[depth];
        int currentCandidateCount = currentCandidate.Count;
        for (int i = 0; i < currentCandidateCount; i++)
        {
            int current = currentCandidate[i];
            if (usedNumbers[current]) continue;
            
            int newRunningTotal = runningTotal + current;
            if (newRunningTotal > total) continue;
            
            int remainingSumNeeded = total - newRunningTotal;
            if (remainingSumNeeded < MinPossibleSum[remainingCellsAfterThis]) continue;
            if (remainingSumNeeded > MaxPossibleSum[remainingCellsAfterThis]) continue;

            numbers[depth] = current;
            usedNumbers[current] = true;
            GetExcludedCandidates(depth + 1, total, newRunningTotal, currentCandidates, excludedCandidates, numbers, usedNumbers);
            numbers[depth] = 0;
            usedNumbers[current] = false;
        }
    }
}