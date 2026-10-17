using Kakuro.Core.Models;

namespace Kakuro.Core;

public sealed class Engine(Action<string>? log = null)
{
    public bool Solve(Puzzle puzzle)
    {
        Board board = new(puzzle);
        
        log?.Invoke("Solving puzzle...");
        
        bool solved = board.Solve(log);
        if (solved)
        {
            log?.Invoke(board.ToString());
            if (board.IsSolutionValid())
            {
                log?.Invoke("Solution is valid");
            }
            else
            {
                log?.Invoke("Solution is invalid");
            }
        }
        else
        {
            log?.Invoke("Unable to solve puzzle");
        }
        
        return solved;
    }
}