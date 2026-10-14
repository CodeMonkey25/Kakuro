using System.Collections;

namespace Kakuro.Core.Models;

public abstract class CandidateCollection : IReadOnlyList<int>
{
    public abstract IEnumerator<int> GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public abstract int Count { get; }
    public abstract int this[int index] { get; }

    public abstract void Add(int candidate);
    public abstract bool Remove(int candidate);
    public abstract void Clear();
    public abstract CandidateCollectionState GetState();
    public abstract void SetState(CandidateCollectionState state);

    public abstract class CandidateCollectionState
    {
        public abstract bool IsStateValid(int value);
    }
}
