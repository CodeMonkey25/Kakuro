using System.Diagnostics;
using Kakuro.Core.Extensions;
using Kakuro.Core.Models;

namespace Kakuro.Console;

class Program
{
    static void Main(string[] args)
    {
        string timing = TimeIt(Run);
        System.Console.WriteLine(timing);
    }
    
    private static void Run()
    {
        Puzzle puzzle = File.ReadLines("puzzles.txt")
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Skip(1)
            .Take(1)
            .Select(Puzzle.Parse)
            .First();
        
        System.Console.WriteLine(puzzle);
        
        System.Console.WriteLine();
        System.Console.WriteLine(string.Join(", ", puzzle.Runs.First()));
        System.Console.WriteLine(string.Join(", ", puzzle.Runs.First().Candidates));
        
        System.Console.WriteLine();
        System.Console.WriteLine(puzzle.Rows + ", " + puzzle.Columns);
        Board board = new(puzzle);
        System.Console.WriteLine(board);
    }
    
    private static string TimeIt(Action action, int count = 1)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (count > 0)
        {
            action.Invoke();
            --count;
        }

        watch.Stop();
        return $"Total time: {watch.Elapsed.ReadableTime()}";
    }

}