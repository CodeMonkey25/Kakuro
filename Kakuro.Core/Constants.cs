using System.Collections.Immutable;
using Kakuro.Core.Helpers;

namespace Kakuro.Core;

public static class Constants
{
    public const int MinCandidate = 1;
    public const int MaxCandidate = 9;
    public const int UnsolvedCell = MinCandidate - 1;
    public const int MinRunLength = 1;
    public const int MaxRunLength = MaxCandidate - MinCandidate + 1;
    public const int MinRunTotal = MinCandidate;
    public const int MaxRunTotal = (MinCandidate + MaxCandidate) * MaxRunLength / 2;
    public const string LogIndent = "   ";

    public static readonly ImmutableArray<int> AllCandidates =
        Enumerable.Range(MinCandidate, MaxRunLength)
            .ToImmutableArray();

    public static readonly ImmutableArray<int> MinPossibleSum =
        Enumerable.Range(0, MaxRunLength + 1)
            .Select(i => AllCandidates.Take(i).Sum())
            .ToImmutableArray();

    public static readonly ImmutableArray<int> MaxPossibleSum =
        Enumerable.Range(0, MaxRunLength + 1)
            .Select(i => AllCandidates.TakeLast(i).Sum())
            .ToImmutableArray();

    public static readonly uint AllCandidatesMask = MaskHelper.MaskForCandidates(AllCandidates);
}
