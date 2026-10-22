using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Kakuro.Core.Interfaces;

namespace Kakuro.Core.Models;

public sealed class Board
{
    private readonly Puzzle _puzzle;
    private readonly ICell[] _cells;
    private readonly PlayableCell[] _playableCells;
    private readonly Run[] _runs;
    
    // reusable scratch state for UpdateCellConstraints()
    private readonly RunSolveState _runSolveState = new();
    
    public Board(Puzzle puzzle)
    {
        _puzzle = puzzle;
        
        // prepare Cells array
        _cells = new ICell[_puzzle.Rows * _puzzle.Columns];
        Array.Fill(_cells, EmptyCell.Instance);
        
        List<Run> runs = new(_puzzle.Runs.Length);
        foreach (Puzzle.Run puzzleRun in _puzzle.Runs)
        {
            // instantiate playable cells from puzzle runs
            List<PlayableCell> runCells = new(puzzleRun.Length);
            foreach ((int row, int column) in puzzleRun.GetCellCoordinates())
            {
                int cellsIndex = row * _puzzle.Columns + column;
                if (_cells[cellsIndex] is not PlayableCell playableCell)
                {
                    playableCell = new PlayableCell(row, column);
                    _cells[cellsIndex] = playableCell;
                }
                runCells.Add(playableCell);
            }
            
            // instantiate Board Runs
            Run run = new(puzzleRun, runCells.ToImmutableArray());
            runs.Add(run);
            
            // bind run cells together
            foreach (PlayableCell cell in runCells)
            {
                cell.BindCells(runCells);
                cell.CandidatesChanged += run.MarkDirty;
            }
        }
        
        _runs = runs.ToArray();
        _playableCells = _cells.OfType<PlayableCell>().ToArray();
    }
    
    public override string ToString()
    {
        string rowDivider = new('-', _puzzle.Columns * 2 + 1);
        StringBuilder sb = new();
        for (int r = 0; r < _puzzle.Rows; r++)
        {
            sb.AppendLine(rowDivider);
            for (int c = 0; c < _puzzle.Columns; c++)
            {
                sb.Append('|');
                sb.Append(_cells[r * _puzzle.Columns + c]);
            }
            sb.Append('|');
            sb.AppendLine();
        }
        sb.Append(rowDivider);
        return sb.ToString();
    }

