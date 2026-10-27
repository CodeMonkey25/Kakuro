using System.Collections;
using System.Numerics;

namespace Kakuro.Core.Models;

public abstract class CandidateCollection : IReadOnlyList<int>
{
    #region interfaces

    public IEnumerator<int> GetEnumerator()
    {
        int[] items = new int[Count];
        CopyTo(items);
        foreach (int i in items)
        {
            yield return i;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public abstract int Count { get; }

    public int this[int index] => Get(index);

    #endregion

    public static CandidateCollection Build(IEnumerable<int> candidates) => new CandidateCollectionMask(candidates);

    public abstract void Add(int candidate);
    public abstract bool Remove(int candidate);
    public abstract int Get(int index);
    public abstract uint GetMask();
    public abstract bool Contains(int candidate);
    public abstract void Clear();
    public abstract ICandidateCollectionState GetState();
    public abstract void SetState(ICandidateCollectionState state);
    public abstract void CopyTo(Span<int> destination);

    public virtual void AddAll(IEnumerable<int> candidates)
    {
        foreach (int candidate in candidates)
        {
            Add(candidate);
        }
    }

    public virtual bool RemoveAll(uint candidatesMask)
    {
        bool removed = false;
        for (uint mask = candidatesMask; mask != 0; mask &= mask - 1)
        {
            int candidate = BitOperations.TrailingZeroCount(mask) + Constants.MinCandidate;
            removed |= Remove(candidate);
        }
        return removed;
    }

    public virtual bool RemoveAll(IEnumerable<int> candidates)
    {
        bool removed = false;
        foreach (int candidate in candidates)
        {
            removed |= Remove(candidate);
        }
        return removed;
    }

    public interface ICandidateCollectionState
    {
        public bool IsStateValid(int value);
    }
}
