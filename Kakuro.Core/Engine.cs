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
        do
        {
            (string error, changed) = board.UpdateCellConstraints();
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