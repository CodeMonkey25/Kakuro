using Kakuro.Core.Models;

namespace Kakuro.Core.Tests.Models;

public class EmptyCellTests
{
    [Fact]
    public void Instance_ReturnsSameSingletonInstance()
    {
        Assert.Same(EmptyCell.Instance, EmptyCell.Instance);
    }

    [Fact]
    public void ToString_ReturnsX()
    {
        Assert.Equal("X", EmptyCell.Instance.ToString());
    }

    [Fact]
    public void GetState_ReturnsEqualStatesEveryTime()
    {
        Assert.Equal(EmptyCell.Instance.GetState(), EmptyCell.Instance.GetState());
        Assert.Equal(EmptyCell.Instance.GetState().GetHashCode(), EmptyCell.Instance.GetState().GetHashCode());
    }

    [Fact]
    public void State_IsNotEqualToPlayableCellState()
    {
        Assert.NotEqual(EmptyCell.Instance.GetState(), new PlayableCell(0, 0).GetState());
    }

    [Fact]
    public void IsValidState_AcceptsItsOwnStateOnly()
    {
        Assert.True(EmptyCell.Instance.IsValidState(EmptyCell.Instance.GetState()));
        Assert.False(EmptyCell.Instance.IsValidState(new PlayableCell(0, 0).GetState()));
    }
}
