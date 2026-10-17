namespace Kakuro.Core.Models;

public sealed class CandidateCollectionArray : CandidateCollection
{
    private readonly bool[] _candidates = new bool[9];

    #region interfaces
    public override IEnumerator<int> GetEnumerator()
    {
        for (var i = 0; i < _candidates.Length; i++)
        {
            if (_candidates[i])
            {
                yield return i + 1;
            }
        }
    }

    public override int Count
    {
        get
        {
            int count = 0;
            foreach (bool b in _candidates)
            {
                if (b) count++;
            }
            return count;
        }
    }

    #endregion

    public CandidateCollectionArray(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            Add(candidate);
        }
    }

    public override void Add(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, MaxCandidate);
        _candidates[candidate - 1] = true;
    }

    public override bool Remove(int candidate)
    {
        if (candidate is < MinCandidate or > MaxCandidate) return false;
        if (!Contains(candidate)) return false;
        _candidates[candidate - 1] = false;
        return true;
    }

    public override int Get(int index)
    {
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
        throw new ArgumentOutOfRangeException();
    }

    public override bool Contains(int candidate) => candidate is >= MinCandidate and <= MaxCandidate && _candidates[candidate - 1];

    public override void Clear() => Array.Clear(_candidates);

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

    public override CandidateCollectionState GetState() => new CandidateCollectionArrayState(_candidates.ToArray());

    public override void SetState(CandidateCollectionState state)
    {
        if (state is not CandidateCollectionArrayState arrayState)
            throw new ArgumentException("Invalid state type", nameof(state));
        Array.Copy(arrayState.Candidates, _candidates, _candidates.Length);
    }
    
    private sealed record CandidateCollectionArrayState(bool[] Candidates) : CandidateCollectionState
    {
        public override bool IsStateValid(int value)
        {
            if (Candidates.Length != 9) return false;
            if (value is < 0 or > 9) return false;
            if (!Candidates.Any(b => b)) return false;
            if (value != 0)
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
            return Candidates.SequenceEqual(other.Candidates);
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