using System.Text;
using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class BoardTests
{
    // A 3x3 puzzle with intersecting runs:
    // (0,1) -> horiz len 2: (0,2), (0,3) (Total 16)
    // (0,2) -> vert len 2: (1,2), (2,2) (Total 12)
    // (1,0) -> horiz len 2: (1,1), (1,2) (Total 3)
    // (2,0) -> horiz len 2: (2,1), (2,2) (Total 7)
    private const string SamplePuzzleString = "kakuro.com|1|Easy|16,2,0,1,0|12,2,0,2,1|3,2,1,0,0|7,2,2,0,0";

    [Fact]
    public void Constructor_InstantiatesGridAndSharesIntersectingCells()
    {
        Puzzle puzzle = Puzzle.Parse(SamplePuzzleString);
        Board board = new(puzzle);

        // Verify the board initially is not solved
        Assert.False(board.IsSolved());

        // Check ToString format initially contains expected dividers and 'X' for non-cells
        string boardString = board.ToString();
        Assert.Contains("-------", boardString);
        Assert.Contains("|X|", boardString);
    }

    [Fact]
    public void IsSolved_ReturnsFalseWhenAnyCellIsUnsolved_AndTrueWhenAllSolved()
    {
        // 2x2 board: 1 horizontal run (1,1 len 1 -> (1,1)) and 1 vertical run (1,1 len 1 -> (1,1))
        // Run with Total 9, Length 1 -> Candidates {9}, Exclusions {1..8}
        Puzzle singleCellPuzzle = Puzzle.Parse("test|1|Easy|9,1,1,1,0|9,1,1,1,1");
        Board board = new(singleCellPuzzle);

        Assert.False(board.IsSolved());

        (string error, bool changed) = UpdateCellConstraints(board);

        Assert.Equal("", error);
        Assert.True(changed);
        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
        Assert.Contains("|9|", board.ToString());
    }
    
    [Fact]
    public void IsSolved_ReturnsFalseWhenAnyCellIsUnsolved_AndTrueWhenAllSolved2()
    {
        // 2x2 block of cells at rows 1-2, columns 1-2, with clue cells in row 0 and column 0
        Puzzle singleCellPuzzle = Puzzle.Parse("test|1|Easy|16,2,1,0,0|17,2,2,0,0|17,2,0,1,1|16,2,0,2,1");
        Board board = new(singleCellPuzzle);

        Assert.False(board.IsSolved());

        (string error, bool changed) = UpdateCellConstraints(board);

        Assert.Equal("", error);
        Assert.True(changed);
        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());

        string expected = new StringBuilder()
            .AppendLine("-------")
            .AppendLine("|X|X|X|")
            .AppendLine("-------")
            .AppendLine("|X|9|7|")
            .AppendLine("-------")
            .AppendLine("|X|8|9|")
            .AppendLine("-------")
            .ToString();

        Assert.Equal(expected, board.ToString());
    }

    [Fact]
    public void IsSolutionValid_WhenUnsolved_ReturnsFalse()
    {
        Puzzle puzzle = Puzzle.Parse(SamplePuzzleString);
        Board board = new(puzzle);

        Assert.False(board.IsSolved());
        Assert.False(board.IsSolutionValid());
    }

    [Fact]
    public void IsSolutionValid_WhenSolvedWithCorrectTotals_ReturnsTrue()
    {
        // 2x2 board: 1 horizontal run (1,1 len 1 -> (1,1)) and 1 vertical run (1,1 len 1 -> (1,1))
        Puzzle singleCellPuzzle = Puzzle.Parse("test|1|Easy|9,1,1,1,0|9,1,1,1,1");
        Board board = new(singleCellPuzzle);

        (string error, bool changed) = UpdateCellConstraints(board);

        Assert.Equal("", error);
        Assert.True(changed);
        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    [Fact]
    public void ToString_FormatsGridWithDividersAndCells()
    {
        // Puzzle with 2 rows, 2 columns: Cell at (1,1) after the clue cell at (1,0) and (0,1)
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|4,1,1,0,0|4,1,0,1,1");
        Board board = new(puzzle);

        string expectedUnsolved = new StringBuilder()
            .AppendLine("-----")
            .AppendLine("|X|X|")
            .AppendLine("-----")
            .AppendLine("|X| |")
            .AppendLine("-----")
            .ToString();

        Assert.Equal(expectedUnsolved, board.ToString());
    }

    [Fact]
    public void UpdateCellConstraints_AppliesExclusionsAcrossRuns()
    {
        // Puzzle #1 from calendar/official dataset
        string officialPuzzle = "The Official Book of Kakuro|41|Easy|13,2,0,1,1|11,3,0,2,1|9,3,0,6,1|14,2,0,7,1|17,2,1,0,0|16,5,1,3,1|8,2,1,5,0|34,5,1,5,1|7,3,2,0,0|23,3,2,4,0|15,3,2,4,1|15,5,3,1,0|13,3,4,2,0|21,3,4,2,1|18,3,4,6,1|35,5,5,1,0|6,2,5,1,1|9,2,5,7,1|7,3,6,0,0|24,3,6,4,0|12,2,7,0,0|5,2,7,5,0";
        Puzzle puzzle = Puzzle.Parse(officialPuzzle);
        Board board = new(puzzle);

        (string result, bool changed) = UpdateCellConstraints(board);

        Assert.Equal("", result);
        Assert.True(changed);
    }

    [Fact]
    public void UpdateCellConstraints_WhenRunForcesNakedSingle_SolvesCell()
    {
        // Run with Total 3, Length 2 has candidates {1, 2} and exclusions {3, 4, 5, 6, 7, 8, 9}
        // Run with Total 4, Length 2 has candidates {1, 3} and exclusions {2, 4, 5, 6, 7, 8, 9}
        // When these two runs intersect on a cell, exclusions {2..9} and {2,4..9} leave only digit 1.
        // Therefore, the intersecting cell should automatically solve to 1.
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,0,1,1");
        Board board = new(puzzle);

        (string error, bool changed) = UpdateCellConstraints(board);

        Assert.Equal("", error);
        Assert.True(changed);
        string boardDisplay = board.ToString();
        Assert.Contains("|1|", boardDisplay);
    }

    [Fact]
    public void UpdateCellConstraints_ContradictoryIntersectingRuns_ReturnsError()
    {
        // Horizontal run with Total 3, Length 2 -> candidates {1, 2}, exclusions {3..9}
        // Vertical run with Total 17, Length 2 -> candidates {8, 9}, exclusions {1..7}
        // Intersecting at (1, 1), union of exclusions is {1..9}, leaving no valid candidate.
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|17,2,0,1,1");
        Board board = new(puzzle);

        (string error, bool changed) = UpdateCellConstraints(board);

        Assert.Contains("has no candidates", error);
        Assert.False(changed);
    }

    [Fact]
    public void UpdateCellConstraints_WhenAlreadyAtFixedPoint_ReturnsChangedFalse()
    {
        Puzzle puzzle = Puzzle.Parse("test|1|Easy|3,2,1,0,0|4,2,0,1,1");
        Board board = new(puzzle);

        Span<int> numberBuffer = stackalloc int[9];
        Span<HashSet<int>> updatedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
        Span<int[]> currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => Array.Empty<int>()).ToArray();

        var (firstError, firstChanged) = board.UpdateCellConstraints(numberBuffer, updatedCandidatesBuffer, currentCandidatesBuffer);
        Assert.Equal("", firstError);
        Assert.True(firstChanged);

        var (secondError, secondChanged) = board.UpdateCellConstraints(numberBuffer, updatedCandidatesBuffer, currentCandidatesBuffer);
        Assert.Equal("", secondError);
        Assert.False(secondChanged);
    }

    [Fact]
    public void UpdateCellConstraints_ValidDatasetPuzzle_IterativelySolves()
    {
        string officialPuzzle = "The Official Book of Kakuro|41|Easy|13,2,0,1,1|11,3,0,2,1|9,3,0,6,1|14,2,0,7,1|17,2,1,0,0|16,5,1,3,1|8,2,1,5,0|34,5,1,5,1|7,3,2,0,0|23,3,2,4,0|15,3,2,4,1|15,5,3,1,0|13,3,4,2,0|21,3,4,2,1|18,3,4,6,1|35,5,5,1,0|6,2,5,1,1|9,2,5,7,1|7,3,6,0,0|24,3,6,4,0|12,2,7,0,0|5,2,7,5,0";
        Puzzle puzzle = Puzzle.Parse(officialPuzzle);
        Board board = new(puzzle);

        Span<int> numberBuffer = stackalloc int[9];
        Span<HashSet<int>> updatedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
        Span<int[]> currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => Array.Empty<int>()).ToArray();

        string error = "";
        bool changed;
        do
        {
            (error, changed) = board.UpdateCellConstraints(numberBuffer, updatedCandidatesBuffer, currentCandidatesBuffer);
            Assert.Equal("", error);
        } while (changed);

        Assert.True(board.IsSolved());
        Assert.True(board.IsSolutionValid());
    }

    [Theory]
    [InlineData("test|1|Easy|3,2,0,1,1|4,2,4,0,0")] // Horizontal run on row 4 exceeds vertical run extent (row 2)
    [InlineData("test|2|Easy|4,2,0,0,0|3,2,0,4,1")] // Vertical run on column 4 exceeds horizontal run extent (column 2)
    [InlineData("test|3|Easy|4,2,2,0,0")]             // Only horizontal runs (Rows defaults to 1, but row 2 is needed)
    [InlineData("test|4|Easy|4,2,0,2,1")]             // Only vertical runs (Columns defaults to 1, but column 2 is needed)
    public void Constructor_WhenRunCoordinatesExceedOppositeOrientationDimensions_InstantiatesBoardSuccessfully(string puzzleString)
    {
        Puzzle puzzle = Puzzle.Parse(puzzleString);

        // Verifies that board dimensions correctly account for both orientations and edge cases (e.g. single-orientation puzzles),
        // preventing IndexOutOfRangeException during cell instantiation.
        Board board = new(puzzle);

        Assert.NotNull(board);
        Assert.False(string.IsNullOrEmpty(board.ToString()));
    }

    private static (string error, bool changed) UpdateCellConstraints(Board board)
    {
        Span<int> numberBuffer = stackalloc int[9];
        Span<HashSet<int>> updatedCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
        Span<int[]> currentCandidatesBuffer = Enumerable.Range(0, 9).Select(_ => Array.Empty<int>()).ToArray();
        return board.UpdateCellConstraints(numberBuffer, updatedCandidatesBuffer, currentCandidatesBuffer);
    }
}
