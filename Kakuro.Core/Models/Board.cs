using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Kakuro.Core.Helpers;
using Kakuro.Core.Interfaces;

namespace Kakuro.Core.Models;

public sealed class Board
{
    private Puzzle Puzzle { get; }
    private IReadOnlyList<ICell> Cells { get; }
    private IReadOnlyList<PlayableCell> PlayableCells { get; }
    private IReadOnlyList<Run> Runs { get; }

    public Board(Puzzle puzzle)
    {
        Puzzle = puzzle;
        
        // prepare Cells array
        ICell[] cells = new ICell[Puzzle.Rows * Puzzle.Columns];
        Array.Fill(cells, EmptyCell.Instance);
        
        List<Run> runs = new(Puzzle.Runs.Length);
        foreach (Puzzle.Run puzzleRun in Puzzle.Runs)
        {
            // instantiate playable cells from puzzle runs
            List<PlayableCell> runCells = new(puzzleRun.Length);
            foreach ((int row, int column) in puzzleRun.GetCellCoordinates())
            {
                int cellsIndex = row * Puzzle.Columns + column;
                if (cells[cellsIndex] is not PlayableCell playableCell)
                {
                    playableCell = new PlayableCell(row, column);
                    cells[cellsIndex] = playableCell;
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
        
        Cells = cells.ToImmutableArray();
        PlayableCells = cells.OfType<PlayableCell>().ToImmutableArray();
        Runs = runs.ToImmutableArray();
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
        foreach (PlayableCell playableCell in PlayableCells)
        {
            if (!playableCell.IsSolved) return false;
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        HashSet<int> seen = [];
        foreach (Run run in Runs)
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
        ICell.IState[] states = new ICell.IState[PlayableCells.Count];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = PlayableCells[i].GetState();
        }
        return new State(ImmutableCollectionsMarshal.AsImmutableArray(states));
    }

    public void SetState(State state)
    {
        if (state is null || state.CellStates.IsDefault || state.CellStates.Length != PlayableCells.Count)
            throw new ArgumentException("Invalid state");
        
        for (int i = 0; i < PlayableCells.Count; i++)
        {
            if (!PlayableCells[i].IsValidState(state.CellStates[i]))
                throw new ArgumentException("Invalid state");
        }

        for (int i = 0; i < PlayableCells.Count; i++)
        {
            PlayableCells[i].SetState(state.CellStates[i]);
        }
    }
    
    public bool Solve(Action<string>? log = null)
    {
        string? error;
        State initialState = GetState();

        if (Constants.MinCandidate == 1 && Constants.MaxCandidate == 9)
        {
            // we can use the precalculated constraints
            log?.Invoke("Loading precomputed constraints...");
            error = LoadPrecomputedRunConstraints(log);
            if (error != null)
            {
                log?.Invoke($"Constraints conflict detected: {error}");
                SetState(initialState);
                return false;
            }
        }

        // mark all runs as dirty
        foreach (Run run in Runs)
        {
            run.MarkDirty();
        }
        
        RunConstraintWorkspace workspace = new();
        log?.Invoke("Calculating initial constraints...");
        error = CalculateRunConstraints(log, workspace);
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
        bool solved = SolveByGuessing(log, workspace, ref guesses, ref badGuesses);
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
        foreach (Run run in Runs)
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
    private string? CalculateRunConstraints(Action<string>? log, RunConstraintWorkspace workspace)
    {
        int pass = 0;
        bool changed;
        do
        {
            log?.Invoke($"{Constants.LogIndent}Pass {++pass}...");
            (string? error, changed) = UpdateCellConstraints(workspace);
            if (error != null) return error;
        } while (changed);
        return null;
    }

    private (string? error, bool changed) UpdateCellConstraints(RunConstraintWorkspace workspace)
    {
        bool boardWasUpdated = false;

        foreach (Run run in Runs)
        {
            if (!run.IsDirty) continue;
            if (run.IsSolved)
            {
                run.ClearDirty();
                continue;
            }
            
            workspace.Load(run);
            GetExcludedCandidates(0, 0, workspace);
            (string? error, bool runWasUpdated) = workspace.WriteExclusionsToRun();
            if (error != null) return (error, false);
            if (runWasUpdated) boardWasUpdated = true;
            
            run.ClearDirty();
        }

        return (null, boardWasUpdated);
    }

    private static bool GetExcludedCandidates(int depth, int runningTotal, RunConstraintWorkspace workspace)
    {
        if (depth >= workspace.RunLength)
        {
            bool done = false;
            if (runningTotal == workspace.RunTotal)
            {
                done = workspace.KeepCurrentNumbers();
            }
            return done;
        }

        int remainingCellsAfterThis = workspace.RunLength - depth - 1;
        RunCellCandidates runCellCandidates = workspace.GetRunCellCandidates(depth);
        foreach (int current in runCellCandidates.Candidates)
        {
            if (workspace.IsNumberUsed(current)) continue;

            int newRunningTotal = runningTotal + current;
            if (newRunningTotal > workspace.RunTotal) return false;
            
            int remainingSumNeeded = workspace.RunTotal - newRunningTotal;
            if (remainingSumNeeded < Constants.MinPossibleSum[remainingCellsAfterThis]) continue;
            if (remainingSumNeeded > Constants.MaxPossibleSum[remainingCellsAfterThis]) continue;

            workspace.UseNumber(depth, current);
            bool done = GetExcludedCandidates(depth + 1, newRunningTotal, workspace);
            workspace.ClearNumber(depth);
            if (done) return true; // every candidate has been confirmed viable, nothing left to exclude
        }
        return false;
    }

    private bool SolveByGuessing(Action<string>? log, RunConstraintWorkspace workspace, ref int guesses, ref int badGuesses)
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
            if (guessError == null && CalculateRunConstraints(null, workspace) == null && SolveByGuessing(log, workspace, ref guesses, ref badGuesses)) return true;
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
        
        foreach (PlayableCell playableCell in PlayableCells)
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

    private sealed class RunConstraintWorkspace
    {
        private readonly RunCellCandidates[] _cellCandidatesBuffer = Enumerable.Range(0, Constants.MaxRunLength).Select(_ => new RunCellCandidates()).ToArray();
        private readonly int[] _numbers = new int[Constants.MaxRunLength];
        private uint _usedNumbersMask;
        private Run _run = null!;

        public int RunLength => _run.Length;
        public int RunTotal => _run.Total;
        public RunCellCandidates GetRunCellCandidates(int index) => _cellCandidatesBuffer[index];

        public void Load(Run run)
        {
            _run = run;
            _usedNumbersMask = 0;
            Array.Clear(_numbers);
            for (int i = 0; i < RunLength; i++)
            {
                _cellCandidatesBuffer[i].Load(run.Cells[i]);
            }
        }

        public bool KeepCurrentNumbers()
        {
            uint remaining = 0;
            for (int i = 0; i < RunLength; i++)
            {
                _cellCandidatesBuffer[i].RemoveExclusion(_numbers[i]);
                remaining |= _cellCandidatesBuffer[i].GetExclusionsMask();
            }
            return remaining == 0;
        }

        public void UseNumber(int depth, int current)
        {
            _usedNumbersMask |= MaskHelper.BitFor(current);
            _numbers[depth] = current;
        }

        public void ClearNumber(int depth)
        {
            _usedNumbersMask &= ~MaskHelper.BitFor(_numbers[depth]);
            _numbers[depth] = 0;
        }

        public bool IsNumberUsed(int number) => (_usedNumbersMask & MaskHelper.BitFor(number)) != 0;

        public (string? error, bool changed) WriteExclusionsToRun()
        {
            bool runWasUpdated = false;
            for (int i = 0; i < RunLength; i++)
            {
                PlayableCell cell = _run.Cells[i];
                RunCellCandidates runCellCandidates = _cellCandidatesBuffer[i];

                (string? error, bool cellWasUpdated) = cell.RemoveCandidates(runCellCandidates.GetExclusionsMask());
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

        public void AddExclusion(int number) => _excluded |= MaskHelper.BitForCandidate(number);

        public void RemoveExclusion(int number) => _excluded &= ~MaskHelper.BitForCandidate(number);
        
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
                Current = bit + Constants.MinCandidate;
                _mask &= _mask - 1;
                return true;
            }
        }
    }
}
