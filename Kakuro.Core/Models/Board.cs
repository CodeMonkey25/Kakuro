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
    
    // buffers for use with GetExcludedCandidates() and UpdateCellConstraints()
    private readonly int[] _numberBuffer = new int[9];
    private readonly bool[] _usedNumbersBuffer = new bool[10]; // indices 1..9 for digits
    private readonly RunCellCandidates[] _cellCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new RunCellCandidates()).ToArray();
    
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
        sb.Append(rowDivider);
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
    
    public bool Solve(Action<string>? log = null)
    {
        State initialState = GetState();

        string? error = PropagateConstraints();
        if (error == null)
        {
            if (SolveByGuessing(log)) return true;
        }
        else
        {
            log?.Invoke($"constraints conflict detected: {error}");
        }

        // a failed search can leave the board half-propagated (possibly with empty candidate lists)
        SetState(initialState);
        return false;
    }

    // repeats UpdateCellConstraints() until nothing changes; returns null on success or the first error
    private string? PropagateConstraints()
    {
        bool changed;
        do
        {
            (string? error, changed) = UpdateCellConstraints();
            if (error != null) return error;
        } while (changed);
        return null;
    }

    private bool SolveByGuessing(Action<string>? log)
    {
        // identify unsolved cell with lowest number of candidates
        PlayableCell? cell = GetCellWithLowestCandidates();
        if (cell == null) return true; // no unsolved cells remain
        
        // load candidates for guessing
        Span<int> candidates = stackalloc int[cell.Candidates.Count];
        cell.CopyCandidatesTo(candidates);
        
        // try each candidate in turn
        State state = GetState();
        foreach (int candidate in candidates)
        {
            log?.Invoke($"Cell ({cell.Row}, {cell.Column}): guessing {candidate}");
            string? guessError = cell.Solve(candidate);
            if (guessError == null && PropagateConstraints() == null && SolveByGuessing(log)) return true;
            log?.Invoke($"Cell ({cell.Row}, {cell.Column}): guess {candidate} was bad, reverting");
            SetState(state);
        }
        
        return false;
    }

    private (string? error, bool changed) UpdateCellConstraints()
    {
        bool boardWasUpdated = false;

        foreach (Run run in Runs)
        {
            Span<RunCellCandidates> cellCandidates = _cellCandidatesBuffer.AsSpan(0, run.Length);
            Span<int> numbers = _numberBuffer.AsSpan(0, run.Length);
            Span<bool> usedNumbers = _usedNumbersBuffer.AsSpan();

            for (int i = 0; i < run.Length; i++)
            {
                cellCandidates[i].Load(run.Cells[i]);
            }
            usedNumbers.Clear();

            GetExcludedCandidates(0, run.Total, 0, cellCandidates, numbers, usedNumbers);

            for (int i = 0; i < run.Length; i++)
            {
                PlayableCell cell = run.Cells[i];

                (string? error, bool cellWasUpdated) = cell.RemoveCandidates(cellCandidates[i].Excluded);
                if (error != null) return (error, false);
                if (cellWasUpdated) boardWasUpdated = true;
            }
        }

        return (null, boardWasUpdated);
    }

    private void GetExcludedCandidates(int depth, int total, int runningTotal, Span<RunCellCandidates> cellCandidates, Span<int> numbers, Span<bool> usedNumbers)
    {
        if (depth >= numbers.Length)
        {
            if (runningTotal == total)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    cellCandidates[i].Excluded.Remove(numbers[i]);
                }
            }
            return;
        }

        int remainingCellsAfterThis = numbers.Length - depth - 1;
        foreach (int current in cellCandidates[depth].Candidates)
        {
            if (usedNumbers[current]) continue;
            
            int newRunningTotal = runningTotal + current;
            if (newRunningTotal > total) continue;
            
            int remainingSumNeeded = total - newRunningTotal;
            if (remainingSumNeeded < MinPossibleSum[remainingCellsAfterThis]) continue;
            if (remainingSumNeeded > MaxPossibleSum[remainingCellsAfterThis]) continue;

            numbers[depth] = current;
            usedNumbers[current] = true;
            GetExcludedCandidates(depth + 1, total, newRunningTotal, cellCandidates, numbers, usedNumbers);
            numbers[depth] = 0;
            usedNumbers[current] = false;
        }
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

    private sealed class RunCellCandidates
    {
        private int _candidateCount;
        private readonly int[] _candidates = new int[9];

        public HashSet<int> Excluded { get; } = new();
        public Span<int> Candidates => _candidates.AsSpan(0, _candidateCount);

        public void Load(PlayableCell cell)
        {
            _candidateCount = cell.Candidates.Count;

            cell.CopyCandidatesTo(Candidates);

            Excluded.Clear();
            foreach (int candidate in Candidates)
            {
                Excluded.Add(candidate);
            }
        }
    }
}
