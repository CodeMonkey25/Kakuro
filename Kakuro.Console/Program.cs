using System.Diagnostics;
using Kakuro.Core;
using Kakuro.Core.Extensions;
using Kakuro.Core.Models;

namespace Kakuro.Console;

class Program
{
    static void Main(string[] args)
    {
        TimeSpan timing = TimeIt(Run);
        System.Console.WriteLine($"Total time: {timing.ReadableTime()}");
    }
    
    private static void Run()
    {
        Puzzle[] puzzles = File.ReadLines("puzzles.txt")
            .Select(static p => p.Trim())
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            // .Skip(19) // 19 has a lot of passes and guesses - 46 & 57 are slow
            // .Take(1)
            .Select(Puzzle.Parse)
            .ToArray();
        
        // run an initial solve to load static classes and JIT the code
        new Engine(System.Console.WriteLine).Solve(puzzles.First());
        
        int i = 0;
        List<string> log = new();
        Engine engine = new(log.Add);
        List<(int line, TimeSpan time, int guesses)> timings = new();
        foreach (Puzzle puzzle in puzzles)
        {
            log.Clear();
            
            int line = i++;
            TimeSpan time = TimeIt(() => engine.Solve(puzzle));
            int guesses = 0;
            
            if (log.Any(m => m.StartsWith("Solved in ")))
            {
                guesses = int.Parse(log.First(m => m.StartsWith("Solved in ")).Split(' ')[2]);
            }
            
            timings.Add((line, time, guesses));
        }
        
        System.Console.WriteLine("Line\tGuesses\tTime");
        foreach ((int line, TimeSpan time, int guesses) in timings)
        {
            System.Console.WriteLine($"{line}\t{(guesses == 0 ? "" : guesses.ToString())}\t{time.ReadableTime()}");
        }
    }
    
    private static TimeSpan TimeIt(Action action)
    {
        long startTime = Stopwatch.GetTimestamp();
        action.Invoke();
        return Stopwatch.GetElapsedTime(startTime);
    }
}