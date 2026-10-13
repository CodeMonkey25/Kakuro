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
        
        string? error = SolveLogically(board);
        if (error != null)
        {
            Log?.Invoke(error);
            return false;
        }

        if (!board.IsSolved())
        {
            SolveViaBruteForce(board);
        }
        
        Log?.Invoke(board.ToString());

        if (board.IsSolved())
        {
            Log?.Invoke(board.IsSolutionValid() ? "Solution is valid" : "Solution is invalid");
        }
        else
        {
            Log?.Invoke("Unable to solve puzzle");
        }
        
        return board.IsSolved();
    }

    private string? SolveLogically(Board board)
    {
        bool changed;
        Log?.Invoke("Updating run constraints...");
        int pass = 0;
        do
        {
            Log?.Invoke($"\tPass {++pass}...");
            (string? error, changed) = board.UpdateCellConstraints();
            if (error != null) return error;
        } while (changed);

        return null;
    }
    
    private bool SolveViaBruteForce(Board board)
    {
        return board.BruteForceCells();
    }
}