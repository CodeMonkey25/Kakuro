using BenchmarkDotNet.Attributes;
using Kakuro.Core;
using Kakuro.Core.Models;

namespace Kakuro.Benchmarks;

[MemoryDiagnoser]
public class CandidateCollectionBenchmarks
{
    private static readonly int[] Source = Constants.AllCandidates.ToArray();

    private CandidateCollection _list = null!;
    private CandidateCollection _mask = null!;
    private CandidateCollection _array = null!;
    private CandidateCollection.ICandidateCollectionState _listState = null!;
    private CandidateCollection.ICandidateCollectionState _maskState = null!;
    private CandidateCollection.ICandidateCollectionState _arrayState = null!;
    private readonly int[] _buffer = new int[Constants.MaxRunLength];

    [GlobalSetup]
    public void Setup()
    {
        _list = new CandidateCollectionList(Source);
        _mask = new CandidateCollectionMask(Source);
        _listState = _list.GetState();
        _maskState = _mask.GetState();
        _array = new CandidateCollectionArray(Source);
        _arrayState = _array.GetState();
    }

    [Benchmark] public CandidateCollection Construct_List() => new CandidateCollectionList(Source);
    [Benchmark] public CandidateCollection Construct_Mask() => new CandidateCollectionMask(Source);
    [Benchmark] public CandidateCollection Construct_Array() => new CandidateCollectionArray(Source);

    [Benchmark]
    public int Iterate_List()
    {
        int sum = 0;
        for (int i = 0; i < _list.Count; i++) sum += _list.Get(i);
        return sum;
    }

    [Benchmark]
    public int Iterate_Mask()
    {
        int sum = 0;
        for (int i = 0; i < _mask.Count; i++) sum += _mask.Get(i);
        return sum;
    }

    [Benchmark]
    public int Iterate_Array()
    {
        int sum = 0;
        for (int i = 0; i < _array.Count; i++) sum += _array.Get(i);
        return sum;
    }

    [Benchmark]
    public int Contains_List()
    {
        int hits = 0;
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) if (_list.Contains(i)) hits++;
        return hits;
    }

    [Benchmark]
    public int Contains_Mask()
    {
        int hits = 0;
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) if (_mask.Contains(i)) hits++;
        return hits;
    }

    [Benchmark]
    public int Contains_Array()
    {
        int hits = 0;
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) if (_array.Contains(i)) hits++;
        return hits;
    }

    [Benchmark]
    public int Count_List() => _list.Count;

    [Benchmark]
    public int Count_Mask() => _mask.Count;

    [Benchmark]
    public int Count_Array() => _array.Count;

    [Benchmark]
    public int Get_List()
    {
        int sum = 0;
        for (int i = 0; i < Constants.MaxRunLength; i++) sum += _list.Get(i);
        return sum;
    }

    [Benchmark]
    public int Get_Mask()
    {
        int sum = 0;
        for (int i = 0; i < Constants.MaxRunLength; i++) sum += _mask.Get(i);
        return sum;
    }

    [Benchmark]
    public int Get_Array()
    {
        int sum = 0;
        for (int i = 0; i < Constants.MaxRunLength; i++) sum += _array.Get(i);
        return sum;
    }

    [Benchmark]
    public void RemoveAdd_List()
    {
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _list.Remove(i);
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _list.Add(i);
    }

    [Benchmark]
    public void RemoveAdd_Mask()
    {
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _mask.Remove(i);
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _mask.Add(i);
    }

    [Benchmark]
    public void RemoveAdd_Array()
    {
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _array.Remove(i);
        for (int i = Constants.MinCandidate; i <= Constants.MaxCandidate; i++) _array.Add(i);
    }

    [Benchmark]
    public void CopyTo_List() => _list.CopyTo(_buffer);

    [Benchmark]
    public void CopyTo_Mask() => _mask.CopyTo(_buffer);

    [Benchmark]
    public void CopyTo_Array() => _array.CopyTo(_buffer);

    [Benchmark]
    public CandidateCollection.ICandidateCollectionState GetState_List() => _list.GetState();

    [Benchmark]
    public CandidateCollection.ICandidateCollectionState GetState_Mask() => _mask.GetState();

    [Benchmark]
    public CandidateCollection.ICandidateCollectionState GetState_Array() => _array.GetState();

    [Benchmark]
    public void SetState_List() => _list.SetState(_listState);

    [Benchmark]
    public void SetState_Mask() => _mask.SetState(_maskState);

    [Benchmark]
    public void SetState_Array() => _array.SetState(_arrayState);
}
