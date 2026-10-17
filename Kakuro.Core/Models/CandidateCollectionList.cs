using System.Collections.Immutable;

namespace Kakuro.Core.Models;

public sealed record CandidateCollectionList : CandidateCollection
{
    private readonly List<int> _candidates = [];

    public CandidateCollectionList(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            Add(candidate);
        }
    }

    #region interfaces
    public override IEnumerator<int> GetEnumerator() => _candidates.GetEnumerator();

    public override int Count => _candidates.Count;
    #endregion

    public bool Equals(CandidateCollectionList? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _candidates.SequenceEqual(other._candidates);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (int candidate in _candidates)
        {
            hashCode.Add(candidate);
        }
        return hashCode.ToHashCode();
    }

    public override void Add(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, MaxCandidate);
        _candidates.Add(candidate);
    }

    public override bool Remove(int candidate) => _candidates.Remove(candidate);
    
    public override int Get(int index) => _candidates[index];

    public override bool Contains(int candidate) => _candidates.Contains(candidate);
    
    public override void Clear() => _candidates.Clear();
    
    public override CandidateCollectionState GetState() => new CandidateCollectionListState(_candidates.ToImmutableArray());
    
    public override void SetState(CandidateCollectionState state)
    {
        if (state is not CandidateCollectionListState listState)
            throw new ArgumentException("Invalid state type", nameof(state));
        _candidates.Clear();
        foreach (int candidate in listState.Candidates)
        {
            _candidates.Add(candidate);
        }
    }
    
    public override void CopyTo(Span<int> destination) => _candidates.CopyTo(destination);

    private sealed record CandidateCollectionListState(ImmutableArray<int> Candidates) : CandidateCollectionState
    {
        public override bool IsStateValid(int value)
        {
            if (Candidates.IsDefault) return false;
            if (value != 0) return Candidates.Length == 1 && Candidates[0] == value;
            return Candidates.Length > 0 && Candidates.All(c => c is >= 1 and <= 9);
        }

        public bool Equals(CandidateCollectionListState? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Candidates.AsSpan().SequenceEqual(other.Candidates.AsSpan());
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();
            foreach (int candidate in Candidates)
            {
                hashCode.Add(candidate);
            }
            return hashCode.ToHashCode();
        }
    }
}
