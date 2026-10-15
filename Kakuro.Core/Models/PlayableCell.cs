namespace Kakuro.Core.Models;

public sealed class PlayableCell : Cell
{
    public int Row { get; }
    public int Column { get; }
    public int Value { get; private set; }
    public bool IsSolved => Value != 0;
    
    private readonly CandidateCollection _candidates = CandidateCollection.Build([1, 2, 3, 4, 5, 6, 7, 8, 9]);
    public IReadOnlyCollection<int> Candidates => _candidates;
    
    private readonly List<PlayableCell> _linkedCells = [];
    public IReadOnlyList<PlayableCell> LinkedCells => _linkedCells;
    
    public PlayableCell(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public (string? error, bool removed) RemoveCandidates(HashSet<int> candidates)
    {
        string? error = null;
        bool removed = false;
        foreach (int c in candidates)
        {
            (error, bool removedCandidate) = RemoveCandidate(c);
            if (error != null) break;
            if (removedCandidate) removed = true;
        }
        return (error, removed);
    }

    public (string? error, bool removed) RemoveCandidate(int candidate)
    {
        string? error = null;
        bool removed = false;
        
        if (IsSolved)
        {
            error = Value == candidate ? $"Cell at row {Row}, column {Column} has no candidates." : null;
        }
        else if (_candidates.Remove(candidate))
        {
            removed = true;
            
            switch (_candidates.Count)
            {
                case 0:
                    error = $"Cell at row {Row}, column {Column} has no candidates.";
                    break;
                case 1:
                    error = Solve(_candidates.First());
                    break;
            }
        }
        
        return (error, removed);
    }

    public string? Solve(int value)
    {
        if (IsSolved)
        {
            return Value != value ? $"Cell at row {Row}, column {Column} is already solved." : null;
        }
        
        Value = value;
        _candidates.Clear();
        _candidates.Add(value);
        
        foreach (PlayableCell linkedCell in _linkedCells)
        {
            (string? error, _) = linkedCell.RemoveCandidate(value);
            if (error != null) return error;
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
    
    public override string ToString() => Value == 0 ? " " : Value.ToString();
    
    public override State GetState() => new PlayableCellState(Value, _candidates.GetState());

    public override void SetState(State state)
    {
        if (state is not PlayableCellState playableCellState || !IsValidState(playableCellState))
            throw new ArgumentException("Invalid state");
        
        Value = playableCellState.Value;
        _candidates.SetState(playableCellState.Candidates);
    }

    public override bool IsValidState(State state)
    {
        if (state is not PlayableCellState { Value: var value, Candidates: var candidates }) return false;
        if (value is < 0 or > 9) return false;
        return candidates.IsStateValid(value);
    }

    public void CopyCandidatesTo(Span<int> candidates) => _candidates.CopyTo(candidates);

    private sealed record PlayableCellState(int Value, CandidateCollection.CandidateCollectionState Candidates) : Cell.State
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