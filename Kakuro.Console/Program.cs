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
        Puzzle[] puzzles = [];

        TimeSpan loadTime = TimeIt(() =>
        {
            puzzles = File.ReadLines("puzzles.txt")
                .Select(static p => p.Trim())
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Select(Puzzle.Parse)
                .ToArray();
        });
        System.Console.WriteLine($"Loaded {puzzles.Length} puzzle(s) in {loadTime.ReadableTime()}");
        
        List<string> log = new();

        // run an initial solve to load static classes and JIT the code
        loadTime = TimeIt(() =>
        {
            // line 19 has a lot of passes and guesses - 2, 46, 48, & 57 are slow
            new Engine(log.Add).Solve(puzzles[19]);
        });
        System.Console.WriteLine(string.Join("\n", log));
        System.Console.WriteLine($"Initial run: {loadTime.ReadableTime()}");
        
        int i = 0;
        Engine engine = new(log.Add);
        List<(int line, TimeSpan time, int goodGuesses, int badGuesses)> timings = new();
        foreach (Puzzle puzzle in puzzles)
        {
            log.Clear();
            
            int line = i++;
            TimeSpan time = TimeIt(() => engine.Solve(puzzle));
            int goodGuesses = 0;
            int badGuesses = 0;

            string? guessLine = log.FirstOrDefault(static m => m.StartsWith("Solved in "));
            if (guessLine != null)
            {
                string[] parts = guessLine.Split(' ');
                goodGuesses = int.Parse(parts[2]);
                badGuesses = int.Parse(parts[6]);
            }
            
            timings.Add((line, time, goodGuesses, badGuesses));
        }
        
        System.Console.WriteLine("\tGood\tBad");
        System.Console.WriteLine("Line\tGuesses\tGuesses\tTime");
        foreach ((int line, TimeSpan time, int goodGuesses, int badGuesses) in timings)
        {
            string good = goodGuesses == 0 ? "" : goodGuesses.ToString();
            string bad = badGuesses == 0 ? "" : badGuesses.ToString();
            System.Console.WriteLine($"{line}\t{good}\t{bad}\t{time.ReadableTime()}");
        }
    }
    
    private static TimeSpan TimeIt(Action action)
    {
        long startTime = Stopwatch.GetTimestamp();
        action.Invoke();
        return Stopwatch.GetElapsedTime(startTime);
    }
}