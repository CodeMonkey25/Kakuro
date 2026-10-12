using Kakuro.Core.Models;

namespace Kakuro.Core;

public class Engine
{
    private Action<string>? Log { get; }

    public Engine(Action<string>? log = null)
    {
        Log = log;
    }

    public bool SolveLogically(Puzzle puzzle)
    {
        bool changed = false;
        Board board = new(puzzle);
        
        // instantiate buffers
        Span<int> numberBuffer = stackalloc int[9];
        Span<HashSet<int>> updatedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
        Span<int[]> currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => Array.Empty<int>()).ToArray();
        
        Log?.Invoke("Solving puzzle...");
        
        Log?.Invoke("Updating run constraints...");
        do
        {
            (string error, changed) = board.UpdateCellConstraints(numberBuffer, updatedCandidatesBuffer, currentCandidatesBuffer);
            if (error != "")
            {
                Log?.Invoke(error);
                return false;
            }
        } while (changed);

        Log?.Invoke(board.ToString());
        return board.IsSolved();
    }
}