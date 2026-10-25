using BenchmarkDotNet.Attributes;
using Kakuro.Core;
using Kakuro.Core.Models;

namespace Kakuro.Benchmarks;

[MemoryDiagnoser]
public class BoardBenchmarks
{
    // "The Official Book of Kakuro" #41
    private const string PuzzleText =
        "The Official Book of Kakuro|41|Easy|13,2,0,1,1|11,3,0,2,1|9,3,0,6,1|14,2,0,7,1|17,2,1,0,0|16,5,1,3,1|8,2,1,5,0|34,5,1,5,1|7,3,2,0,0|23,3,2,4,0|15,3,2,4,1|15,5,3,1,0|13,3,4,2,0|21,3,4,2,1|18,3,4,6,1|35,5,5,1,0|6,2,5,1,1|9,2,5,7,1|7,3,6,0,0|24,3,6,4,0|12,2,7,0,0|5,2,7,5,0";

    private Puzzle _puzzle = null!;
    private Board _board = null!;
    private Board.State _state = null!;
    private Engine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _puzzle = Puzzle.Parse(PuzzleText);
        _board = new Board(_puzzle);
        _state = _board.GetState();
        _engine = new Engine();
    }

    [Benchmark] public Board.State GetState() => _board.GetState();

    [Benchmark] public void SetState() => _board.SetState(_state);

    [Benchmark]
    public bool Solve() => _engine.Solve(_puzzle);
}
