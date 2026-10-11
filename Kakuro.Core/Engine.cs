using Kakuro.Core.Models;

namespace Kakuro.Core;

public class Engine
{
    private Action<string>? Log { get; }

    public Engine(Action<string>? log = null)
    {
        Log = log;
    }

    public bool Solve(Puzzle puzzle)
    {
        string error = "";
        Board board = new(puzzle);
        Log?.Invoke("Solving puzzle...");
        
        Log?.Invoke("Applying run constraints...");
        error = board.ApplyRunConstraints();
        if (error != "")
        {
            Log?.Invoke(error);
            return false;
        }

        Log?.Invoke(board.ToString());
        return board.IsSolved();
    }
}