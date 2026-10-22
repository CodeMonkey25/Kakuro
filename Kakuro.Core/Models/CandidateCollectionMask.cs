using System.Numerics;

namespace Kakuro.Core.Models;

/// <summary>
/// Bitmask-backed candidate set for values <c>MinCandidate</c>..<c>MaxCandidate</c>. Value <c>v</c> is stored in bit <c>v - MinCandidate</c>.
/// Candidates are unique and enumerate in ascending order.
/// </summary>
public sealed class CandidateCollectionMask : CandidateCollection
{
    private uint _candidateMask;

    public CandidateCollectionMask(IEnumerable<int> candidates)
    {
        AddAll(candidates);
    }

    private static uint BitFor(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, Constants.MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, Constants.MaxCandidate);
        return 1u << (candidate - Constants.MinCandidate);
    }

    public override int Count => BitOperations.PopCount(_candidateMask);

    public override void Add(int candidate)
    {
        _candidateMask |= BitFor(candidate);
    }

    public override void AddAll(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            _candidateMask |= BitFor(candidate);
        }
    }

    public override bool Remove(int candidate)
    {
        if (candidate is < Constants.MinCandidate or > Constants.MaxCandidate) return false;

        uint bit = 1u << (candidate - Constants.MinCandidate);
        bool removed = (_candidateMask & bit) != 0;
        _candidateMask &= ~bit;
        return removed;
    }
    
    public override int Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        uint mask = _candidateMask;
        for (int i = 0; i < index; i++)
        {
            mask &= mask - 1;
        }
        return BitOperations.TrailingZeroCount(mask) + Constants.MinCandidate;
    }
    
    public override uint GetMask() => _candidateMask;

    public override bool Contains(int candidate) =>
        candidate is >= Constants.MinCandidate and <= Constants.MaxCandidate && (_candidateMask & (1u << (candidate - Constants.MinCandidate))) != 0;

    public override void Clear()
    {
        _candidateMask = 0;
    }

    public override ICandidateCollectionState GetState() => new CandidateCollectionMaskState(_candidateMask);

    public override void SetState(ICandidateCollectionState state)
    {
        if (state is not CandidateCollectionMaskState maskState)
            throw new ArgumentException("Invalid state type", nameof(state));
        _candidateMask = maskState.CandidateMask;
    }
    
    public override void CopyTo(Span<int> destination)
    {
        int i = 0;
        for (uint m = _candidateMask; m != 0; m &= m - 1)
        {
            destination[i++] = BitOperations.TrailingZeroCount(m) + Constants.MinCandidate;
        }
    }

    public override bool RemoveAll(uint candidatesMask)
    {
        uint toRemove = _candidateMask & candidatesMask & Constants.AllCandidatesMask;
        if (toRemove == 0) return false;
        _candidateMask &= ~toRemove;
        return true;
    }

    public override bool RemoveAll(IEnumerable<int> candidates)
    {
        uint mask = 0;
        foreach (int c in candidates)
        {
            if (c is >= Constants.MinCandidate and <= Constants.MaxCandidate)
                mask |= 1u << (c - Constants.MinCandidate);
        }
        return RemoveAll(mask);
    }

    private sealed record CandidateCollectionMaskState(uint CandidateMask) : ICandidateCollectionState
    {
        public bool IsStateValid(int value)
        {
            if (value != Constants.UnsolvedCell)
            {
                return value is >= Constants.MinCandidate and <= Constants.MaxCandidate && CandidateMask == 1u << (value - Constants.MinCandidate);
            }

            return CandidateMask != 0 && (CandidateMask & ~Constants.AllCandidatesMask) == 0;
        }
    }
}
