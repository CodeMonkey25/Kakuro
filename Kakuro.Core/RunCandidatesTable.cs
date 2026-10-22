using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Kakuro.Core;

public static class RunCandidatesTable
{
    internal static FrozenDictionary<(int length, int total), ImmutableArray<int>> InclusionEntries { get; } =
        new Dictionary<(int length, int total), ImmutableArray<int>>()
        {
            { (1, 1), [1] },
            { (1, 2), [2] },
            { (1, 3), [3] },
            { (1, 4), [4] },
            { (1, 5), [5] },
            { (1, 6), [6] },
            { (1, 7), [7] },
            { (1, 8), [8] },
            { (1, 9), [9] },

            { (2, 3), [1, 2] },
            { (2, 4), [1, 3] },
            { (2, 16), [7, 9] },
            { (2, 17), [8, 9] },

            { (3, 6), [1, 2, 3] },
            { (3, 7), [1, 2, 4] },
            { (3, 23), [6, 8, 9] },
            { (3, 24), [7, 8, 9] },

            { (4, 10), [1, 2, 3, 4] },
            { (4, 11), [1, 2, 3, 5] },
            { (4, 29), [5, 7, 8, 9] },
            { (4, 30), [6, 7, 8, 9] },

            { (5, 15), [1, 2, 3, 4, 5] },
            { (5, 16), [1, 2, 3, 4, 6] },
            { (5, 34), [4, 6, 7, 8, 9] },
            { (5, 35), [5, 6, 7, 8, 9] },

            { (6, 21), [1, 2, 3, 4, 5, 6] },
            { (6, 22), [1, 2, 3, 4, 5, 7] },
            { (6, 38), [3, 5, 6, 7, 8, 9] },
            { (6, 39), [4, 5, 6, 7, 8, 9] },

            { (7, 28), [1, 2, 3, 4, 5, 6, 7] },
            { (7, 29), [1, 2, 3, 4, 5, 6, 8] },
            { (7, 41), [2, 4, 5, 6, 7, 8, 9] },
            { (7, 42), [3, 4, 5, 6, 7, 8, 9] },

            { (8, 36), [1, 2, 3, 4, 5, 6, 7, 8] },
            { (8, 37), [1, 2, 3, 4, 5, 6, 7, 9] },
            { (8, 38), [1, 2, 3, 4, 5, 6, 8, 9] },
            { (8, 39), [1, 2, 3, 4, 5, 7, 8, 9] },
            { (8, 40), [1, 2, 3, 4, 6, 7, 8, 9] },
            { (8, 41), [1, 2, 3, 5, 6, 7, 8, 9] },
            { (8, 42), [1, 2, 4, 5, 6, 7, 8, 9] },
            { (8, 43), [1, 3, 4, 5, 6, 7, 8, 9] },
            { (8, 44), [2, 3, 4, 5, 6, 7, 8, 9] },

            { (9, 45), Constants.AllCandidates },
        }.ToFrozenDictionary();

    internal static FrozenDictionary<(int length, int total), ImmutableArray<int>> ExclusionEntries { get; } =
        InclusionEntries
            .Select(e => KeyValuePair.Create(e.Key, Constants.AllCandidates.Except(e.Value).ToImmutableArray()))
            .ToFrozenDictionary();

    // [length - MinRunLength, total - MinRunTotal] -> exclusion mask; array indexing keeps the solver's hot-path lookup cheap
    private static readonly uint[,] ExclusionMasks =
        ExclusionEntries
            .Aggregate(
                new uint[Constants.MaxRunLength - Constants.MinRunLength + 1, Constants.MaxRunTotal - Constants.MinRunTotal + 1],
                (masks, entry) =>
                {
                    int length = entry.Key.length - Constants.MinRunLength;
                    int total = entry.Key.total - Constants.MinRunTotal;
                    masks[length, total] = entry.Value.Aggregate(0u, (mask, c) => mask | (1u << (c - Constants.MinCandidate)));
                    return masks;
                });

    public static uint GetRunExclusionMask(int length, int total)
    {
        if (length < Constants.MinRunLength || length > Constants.MaxRunLength) return 0;
        if (total < Constants.MinRunTotal || total > Constants.MaxRunTotal) return 0;
        return ExclusionMasks[length - Constants.MinRunLength, total - Constants.MinRunTotal];
    }

    public static ImmutableArray<int> GetRunInclusions(int length, int total) =>
        InclusionEntries.GetValueOrDefault((length, total), ImmutableArray<int>.Empty);

    public static ImmutableArray<int> GetRunExclusions(int length, int total) =>
        ExclusionEntries.GetValueOrDefault((length, total), ImmutableArray<int>.Empty);
}