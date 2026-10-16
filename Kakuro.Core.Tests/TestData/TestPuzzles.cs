using System.Collections.Immutable;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.TestData;

/// <summary>
/// Shared puzzle fixtures and helpers. Coordinates: a run's (row, column) is its hint cell; its cells follow it.
/// </summary>
internal static class TestPuzzles
{
    /// <summary>3x4 grid; four runs, two of which share cells.</summary>
    public const string Sample = "kakuro.com|1|Easy|16,2,0,1,0|12,2,0,2,1|3,2,1,0,0|7,2,2,0,0";

    /// <summary>
    /// Horizontal 3 over (1,1),(1,2); vertical 4 over (1,1),(2,1). Propagation alone solves it:
    /// <code>
    /// X X X
    /// X 1 2
    /// X 3 X
    /// </code>
    /// </summary>
    public const string ForcedCorner = "test|1|Easy|3,2,1,0,0|4,2,0,1,1";

    /// <summary>Same cell (1,1) must be in {1,2} (row 3) and {8,9} (column 17): propagation alone fails.</summary>
    public const string Contradictory = "test|1|Easy|3,2,1,0,0|17,2,0,1,1";

    /// <summary>Two unrelated single-cell runs, both 9.</summary>
    public const string TwoSingleCells = "test|1|Easy|9,1,1,1,0|9,1,1,1,1";

    /// <summary>2x2 block solved by propagation alone: 1 2 / 8 9.</summary>
    public const string ForcedBlock = "test|1|Easy|3,2,1,0,0|17,2,2,0,0|9,2,0,1,1|11,2,0,2,1";

    /// <summary>2x2 block with a single solution, 9 7 / 8 9.</summary>
    public const string UniqueBlock = "test|1|Easy|16,2,1,0,0|17,2,2,0,0|17,2,0,1,1|16,2,0,2,1";

    /// <summary>2x2 block where every run totals 10: propagation can't pick a value, guessing is required.</summary>
    public const string GuessingBlock = "test|1|Easy|10,2,1,0,0|10,2,2,0,0|10,2,0,1,1|10,2,0,2,1";

    /// <summary>3x3 block whose row totals (46) differ from its column totals (50); only guessing exposes the contradiction.</summary>
    public const string UnsolvableAfterGuessing = "test|1|Easy|19,3,1,0,0|13,3,2,0,0|14,3,3,0,0|18,3,0,1,1|18,3,0,2,1|14,3,0,3,1";

    public const string Official41 = "The Official Book of Kakuro|41|Easy|13,2,0,1,1|11,3,0,2,1|9,3,0,6,1|14,2,0,7,1|17,2,1,0,0|16,5,1,3,1|8,2,1,5,0|34,5,1,5,1|7,3,2,0,0|23,3,2,4,0|15,3,2,4,1|15,5,3,1,0|13,3,4,2,0|21,3,4,2,1|18,3,4,6,1|35,5,5,1,0|6,2,5,1,1|9,2,5,7,1|7,3,6,0,0|24,3,6,4,0|12,2,7,0,0|5,2,7,5,0";

    /// <summary>A puzzle with a single horizontal run of the given total and length.</summary>
    public static string SingleRun(int total, int length) => $"test|1|Easy|{total},{length},0,0,0";

    /// <summary>Every non-blank line of the shared puzzles.txt (copied next to the test assembly).</summary>
    public static TheoryData<string> FromFile()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "puzzles.txt");
        TheoryData<string> data = [];
        foreach (string line in File.ReadLines(path).Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            data.Add(line);
        }
        return data;
    }

    /// <summary>
    /// Renders the expected <c>Board.ToString()</c> from one string per row; each character is a cell
    /// ('X' = empty cell, ' ' = unsolved playable cell, a digit = solved cell).
    /// </summary>
    public static string Grid(params string[] rows)
    {
        string divider = new('-', rows[0].Length * 2 + 1);
        List<string> lines = [];
        foreach (string row in rows)
        {
            lines.Add(divider);
            lines.Add("|" + string.Join('|', row.ToCharArray()) + "|");
        }
        lines.Add(divider);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Builds a board state in which the playable cells (in grid order) hold the given values;
    /// a 0 leaves that cell as it currently is. Lets tests construct arbitrary (even invalid) solutions.
    /// </summary>
    public static Board.State WithPlayableValues(Board board, params int[] values)
    {
        ImmutableArray<Cell.State>.Builder cells = board.GetState().CellStates.ToBuilder();
        Assert.Equal(values.Length, cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            if (values[i] == 0) continue;

            PlayableCell cell = new(0, 0);
            Assert.Null(cell.Solve(values[i]));
            cells[i] = cell.GetState();
        }
        return new Board.State(cells.ToImmutable());
    }
}
