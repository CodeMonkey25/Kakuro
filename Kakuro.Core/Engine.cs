using System.Collections.Immutable;
using System.Numerics;
using Kakuro.Core.Helpers;
using Kakuro.Core.Models;

namespace Kakuro.Core;

public sealed class Engine
{
    private readonly Action<string>? _log;

    public Engine(Action<string>? log = null)
    {
        _log = log;
    }

    public bool Solve(Puzzle puzzle)
    {
        Board board = new(puzzle);
        
        bool solved = Solve(board);
        if (solved)
        {
            _log?.Invoke(board.ToString());
            if (board.IsSolutionValid())
            {
                _log?.Invoke("Solution is valid");
            }
            else
            {
                _log?.Invoke("Solution is invalid");
            }
        }
        else
        {
            _log?.Invoke("Unable to solve puzzle");
        }
        
        return solved;
    }

    private bool Solve(Board board)
    {
        Board.State initialState = board.GetState();
        string? error;

        _log?.Invoke("Solving puzzle...");
        _log?.Invoke(board.ToString());

        if (Constants.MinCandidate == 1 && Constants.MaxCandidate == 9)
        {
            // we can use the precalculated constraints
            _log?.Invoke("Loading precomputed constraints...");
            error = LoadPrecomputedRunConstraints(board);
            if (error != null)
            {
                _log?.Invoke($"Constraints conflict detected: {error}");
                board.SetState(initialState);
                return false;
            }
        }

        // mark all runs as dirty
        foreach (Board.Run run in board.Runs)
        {
            run.MarkDirty();
        }
        
        RunConstraintWorkspace workspace = new();
        _log?.Invoke("Calculating initial constraints...");
        error = CalculateRunConstraints(board, workspace, true);
        if (error != null)
        {
            _log?.Invoke($"Constraints conflict detected: {error}");
            board.SetState(initialState);
            return false;
        }

        if (board.IsSolved()) return true;
        
        _log?.Invoke("Solving by guessing...");
        int guesses = 0;
        int badGuesses = 0;
        bool solved = SolveByGuessing(board, workspace, ref guesses, ref badGuesses);
        if (!solved)
        {
            _log?.Invoke("Unable to guess a solution");
            board.SetState(initialState);
            return false;
        }
        
        _log?.Invoke($"Solved in {guesses} guesses, not including {badGuesses} bad guesses.");
        return true;
    }

    private string? LoadPrecomputedRunConstraints(Board board)
    {
        foreach (Board.Run run in board.Runs)
        {
            (uint mask, ImmutableArray<int> exclusions) = RunCandidatesTable.GetRunExclusionMask(run.Length, run.Total);
            if (mask == 0) continue;
            
            _log?.Invoke($"{Constants.LogIndent}Run ({run.Length}, {run.Total}): candidates removed [{string.Join(", ", exclusions)}]...");
            
            foreach (PlayableCell cell in run.Cells)
            {
                (string? error, _) = cell.RemoveCandidates(mask);
                if (error != null) return error;
            }
        }
        return null;
    }

    // repeats UpdateCellConstraints() until nothing changes; returns null on success or the first error
    private string? CalculateRunConstraints(Board board, RunConstraintWorkspace workspace, bool isLogging = false)
    {
        int pass = 0;
        bool changed;
        do
        {
            if (isLogging)
                _log?.Invoke($"{Constants.LogIndent}Pass {++pass}...");
            (string? error, changed) = UpdateCellConstraints(board, workspace);
            if (error != null) return error;
        } while (changed);
        return null;
    }

    private (string? error, bool changed) UpdateCellConstraints(Board board, RunConstraintWorkspace workspace)
    {
        bool boardWasUpdated = false;

        foreach (Board.Run run in board.Runs)
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

    private bool GetExcludedCandidates(int depth, int runningTotal, RunConstraintWorkspace workspace)
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

    private bool SolveByGuessing(Board board, RunConstraintWorkspace workspace, ref int guesses, ref int badGuesses)
    {
        string indent = string.Concat(Enumerable.Repeat(Constants.LogIndent, guesses + 1));
        
        // identify unsolved cell with lowest number of candidates
        PlayableCell? cell = GetCellWithLowestCandidates(board);
        if (cell == null) return true; // no unsolved cells remain
        
        // load candidates for guessing
        Span<int> candidates = stackalloc int[cell.CandidateCount];
        cell.CopyCandidatesTo(candidates);
        
        // try each candidate in turn
        Board.State state = board.GetState();
        foreach (int candidate in candidates)
        {
            _log?.Invoke($"{indent}Cell ({cell.Row}, {cell.Column}): guessing {candidate}...");
            string? guessError = cell.Solve(candidate);
            guesses++;
            if (guessError == null && CalculateRunConstraints(board, workspace) == null && SolveByGuessing(board, workspace, ref guesses, ref badGuesses)) return true;
            _log?.Invoke($"{indent}Cell ({cell.Row}, {cell.Column}): guess {candidate} was bad, reverting...");
            board.SetState(state);
            guesses--;
            badGuesses++;
        }
        
        return false;
    }

    private PlayableCell? GetCellWithLowestCandidates(Board board)
    {
        PlayableCell? result = null;
        
        foreach (PlayableCell playableCell in board.PlayableCells)
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

    private sealed class RunConstraintWorkspace
    {
        private readonly RunCellCandidates[] _cellCandidatesBuffer = Enumerable.Range(0, Constants.MaxRunLength).Select(_ => new RunCellCandidates()).ToArray();
        private readonly int[] _numbers = new int[Constants.MaxRunLength];
        private uint _usedNumbersMask;
        private Board.Run _run = null!;

        public int RunLength => _run.Length;
        public int RunTotal => _run.Total;
        public RunCellCandidates GetRunCellCandidates(int index) => _cellCandidatesBuffer[index];

        public void Load(Board.Run run)
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