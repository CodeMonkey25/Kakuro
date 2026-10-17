using System.Collections.Immutable;

namespace Kakuro.Core;

public static class Constants
{
    public const int UnsolvedCell = 0;
    public const int MinCandidate = 1;
    public const int MaxCandidate = 9;
    public static ImmutableArray<int> AllCandidates { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    public static ImmutableArray<int> MinPossibleSum { get; } = [0, 1, 3, 6, 10, 15, 21, 28, 36, 45];
    public static ImmutableArray<int> MaxPossibleSum { get; } = [0, 9, 17, 24, 30, 35, 39, 42, 44, 45];
}