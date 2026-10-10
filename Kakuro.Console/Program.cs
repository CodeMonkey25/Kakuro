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
        Puzzle[] puzzles = File.ReadLines("puzzles.txt")
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Puzzle.Parse)
            .ToArray();
        
        // foreach (Puzzle puzzle in puzzles)
        // {
        //     System.Console.WriteLine(puzzle);
        // }
        //
        // System.Console.WriteLine();
        // System.Console.WriteLine(string.Join(", ", puzzles.First().Runs.First()));
        // System.Console.WriteLine(string.Join(", ", puzzles.First().Runs.First().Candidates));
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