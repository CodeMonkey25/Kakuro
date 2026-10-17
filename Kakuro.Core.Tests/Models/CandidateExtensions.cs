using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

internal static class CandidateExtensions
{
    public static int[] ToArray(this CandidateCollection collection)
    {
        int[] result = new int[collection.Count];
        collection.CopyTo(result);
        return result;
    }

    public static int[] CandidatesArray(this PlayableCell cell)
    {
        int[] result = new int[cell.CandidateCount];
        cell.CopyCandidatesTo(result);
        return result;
    }
}
