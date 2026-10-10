using Kakuro.Core.Models;

namespace Kakuro.Console;

class Program
{
    static void Main(string[] args)
    {
        Puzzle[] puzzles = File.ReadLines("puzzles.txt")
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Puzzle.Parse)
            .ToArray();
        
        foreach (Puzzle puzzle in puzzles)
        {
            System.Console.WriteLine(puzzle);
        }
    }
}