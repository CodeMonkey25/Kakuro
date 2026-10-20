using Kakuro.Core.Interfaces;

namespace Kakuro.Core.Models;

public sealed class PlayableCell(int row, int column) : ICell
{
    public int Row { get; } = row;
    public int Column { get; } = column;
    public int Value { get; private set; }
    public bool IsSolved => Value != Constants.UnsolvedCell;
    
    private readonly CandidateCollection _candidates = CandidateCollection.Build(Constants.AllCandidates);
    public int CandidateCount => _candidates.Count;
    
    private readonly List<PlayableCell> _linkedCells = [];
    public IReadOnlyList<PlayableCell> LinkedCells => _linkedCells;
    
    private readonly List<Board.Run> _linkedRuns = [];

    public (string? error, bool removed) RemoveCandidates(uint candidatesMask)
    {
        if (IsSolved)
        {
            bool removesValue = (candidatesMask & (1u << (Value - 1))) != 0;
            return (removesValue ? NoCandidatesError() : null, false);
        }

        return AfterRemoval(_candidates.RemoveAll(candidatesMask));
    }

    public (string? error, bool removed) RemoveCandidates(IEnumerable<int> candidates)
    {
        if (IsSolved)
        {
            return (candidates.Contains(Value) ? NoCandidatesError() : null, false);
        }

        return AfterRemoval(_candidates.RemoveAll(candidates));
    }

    public (string? error, bool removed) RemoveCandidate(int candidate)
    {
        if (IsSolved)
        {
            return (Value == candidate ? NoCandidatesError() : null, false);
        }

        return AfterRemoval(_candidates.Remove(candidate));
    }

    private string NoCandidatesError() => $"Cell at row {Row}, column {Column} has no candidates.";

    private (string? error, bool removed) AfterRemoval(bool removed)
    {
        if (!removed) return (null, false);

        foreach (Board.Run run in _linkedRuns)
        {
            run.MarkDirty();
        }

        string? error = _candidates.Count switch
        {
            0 => NoCandidatesError(),
            1 => Solve(_candidates.Get(0)),
            _ => null
        };
        return (error, true);
    }

    public string? Solve(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, Constants.MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Constants.MaxCandidate);

        if (IsSolved)
        {
            return Value != value ? $"Cell at row {Row}, column {Column} is already solved." : null;
        }

        if (!_candidates.Contains(value))
        {
            return $"Cell at row {Row}, column {Column} does not contain candidate {value}.";
        }

        Value = value;
        _candidates.Clear();
        _candidates.Add(value);
        
        foreach (PlayableCell linkedCell in _linkedCells)
        {
            (string? error, _) = linkedCell.RemoveCandidate(value);
            if (error != null) return error;
        }
        
        foreach (Board.Run run in _linkedRuns)
        {
            run.MarkDirty();
        }
        
        return null;
    }
    
    public void Bind(IEnumerable<PlayableCell> linkedCells)
    {
        foreach (PlayableCell linkedCell in linkedCells)
        {
            if (linkedCell == this) continue;
            if (_linkedCells.Contains(linkedCell)) continue;
            _linkedCells.Add(linkedCell);
        }
    }

    internal void BindRun(Board.Run run)
    {
        _linkedRuns.Add(run);
    }
    
    public override string ToString() => Value == Constants.UnsolvedCell ? " " : Value.ToString();
    
    public ICell.IState GetState() => new PlayableCellState(Value, _candidates.GetState());

    public void SetState(ICell.IState state)
    {
        if (state is not PlayableCellState playableCellState || !IsValidState(playableCellState))
            throw new ArgumentException("Invalid state");
        
        Value = playableCellState.Value;
        _candidates.SetState(playableCellState.Candidates);
    }

    public bool IsValidState(ICell.IState state)
    {
        if (state is not PlayableCellState { Value: var value, Candidates: var candidates }) return false;
        if (value != Constants.UnsolvedCell && value is < Constants.MinCandidate or > Constants.MaxCandidate) return false;
        return candidates.IsStateValid(value);
    }

    public uint GetCandidatesMask() => _candidates.GetMask();

    public void CopyCandidatesTo(Span<int> candidates) => _candidates.CopyTo(candidates);

    private sealed record PlayableCellState(int Value, CandidateCollection.ICandidateCollectionState Candidates) : ICell.IState
    {
        public bool Equals(PlayableCellState? other) =>
            other is not null && Value == other.Value && Candidates.Equals(other.Candidates);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Value);
            hash.Add(Candidates);
            return hash.ToHashCode();
        }
    }
}