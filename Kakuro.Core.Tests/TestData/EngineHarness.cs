using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.TestData;

/// <summary>Runs <see cref="Engine"/> against a puzzle string and captures its log for assertions.</summary>
internal static class EngineHarness
{
    public const string ValidMessage = "Solution is valid";
    public const string InvalidMessage = "Solution is invalid";
    public const string FailureMessage = "Unable to solve puzzle";

    public static Outcome Solve(string puzzleText)
    {
        Puzzle puzzle = Puzzle.Parse(puzzleText);
        List<string> log = [];
        bool solved = new Engine(log.Add).Solve(puzzle);
        return new Outcome(puzzle, solved, log);
    }

    public sealed record Outcome(Puzzle Puzzle, bool Solved, List<string> Log)
    {
        /// <summary>The solved board, which the engine logs immediately before its validity message.</summary>
        public string SolvedBoard
        {
            get
            {
                int validity = Log.IndexOf(ValidMessage);
                Assert.True(validity > 0, "no solved board was logged");
                return Log[validity - 1];
            }
        }

        public bool Logged(string fragment) => Log.Any(message => message.Contains(fragment));

        public int Count(string fragment) => Log.Count(message => message.Contains(fragment));

        /// <summary>
        /// Checks the solved grid against the puzzle's runs directly (every cell filled, digits distinct, sum equal to the total),
        /// independent of <see cref="Board.IsSolutionValid"/>, which the engine itself relies on to report success.
        /// </summary>
        public void AssertSolvedGridSatisfiesPuzzle()
        {
            Assert.True(Solved);
            string[] lines = SolvedBoard.Split(Environment.NewLine);

            foreach (Puzzle.Run run in Puzzle.Runs)
            {
                List<int> digits = [];
                foreach ((int row, int column) in run.GetCellCoordinates())
                {
                    char cell = lines[row * 2 + 1][column * 2 + 1];
                    Assert.True(char.IsDigit(cell) && cell != '0', $"cell ({row}, {column}) of run {run} is '{cell}', not a digit");
                    digits.Add(cell - '0');
                }

                Assert.Equal(run.Length, digits.Distinct().Count());
                Assert.Equal(run.Total, digits.Sum());
            }
        }
    }
}
