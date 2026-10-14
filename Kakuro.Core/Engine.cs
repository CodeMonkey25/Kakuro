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
        Board board = new(puzzle);
        
        Log?.Invoke("Solving puzzle...");
        
        bool solved = board.Solve();
        
        Log?.Invoke(board.ToString());

        if (solved)
        {
            Log?.Invoke(board.IsSolutionValid() ? "Solution is valid" : "Solution is invalid");
        }
        else
        {
            Log?.Invoke("Unable to solve puzzle");
        }
        
        return solved;
    }
}