    public bool IsSolved()
    {
        foreach (PlayableCell playableCell in _playableCells)
        {
            if (!playableCell.IsSolved) return false;
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        HashSet<int> seen = [];
        foreach (Run run in _runs)
        {
            seen.Clear();
            int sum = 0;
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
    
    public State GetState()
    {
        ICell.IState[] states = new ICell.IState[_playableCells.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = _playableCells[i].GetState();
        }
        return new State(ImmutableCollectionsMarshal.AsImmutableArray(states));
    }

    public void SetState(State state)
    {
        if (state is null || state.CellStates.IsDefault || state.CellStates.Length != _playableCells.Length)
            throw new ArgumentException("Invalid state");
        
        for (int i = 0; i < _playableCells.Length; i++)
        {
            if (!_playableCells[i].IsValidState(state.CellStates[i]))
                throw new ArgumentException("Invalid state");
        }

        for (int i = 0; i < _playableCells.Length; i++)
        {
            _playableCells[i].SetState(state.CellStates[i]);
        }
    }
    
    public bool Solve(Action<string>? log = null)
    {
        State initialState = GetState();
        
        log?.Invoke("Loading precomputed constraints...");
        string? error = LoadPrecomputedRunConstraints(log);
        if (error != null)
        {
            log?.Invoke($"Constraints conflict detected: {error}");
            SetState(initialState);
            return false;
        }

        // mark all runs as dirty
        foreach (Run run in _runs)
        {
            run.MarkDirty();
        }
        
        log?.Invoke("Calculating initial constraints...");
        error = CalculateRunConstraints(log);
        if (error != null)
        {
            log?.Invoke($"Constraints conflict detected: {error}");
            SetState(initialState);
            return false;
        }

        if (IsSolved()) return true;
        
        log?.Invoke("Solving by guessing...");
        int guesses = 0;
        int badGuesses = 0;
        bool solved = SolveByGuessing(log, ref guesses, ref badGuesses);
        if (!solved)
        {
            log?.Invoke("Unable to guess a solution");
            SetState(initialState);
            return false;
        }
        
        log?.Invoke($"Solved in {guesses} guesses, not including {badGuesses} bad guesses.");
        return true;
    }

    private string? LoadPrecomputedRunConstraints(Action<string>? log)
    {
        foreach (Run run in _runs)
        {
            uint exclusionMask = RunCandidatesTable.GetRunExclusionMask(run.Length, run.Total);
            if (exclusionMask == 0) continue;
            
            ImmutableArray<int> excludedDigits = RunCandidatesTable.GetRunExclusions(run.Length, run.Total);
            log?.Invoke($"{Constants.LogIndent}Run ({run.Length}, {run.Total}): candidates removed [{string.Join(", ", excludedDigits)}]...");
            
            foreach (PlayableCell cell in run.Cells)
            {
                (string? error, _) = cell.RemoveCandidates(exclusionMask);
                if (error != null) return error;
            }
        }
        return null;
    }

    // repeats UpdateCellConstraints() until nothing changes; returns null on success or the first error
    private string? CalculateRunConstraints(Action<string>? log = null)
    {
        int pass = 0;
        bool changed;
        do
        {
            log?.Invoke($"{Constants.LogIndent}Pass {++pass}...");
            (string? error, changed) = UpdateCellConstraints();
            if (error != null) return error;
        } while (changed);
        return null;
    }

    private (string? error, bool changed) UpdateCellConstraints()
    {
        bool boardWasUpdated = false;

        foreach (Run run in _runs)
        {
            if (!run.IsDirty) continue;
            if (run.IsSolved)
            {
                run.ClearDirty();
                continue;
            }
            
            _runSolveState.Load(run);
            GetExcludedCandidates(0, 0, _runSolveState);
            (string? error, bool runWasUpdated) = _runSolveState.WriteExclusionsToRun();
            if (error != null) return (error, false);
            if (runWasUpdated) boardWasUpdated = true;
            
            run.ClearDirty();
        }

        return (null, boardWasUpdated);
    }

    private static bool GetExcludedCandidates(int depth, int runningTotal, RunSolveState runSolveState)
    {
        if (depth >= runSolveState.Length)
        {
            bool done = false;
            if (runningTotal == runSolveState.Total)
            {
                done = runSolveState.KeepCurrentNumbers();
            }
            return done;
        }

        int remainingCellsAfterThis = runSolveState.Length - depth - 1;
        RunCellCandidates runCellCandidates = runSolveState.GetRunCellCandidates(depth);
        foreach (int current in runCellCandidates.Candidates)
        {
            if (runSolveState.IsNumberUsed(current)) continue;

            int newRunningTotal = runningTotal + current;
            if (newRunningTotal > runSolveState.Total) return false;
            
            int remainingSumNeeded = runSolveState.Total - newRunningTotal;
            if (remainingSumNeeded < Constants.MinPossibleSum[remainingCellsAfterThis]) continue;
            if (remainingSumNeeded > Constants.MaxPossibleSum[remainingCellsAfterThis]) continue;

            runSolveState.UseNumber(depth, current);
            bool done = GetExcludedCandidates(depth + 1, newRunningTotal, runSolveState);
            runSolveState.ClearNumber(depth);
            if (done) return true; // every candidate has been confirmed viable, nothing left to exclude
        }
        return false;
    }

    private bool SolveByGuessing(Action<string>? log, ref int guesses, ref int badGuesses)
    {
        string indent = string.Concat(Enumerable.Repeat(Constants.LogIndent, guesses + 1));
        
        // identify unsolved cell with lowest number of candidates
        PlayableCell? cell = GetCellWithLowestCandidates();
        if (cell == null) return true; // no unsolved cells remain
        
        // load candidates for guessing
        Span<int> candidates = stackalloc int[cell.CandidateCount];
        cell.CopyCandidatesTo(candidates);
        
        // try each candidate in turn
        State state = GetState();
        foreach (int candidate in candidates)
        {
            log?.Invoke($"{indent}Cell ({cell.Row}, {cell.Column}): guessing {candidate}...");
            string? guessError = cell.Solve(candidate);
            guesses++;
            if (guessError == null && CalculateRunConstraints() == null && SolveByGuessing(log, ref guesses, ref badGuesses)) return true;
            log?.Invoke($"{indent}Cell ({cell.Row}, {cell.Column}): guess {candidate} was bad, reverting...");
            SetState(state);
            guesses--;
            badGuesses++;
        }
        
        return false;
    }

    private PlayableCell? GetCellWithLowestCandidates()
    {
        PlayableCell? result = null;
        
        foreach (PlayableCell playableCell in _playableCells)
        {
            if (playableCell.IsSolved) continue;
            if (playableCell.CandidateCount == 2) return playableCell; // can't do better than that, bail early
            if (result == null || playableCell.CandidateCount < result.CandidateCount)
            {
                result = playableCell;
            }
        }

        return result;
    }

    public sealed class Run
    {
        private Puzzle.Run PuzzleRun { get; }
        public ImmutableArray<PlayableCell> Cells { get; }
        public int Length => PuzzleRun.Length;
        public int Total => PuzzleRun.Total;
        public bool IsDirty { get; private set; }
        public bool IsSolved
        {
            get
            {
                int total = 0;
                foreach (PlayableCell cell in Cells)
                {
                    if (!cell.IsSolved) return false;
                    total += cell.Value;
                }
                return total == Total;
            }
        }

        public Run(Puzzle.Run puzzleRun, ImmutableArray<PlayableCell> cells)
        {
            PuzzleRun = puzzleRun;
            Cells = cells;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
        
        public void ClearDirty()
        {
            IsDirty = false;
        }
    }
    
    public sealed record State(ImmutableArray<ICell.IState> CellStates)
    {
        public bool Equals(State? other) =>
            other is not null && (CellStates.IsDefault || other.CellStates.IsDefault
                ? CellStates.IsDefault && other.CellStates.IsDefault
                : CellStates.AsSpan().SequenceEqual(other.CellStates.AsSpan()));

        public override int GetHashCode()
        {
            HashCode hash = new();
            if (!CellStates.IsDefault)
                foreach (ICell.IState s in CellStates) hash.Add(s);
            return hash.ToHashCode();
        }
    }

    private sealed class RunSolveState
    {
        private readonly RunCellCandidates[] _cellCandidatesBuffer = Enumerable.Range(0, Constants.MaxRunLength).Select(_ => new RunCellCandidates()).ToArray();
        private readonly int[] _numbers = new int[Constants.MaxRunLength];
        private uint _usedNumbersMask;
        private Run _run = null!;

        public int Length => _run.Length;
        public int Total => _run.Total;
        public RunCellCandidates GetRunCellCandidates(int index) => _cellCandidatesBuffer[index];

        public void Load(Run run)
        {
            _run = run;
            _usedNumbersMask = 0;
            Array.Clear(_numbers);
            for (int i = 0; i < Length; i++)
            {
                _cellCandidatesBuffer[i].Load(run.Cells[i]);
            }
        }

        public bool KeepCurrentNumbers()
        {
            uint remaining = 0;
            for (int i = 0; i < Length; i++)
            {
                _cellCandidatesBuffer[i].RemoveExclusion(_numbers[i]);
                remaining |= _cellCandidatesBuffer[i].GetExclusionsMask();
            }
            return remaining == 0;
        }

        public void UseNumber(int depth, int current)
        {
            _usedNumbersMask |= 1u << current;
            _numbers[depth] = current;
        }

        public void ClearNumber(int depth)
        {
            _usedNumbersMask &= ~(1u << _numbers[depth]);
            _numbers[depth] = 0;
        }

        public bool IsNumberUsed(int number) => (_usedNumbersMask & (1u << number)) != 0;

        public (string? error, bool changed) WriteExclusionsToRun()
        {
            bool runWasUpdated = false;
            for (int i = 0; i < Length; i++)
            {
                PlayableCell cell = _run.Cells[i];

                (string? error, bool cellWasUpdated) = cell.RemoveCandidates(_cellCandidatesBuffer[i].GetExclusionsMask());
                if (error != null) return (error, false);
                if (cellWasUpdated) runWasUpdated = true;
            }
            return (null, runWasUpdated);
        }
    }

    private sealed class RunCellCandidates
    {
        private uint _candidates;
        private uint _excluded;
        
        public CandidateEnumerator Candidates => new(_candidates);

        public void Load(PlayableCell cell)
        {
            _excluded = _candidates = cell.GetCandidatesMask();
        }
        
        public void ClearExclusions() => _excluded = 0;

        public void AddExclusion(int number) => _excluded |= 1u << (number - 1);

        public void RemoveExclusion(int number) => _excluded &= ~(1u << (number - 1));
        
        public uint GetExclusionsMask() => _excluded;

        public uint GetCandidatesMask() => _candidates;

        public struct CandidateEnumerator(uint mask)
        {
            private uint _mask = mask;
            public int Current { get; private set; } = 0;

            public readonly CandidateEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_mask == 0) return false;
                int bit = BitOperations.TrailingZeroCount(_mask);
                Current = bit + 1;
                _mask &= _mask - 1;
                return true;
            }
        }
    }
}
