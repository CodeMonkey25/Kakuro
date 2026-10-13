using System.Collections.Immutable;
using System.Text;

namespace Kakuro.Core.Models;

public class Board
{
    private static readonly int[] MinPossibleSum = [0, 1, 3, 6, 10, 15, 21, 28, 36, 45];
    private static readonly int[] MaxPossibleSum = [0, 9, 17, 24, 30, 35, 39, 42, 44, 45];

    private Puzzle Puzzle { get; }
    private Cell[][] Cells { get; }
    private Run[] Runs { get; }
    
    // buffers for use with GetExcludedCandidates()
    private readonly int[] _numberBuffer = new int[9];
    private readonly bool[] _usedNumbersBuffer = new bool[10]; // indices 1..9 for digits
    private readonly HashSet<int>[] _excludedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
    private readonly IReadOnlyList<int>[] _currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => (IReadOnlyList<int>)Array.Empty<int>()).ToArray();
    
    public Board(Puzzle puzzle)
    {
        Puzzle = puzzle;
        
        // prepare Cells array
        Cells = new Cell[Puzzle.Rows][];
        for (int r = 0; r < Puzzle.Rows; r++)
        {
            Cells[r] = new Cell[Puzzle.Columns];
            for (int c = 0; c < Puzzle.Columns; c++)
            {
                Cells[r][c] = EmptyCell.Instance;
            }
        }
        
        // instantiate cells from puzzle runs
        // instantiate Board Runs
        // bind cells together
        Runs = new Run[Puzzle.Runs.Length];
        int i = 0;
        foreach (Puzzle.Run puzzleRun in Puzzle.Runs)
        {
            PlayableCell[] runCells = new PlayableCell[puzzleRun.Length];
            int j = 0;
            foreach ((int row, int column) in puzzleRun.GetCellCoordinates())
            {
                if (Cells[row][column] is not PlayableCell playableCell)
                {
                    playableCell = new PlayableCell(row, column);
                    Cells[row][column] = playableCell;
                }
                runCells[j++] = playableCell;
            }
            Runs[i++] = new Run(puzzleRun, runCells.ToImmutableArray());
            foreach (PlayableCell cell in runCells)
            {
                cell.Bind(runCells);
            }
        }
    }
    
    public override string ToString()
    {
        string rowDivider = new('-', Puzzle.Columns * 2 + 1);
        StringBuilder sb = new();
        foreach (Cell[] row in Cells)
        {
            sb.AppendLine(rowDivider);
            foreach (Cell cell in row)
            {
                sb.Append('|');
                sb.Append(cell);
            }
            sb.Append('|');
            sb.AppendLine();
        }
        sb.AppendLine(rowDivider);
        return sb.ToString();
    }

    public bool IsSolved()
    {
        foreach (Cell[] row in Cells)
        {
            foreach (Cell cell in row)
            {
                if (cell is PlayableCell playableCell && !playableCell.IsSolved)
                    return false;
            }
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        foreach (Run run in Runs)
        {
            if (run.Cells.Any(c => !c.IsSolved)) return false;
            if (run.Cells.Sum(c => c.Value) != run.Total) return false;
            if (run.Cells.DistinctBy(c => c.Value).Count() != run.Length) return false;
        }

        return true;
    }
    
    public (string? error, bool changed) UpdateCellConstraints()
    {
        bool boardWasUpdated = false;

        foreach (Run run in Runs)
        {
            Span<IReadOnlyList<int>> currentCandidates = _currentCandidatesBuffer.AsSpan(0, run.Length);
            ReadOnlySpan<HashSet<int>> excludedCandidates = _excludedCandidatesBuffer.AsSpan(0, run.Length);
            Span<int> numbers = _numberBuffer.AsSpan(0, run.Length);
            Span<bool> usedNumbers = _usedNumbersBuffer.AsSpan();

            for (int j = 0; j < run.Length; j++)
            {
                currentCandidates[j] = run.Cells[j].Candidates;
                excludedCandidates[j].Clear();
                excludedCandidates[j].UnionWith(currentCandidates[j]);
            }
            usedNumbers.Clear();

            GetExcludedCandidates(0, run.Total, 0, currentCandidates, excludedCandidates, numbers, usedNumbers);

            for (int j = 0; j < run.Length; j++)
            {
                PlayableCell cell = run.Cells[j];
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

    private readonly struct Run(Puzzle.Run puzzleRun, ImmutableArray<PlayableCell> cells)
    {
        public ImmutableArray<PlayableCell> Cells { get; } = cells;
        public int Length => puzzleRun.Length;
        public int Total => puzzleRun.Total;
    }
}