using System.Numerics;
using Kakuro.Core.Helpers;

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

    public override int Count => BitOperations.PopCount(_candidateMask);

    public override void Add(int candidate)
    {
        _candidateMask |= MaskHelper.BitForCandidate(candidate);
    }

    public override void AddAll(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            _candidateMask |= MaskHelper.BitForCandidate(candidate);
        }
    }

    public override bool Remove(int candidate)
    {
        if (candidate is < Constants.MinCandidate or > Constants.MaxCandidate) return false;

        uint bit = MaskHelper.BitFor(candidate - Constants.MinCandidate);
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
        candidate is >= Constants.MinCandidate and <= Constants.MaxCandidate && (_candidateMask & MaskHelper.BitFor(candidate - Constants.MinCandidate)) != 0;

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
        if (destination.Length < Count) throw new ArgumentException("Destination is too small", nameof(destination));
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
                mask |= MaskHelper.BitFor(c - Constants.MinCandidate);
        }
        return RemoveAll(mask);
    }

    private sealed record CandidateCollectionMaskState(uint CandidateMask) : ICandidateCollectionState
    {
        public bool IsStateValid(int value)
        {
            if (value != Constants.UnsolvedCell)
            {
                return value is >= Constants.MinCandidate and <= Constants.MaxCandidate && CandidateMask == MaskHelper.BitFor(value - Constants.MinCandidate);
            }

            return CandidateMask != 0 && (CandidateMask & ~Constants.AllCandidatesMask) == 0;
        }
    }
}
