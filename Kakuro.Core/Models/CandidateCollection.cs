using System.Collections;

namespace Kakuro.Core.Models;

public abstract class CandidateCollection
{
    public static CandidateCollection Build(IEnumerable<int> candidates) => new CandidateCollectionMask(candidates);
    
    public abstract int Count { get; }

    public abstract void Add(int candidate);
    public abstract bool Remove(int candidate);
    public abstract int Get(int index);
    public abstract bool Contains(int candidate);
    public abstract void Clear();
    public abstract CandidateCollectionState GetState();
    public abstract void SetState(CandidateCollectionState state);
    public abstract void CopyTo(Span<int> destination);

    public abstract record CandidateCollectionState
    {
        public abstract bool IsStateValid(int value);
    }
}
