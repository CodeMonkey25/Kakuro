using Kakuro.Core.Extensions;

namespace Kakuro.Core.Tests.Extensions;

public class TimeSpanExtensionsTests
{
    [Fact]
    public void ReadableTime_WhenZero_ReturnsZeroMs()
    {
        Assert.Equal("0 ms", TimeSpan.Zero.ReadableTime());
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 1, "1ms")]
    [InlineData(0, 0, 0, 0, 999, "999ms")]
    [InlineData(0, 0, 0, 1, 0, "1s")]
    [InlineData(0, 0, 0, 59, 0, "59s")]
    [InlineData(0, 0, 1, 0, 0, "1m")]
    [InlineData(0, 0, 59, 0, 0, "59m")]
    [InlineData(0, 1, 0, 0, 0, "1h")]
    [InlineData(0, 23, 0, 0, 0, "23h")]
    [InlineData(1, 0, 0, 0, 0, "1d")]
    [InlineData(36, 0, 0, 0, 0, "36d")]
    public void ReadableTime_SingleUnit_FormatsOnlyThatUnit(int days, int hours, int minutes, int seconds, int milliseconds, string expected)
    {
        Assert.Equal(expected, new TimeSpan(days, hours, minutes, seconds, milliseconds).ReadableTime());
    }

    [Fact]
    public void ReadableTime_AllUnits_AreSeparatedBySpacesLargestFirst()
    {
        Assert.Equal("1d 2h 3m 4s 5ms", new TimeSpan(1, 2, 3, 4, 5).ReadableTime());
    }

    [Theory]
    [InlineData(1, 0, 5, 0, 0, "1d 5m")]
    [InlineData(0, 1, 0, 5, 0, "1h 5s")]
    [InlineData(0, 0, 1, 0, 7, "1m 7ms")]
    public void ReadableTime_ZeroUnitsInTheMiddle_AreOmitted(int days, int hours, int minutes, int seconds, int milliseconds, string expected)
    {
        Assert.Equal(expected, new TimeSpan(days, hours, minutes, seconds, milliseconds).ReadableTime());
    }

    [Fact]
    public void ReadableTime_RollsOverIntoLargerUnits()
    {
        Assert.Equal("1h 10m", TimeSpan.FromMinutes(70).ReadableTime());
        Assert.Equal("2d", TimeSpan.FromHours(48).ReadableTime());
        Assert.Equal("1s 500ms", TimeSpan.FromMilliseconds(1500).ReadableTime());
    }

    [Fact]
    public void ReadableTime_SubMillisecondRemainder_IsTruncated()
    {
        Assert.Equal("0 ms", TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond - 1).ReadableTime());
        Assert.Equal("1ms", TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * 3 / 2).ReadableTime());
    }

    [Fact]
    public void ReadableTime_MaxValue_DoesNotThrow()
    {
        Assert.Equal("10675199d 2h 48m 5s 477ms", TimeSpan.MaxValue.ReadableTime());
    }
}
