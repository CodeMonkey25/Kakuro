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
        
        Log?.Invoke("Solving puzzle...");
        
        Log?.Invoke("Updating run constraints...");
        int pass = 0;
        do
        {
            Log?.Invoke($"\tPass {++pass}...");
            (string? error, changed) = board.UpdateCellConstraints();
            if (error != null)
            {
                Log?.Invoke(error);
                return false;
            }
        } while (changed);

        Log?.Invoke(board.ToString());

        if (board.IsSolved())
        {
            Log?.Invoke(board.IsSolutionValid() ? "Solution is valid" : "Solution is invalid");
        }
        else
        {
            Log?.Invoke("Unable to solve puzzle logically");
        }
        
        return board.IsSolved();
    }
}