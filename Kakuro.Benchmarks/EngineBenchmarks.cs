using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Kakuro.Core;
using Kakuro.Core.Models;

namespace Kakuro.Benchmarks;

[MemoryDiagnoser]
public class EngineBenchmarks
{
    // 1-based line numbers in Kakuro.Console/puzzles.txt
    // 1 = easy/small, 5 & 20 = hard, 19 = many passes, 48 & 57 = slow per Console/Program.cs notes
    [Params(1, 5, 19, 20, 48, 49, 57)]
    public int Line { get; set; }

    private Puzzle _puzzle = null!;
    private Engine _engine = null!;

    [GlobalSetup]
    public void Setup()
    {
        string path = Path.GetFullPath(Path.Combine(SourceDir(), "..", "Kakuro.Console", "puzzles.txt"));
        string text = File.ReadLines(path)
            .Select(static p => p.Trim())
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Skip(Line - 1)
            .First();
        _puzzle = Puzzle.Parse(text);
        _engine = new Engine();
    }

    private static string SourceDir([CallerFilePath] string sourcePath = "") => Path.GetDirectoryName(sourcePath)!;

    [Benchmark]
    public bool Solve()
    {
        bool solved = _engine.Solve(_puzzle);
        if (!solved) throw new InvalidOperationException($"Line {Line} not solved correctly");
        return solved;
    }
}
