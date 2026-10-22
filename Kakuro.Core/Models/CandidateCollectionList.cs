using System.Collections.Immutable;

namespace Kakuro.Core.Models;

public sealed class CandidateCollectionList : CandidateCollection
{
    private readonly List<int> _candidates = [];

    public CandidateCollectionList(IEnumerable<int> candidates)
    {
        AddAll(candidates);
    }

    public override int Count => _candidates.Count;

    public override void Add(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, Constants.MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, Constants.MaxCandidate);
        if (Contains(candidate)) return;
        _candidates.Add(candidate);
        _candidates.Sort();
    }

    public override void AddAll(IEnumerable<int> candidates)
    {
        try
        {
            foreach (int candidate in candidates)
            {
                if (Contains(candidate)) continue;
                ArgumentOutOfRangeException.ThrowIfLessThan(candidate, Constants.MinCandidate);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, Constants.MaxCandidate);
                _candidates.Add(candidate);
            }
        }
        finally
        {
            _candidates.Sort();
        }
    }

    public override bool Remove(int candidate) => _candidates.Remove(candidate);

    public override int Get(int index) => _candidates[index];
    
    public override uint GetMask()
    {
        uint mask = 0;
        foreach (int candidate in _candidates)
        {
            mask |= 1u << (candidate - Constants.MinCandidate);
        }
        return mask;
    }

    public override bool Contains(int candidate) => _candidates.Contains(candidate);
    
    public override void Clear() => _candidates.Clear();
    
    public override ICandidateCollectionState GetState() => new CandidateCollectionListState(_candidates.ToImmutableArray());
    
    public override void SetState(ICandidateCollectionState state)
    {
        if (state is not CandidateCollectionListState listState)
            throw new ArgumentException("Invalid state type", nameof(state));
        Clear();
        AddAll(listState.Candidates);
    }
    
    public override void CopyTo(Span<int> destination) => _candidates.CopyTo(destination);

    private sealed record CandidateCollectionListState(ImmutableArray<int> Candidates) : ICandidateCollectionState
    {
        public bool IsStateValid(int value)
        {
            if (Candidates.IsDefault) return false;
            if (value != Constants.UnsolvedCell) return Candidates.Length == 1 && Candidates[0] == value;
            return Candidates.Length > 0 && Candidates.All(c => c is >= Constants.MinCandidate and <= Constants.MaxCandidate);
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
