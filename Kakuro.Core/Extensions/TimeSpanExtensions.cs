namespace Kakuro.Core.Extensions;

public static class TimeSpanExtensions
{
    public static string ReadableTime(this TimeSpan t)
    {
        List<string> parts = new();

        void Add(int val, string unit)
        {
            if (val > 0) parts.Add(val + unit);
        }

        Add(t.Days, "d");
        Add(t.Hours, "h");
        Add(t.Minutes, "m");
        Add(t.Seconds, "s");
        Add(t.Milliseconds, "ms");

        if (!parts.Any()) parts.Add("0 ms");

        return string.Join(" ", parts);
    }
}
