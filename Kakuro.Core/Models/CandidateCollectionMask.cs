using System.Numerics;

namespace Kakuro.Core.Models;

/// <summary>
/// Bitmask-backed candidate set for values 1..9. Value <c>v</c> is stored in bit <c>v - 1</c>.
/// Candidates are unique and enumerate in ascending order.
/// </summary>
public class CandidateCollectionMask : CandidateCollection
{
    private const int MinCandidate = 1;
    private const int MaxCandidate = 9;

    private uint _candidateMask;

    public CandidateCollectionMask(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            _candidateMask |= BitFor(candidate);
        }
    }

    private static uint BitFor(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, MaxCandidate);
        return 1u << (candidate - 1);
    }

    public override IEnumerator<int> GetEnumerator()
    {
        for (uint mask = _candidateMask; mask != 0; mask &= mask - 1)
        {
            yield return BitOperations.TrailingZeroCount(mask) + 1;
        }
    }

    public override int Count => BitOperations.PopCount(_candidateMask);

    public override void Add(int candidate)
    {
        _candidateMask |= BitFor(candidate);
    }

    public override bool Remove(int candidate)
    {
        if (candidate is < MinCandidate or > MaxCandidate) return false;

        uint bit = 1u << (candidate - 1);
        bool removed = (_candidateMask & bit) != 0;
        _candidateMask &= ~bit;
        return removed;
    }

    public override void Clear()
    {
        _candidateMask = 0;
    }

    public override CandidateCollectionState GetState() => new CandidateCollectionMaskState(_candidateMask);

    public override void SetState(CandidateCollectionState state)
    {
        if (state is not CandidateCollectionMaskState maskState)
            throw new ArgumentException("Invalid state type", nameof(state));
        _candidateMask = maskState.CandidateMask;
    }
    
    public override void CopyTo(Span<int> candidates)
    {
        int i = 0;
        for (uint m = _candidateMask; m != 0; m &= m - 1)
        {
            candidates[i++] = BitOperations.TrailingZeroCount(m) + 1;
        }
    }

    private sealed class CandidateCollectionMaskState(uint candidateMask) : CandidateCollectionState, IEquatable<CandidateCollectionMaskState>
    {
        private const uint ValidBits = (1u << MaxCandidate) - 1;

        public readonly uint CandidateMask = candidateMask;

        public override bool IsStateValid(int value)
        {
            if (value != 0)
            {
                return value is >= MinCandidate and <= MaxCandidate && CandidateMask == 1u << (value - 1);
            }

            return CandidateMask != 0 && (CandidateMask & ~ValidBits) == 0;
        }

        public bool Equals(CandidateCollectionMaskState? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return CandidateMask == other.CandidateMask;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is CandidateCollectionMaskState other && Equals(other);

        public override int GetHashCode() => (int)CandidateMask;
    }
}
