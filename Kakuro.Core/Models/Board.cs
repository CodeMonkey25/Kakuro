using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Kakuro.Core.Interfaces;

namespace Kakuro.Core.Models;

public sealed class Board
{
    private Puzzle Puzzle { get; }
    private ImmutableArray<ICell> Cells { get; }
    public ImmutableArray<PlayableCell> PlayableCells { get; }
    public ImmutableArray<Run> Runs { get; }

    public Board(Puzzle puzzle)
    {
        Puzzle = puzzle;
        
        // prepare Cells array
        ICell[] cells = new ICell[Puzzle.Rows * Puzzle.Columns];
        Array.Fill(cells, EmptyCell.Instance);
        
        List<Run> runs = new(Puzzle.Runs.Length);
        foreach (Puzzle.Run puzzleRun in Puzzle.Runs)
        {
            // instantiate playable cells from puzzle runs
            List<PlayableCell> runCells = new(puzzleRun.Length);
            foreach ((int row, int column) in puzzleRun.GetCellCoordinates())
            {
                int cellsIndex = row * Puzzle.Columns + column;
                if (cells[cellsIndex] is not PlayableCell playableCell)
                {
                    playableCell = new PlayableCell(row, column);
                    cells[cellsIndex] = playableCell;
                }
                runCells.Add(playableCell);
            }
            
            // instantiate Board Runs
            Run run = new(puzzleRun, runCells.ToImmutableArray());
            runs.Add(run);
            
            // bind run cells together
            foreach (PlayableCell cell in runCells)
            {
                cell.BindCells(runCells);
                cell.CandidatesChanged += run.MarkDirty;
            }
        }
        
        Cells = cells.ToImmutableArray();
        PlayableCells = cells.OfType<PlayableCell>().ToImmutableArray();
        Runs = runs.ToImmutableArray();
    }
    
    public override string ToString()
    {
        string rowDivider = new('-', Puzzle.Columns * 2 + 1);
        StringBuilder sb = new();
        for (int r = 0; r < Puzzle.Rows; r++)
        {
            sb.AppendLine(rowDivider);
            for (int c = 0; c < Puzzle.Columns; c++)
            {
                sb.Append('|');
                sb.Append(Cells[r * Puzzle.Columns + c]);
            }
            sb.Append('|');
            sb.AppendLine();
        }
        sb.Append(rowDivider);
        return sb.ToString();
    }

    public bool IsSolved()
    {
        foreach (PlayableCell playableCell in PlayableCells)
        {
            if (!playableCell.IsSolved) return false;
        }
        return true;
    }

    public bool IsSolutionValid()
    {
        HashSet<int> seen = [];
        foreach (Run run in Runs)
        {
            seen.Clear();
            int sum = 0;
            foreach (PlayableCell playableCell in run.Cells)
            {
                if (!playableCell.IsSolved) return false;
                if (!seen.Add(playableCell.Value)) return false;
                
                sum += playableCell.Value;
            }
            if (sum != run.Total) return false;
        }

        return true;
    }
    
    public State GetState()
    {
        ICell.IState[] states = new ICell.IState[PlayableCells.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = PlayableCells[i].GetState();
        }
        return new State(ImmutableCollectionsMarshal.AsImmutableArray(states));
    }

    public void SetState(State state)
    {
        if (state is null || state.CellStates.IsDefault || state.CellStates.Length != PlayableCells.Length)
            throw new ArgumentException("Invalid state");
        
        for (int i = 0; i < PlayableCells.Length; i++)
        {
            if (!PlayableCells[i].IsValidState(state.CellStates[i]))
                throw new ArgumentException("Invalid state");
        }

        for (int i = 0; i < PlayableCells.Length; i++)
        {
            PlayableCells[i].SetState(state.CellStates[i]);
        }
    }

    public sealed class Run
    {
        private Puzzle.Run PuzzleRun { get; }
        public ImmutableArray<PlayableCell> Cells { get; }
        public int Length => PuzzleRun.Length;
        public int Total => PuzzleRun.Total;
        public bool IsDirty { get; private set; }
        public bool IsSolved
        {
            get
            {
                int total = 0;
                foreach (PlayableCell cell in Cells)
                {
                    if (!cell.IsSolved) return false;
                    total += cell.Value;
                }
                return total == Total;
            }
        }

        public Run(Puzzle.Run puzzleRun, ImmutableArray<PlayableCell> cells)
        {
            PuzzleRun = puzzleRun;
            Cells = cells;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
        
        public void ClearDirty()
        {
            IsDirty = false;
        }
    }
    
    public sealed record State(ImmutableArray<ICell.IState> CellStates)
    {
        public bool Equals(State? other) =>
            other is not null && (CellStates.IsDefault || other.CellStates.IsDefault
                ? CellStates.IsDefault && other.CellStates.IsDefault
                : CellStates.AsSpan().SequenceEqual(other.CellStates.AsSpan()));

        public override int GetHashCode()
        {
            HashCode hash = new();
            if (!CellStates.IsDefault)
                foreach (ICell.IState s in CellStates) hash.Add(s);
            return hash.ToHashCode();
        }
    }

}
