using System.Collections;

namespace Kakuro.Core.Models;

public abstract class CandidateCollection : IReadOnlyCollection<int>
{
    protected const int MinCandidate = 1;
    protected const int MaxCandidate = 9;
    
    public static CandidateCollection Build(IEnumerable<int> candidates) => new CandidateCollectionMask(candidates);
    
    public abstract IEnumerator<int> GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public abstract int Count { get; }

    public abstract void Add(int candidate);
    public abstract bool Remove(int candidate);
    public abstract int Get(int index);
    public abstract bool Contains(int candidate);
    public abstract void Clear();
    public abstract CandidateCollectionState GetState();
    public abstract void SetState(CandidateCollectionState state);
    public abstract void CopyTo(Span<int> destination);

    public abstract class CandidateCollectionState
    {
        public abstract bool IsStateValid(int value);
    }
}
