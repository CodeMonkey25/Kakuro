using System.Collections.Immutable;
using System.Text;

namespace Kakuro.Core.Models;

public class Board
{
    private static readonly int[] MinPossibleSum = [0, 1, 3, 6, 10, 15, 21, 28, 36, 45];
    private static readonly int[] MaxPossibleSum = [0, 9, 17, 24, 30, 35, 39, 42, 44, 45];

    private Puzzle Puzzle { get; }
    private Cell[] Cells { get; }
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
        Cells = new Cell[Puzzle.Rows * Puzzle.Columns];
        Array.Fill(Cells, EmptyCell.Instance);
        
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
                int index = row * Puzzle.Columns + column;
                if (Cells[index] is not PlayableCell playableCell)
                {
                    playableCell = new PlayableCell(row, column);
                    Cells[index] = playableCell;
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
        for (int r = 0; r < Puzzle.Rows; r++)
        {
            sb.AppendLine(rowDivider);
            for (int c = 0; c < Puzzle.Columns; c++)
            {
                sb.Append('|');
                sb.Append(Cells[r * Puzzle.Columns + c]);
            }
            sb.Append('|');
            sb.AppendLine();
        }
        sb.AppendLine(rowDivider);
        return sb.ToString();
    }

    public bool IsSolved()
    {
        foreach (Cell cell in Cells)
        {
            if (cell is PlayableCell playableCell && !playableCell.IsSolved)
                return false;
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        foreach (Run run in Runs)
        {
            int sum = 0;
            HashSet<int> seen = [];
            foreach (PlayableCell playableCell in run.Cells)
            {
                if (!playableCell.IsSolved) return false;
                if (!seen.Add(playableCell.Value)) return false;
                
                sum += playableCell.Value;
            }
            if (sum != run.Total) return false;
        }

        return true;
    }
    
    public State GetState() => new(Cells.Select(cell => cell.GetState()).ToImmutableArray());

    public void SetState(State state)
    {
        if (state is null || state.CellStates.IsDefault || state.CellStates.Length != Cells.Length)
            throw new ArgumentException("Invalid state");
        
        for (int i = 0; i < Cells.Length; i++)
        {
            if (!Cells[i].IsValidState(state.CellStates[i]))
                throw new ArgumentException("Invalid state");
        }

        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i].SetState(state.CellStates[i]);
        }
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
    
    public bool Solve()
    {
        State initialState = GetState();
        if (TrySolve()) return true;

        // a failed search can leave the board half-propagated (possibly with empty candidate lists)
        SetState(initialState);
        return false;
    }

    private bool TrySolve()
    {
        // update cell constraints
        string? error;
        bool changed;
        do
        {
            (error, changed) = UpdateCellConstraints();
            if (error != null) return false;
        } while (changed);

        PlayableCell? cell = GetCellWithLowestCandidates();
        if (cell == null) return true; // no unsolved cells remain

        State state = GetState();
        foreach (int candidate in cell.Candidates.ToArray())
        {
            error = cell.Solve(candidate);
            if (error == null)
            {
                bool solved = TrySolve();
                if (solved) return true;
            }
            SetState(state);
        }
        
        return false;
    }

    private PlayableCell? GetCellWithLowestCandidates()
    {
        PlayableCell? result = null;
        
        foreach (Cell cell in Cells)
        {
            if (cell is not PlayableCell playableCell) continue;
            if (playableCell.IsSolved) continue;
            if (playableCell.Candidates.Count == 2) return playableCell; // can't do better than that, bail early
            if (result == null || playableCell.Candidates.Count < result.Candidates.Count)
            {
                result = playableCell;
            }
        }

        return result;
    }

    private readonly struct Run(Puzzle.Run puzzleRun, ImmutableArray<PlayableCell> cells)
    {
        public ImmutableArray<PlayableCell> Cells { get; } = cells;
        public int Length => puzzleRun.Length;
        public int Total => puzzleRun.Total;
    }
    
    public sealed record State(ImmutableArray<Cell.State> CellStates)
    {
        public bool Equals(State? other) =>
            other is not null && (CellStates.IsDefault || other.CellStates.IsDefault
                ? CellStates.IsDefault && other.CellStates.IsDefault
                : CellStates.AsSpan().SequenceEqual(other.CellStates.AsSpan()));

        public override int GetHashCode()
        {
            HashCode hash = new();
            if (!CellStates.IsDefault)
                foreach (Cell.State s in CellStates) hash.Add(s);
            return hash.ToHashCode();
        }
    }
}