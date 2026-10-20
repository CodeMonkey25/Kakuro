namespace Kakuro.Core.Models;

public sealed class CandidateCollectionArray : CandidateCollection
{
    private readonly bool[] _candidates = new bool[Constants.MaxCandidate];
    private int _count;

    public override int Count => _count;

    public CandidateCollectionArray(IEnumerable<int> candidates)
    {
        AddAll(candidates);
    }

    public override void Add(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, Constants.MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, Constants.MaxCandidate);
        if (Contains(candidate)) return;
        _candidates[candidate - 1] = true;
        _count++;
    }

    public override bool Remove(int candidate)
    {
        if (candidate is < Constants.MinCandidate or > Constants.MaxCandidate) return false;
        if (!Contains(candidate)) return false;
        _candidates[candidate - 1] = false;
        _count--;
        return true;
    }

    public override int Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        var j = 0;
        for (int i = 0; i < _candidates.Length; i++)
        {
            if (_candidates[i])
            {
                if (j == index)
                {
                    return i + 1;
                }
                j++;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public override bool Contains(int candidate) => candidate is >= Constants.MinCandidate and <= Constants.MaxCandidate && _candidates[candidate - 1];

    public override void Clear()
    {
        Array.Clear(_candidates);
        _count = 0;
    }

    public override void CopyTo(Span<int> destination)
    {
        var j = 0;
        for (int i = 0; i < _candidates.Length; i++)
        {
            if (_candidates[i])
            {
                destination[j++] = i + 1;
            }
        }
    }

    public override CandidateCollectionState GetState() => new CandidateCollectionArrayState(_candidates.ToArray(), _count);

    public override void SetState(CandidateCollectionState state)
    {
        if (state is not CandidateCollectionArrayState arrayState)
            throw new ArgumentException("Invalid state type", nameof(state));
        Array.Copy(arrayState.Candidates, _candidates, _candidates.Length);
        _count = arrayState.Count;
    }
    
    private sealed record CandidateCollectionArrayState(bool[] Candidates, int Count) : CandidateCollectionState
    {
        public override bool IsStateValid(int value)
        {
            if (Candidates.Length != Constants.MaxCandidate) return false;
            if (value != Constants.UnsolvedCell && value is < Constants.MinCandidate or > Constants.MaxCandidate) return false;
            if (!Candidates.Any(b => b)) return false;
            if (value != Constants.UnsolvedCell)
            {
                int count = 0;
                foreach (bool b in Candidates)
                {
                    if (b) count++;
                }
                return Candidates[value - 1] && count == 1;
            }
            return true;
        }
        
        public bool Equals(CandidateCollectionArrayState? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Candidates.AsSpan().SequenceEqual(other.Candidates.AsSpan());
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();
            foreach (bool candidate in Candidates)
            {
                hashCode.Add(candidate);
            }
            return hashCode.ToHashCode();
        }
    }
}