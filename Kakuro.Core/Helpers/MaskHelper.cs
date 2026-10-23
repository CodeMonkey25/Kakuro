namespace Kakuro.Core.Helpers;

public static class MaskHelper
{
    public static uint BitFor(int value) => 1u << value;

    public static uint BitForCandidate(int candidate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(candidate, Constants.MinCandidate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidate, Constants.MaxCandidate);
        return BitFor(candidate - Constants.MinCandidate);
    }

    public static uint MaskFor(IEnumerable<int> values)
    {
        uint mask = 0;
        foreach (int value in values)
        {
            mask |= BitFor(value);
        }
        return mask;
    }

    public static uint MaskForCandidates(IEnumerable<int> candidates)
    {
        uint mask = 0;
        foreach (int candidate in candidates)
        {
            mask |= BitForCandidate(candidate);
        }
        return mask;
    }
}