using Nem_Mockery;

namespace xUnit_Nem_Mockery;

/// <summary>
/// The Times contract: range checks per factory method, argument validation, and
/// the descriptions used in failure messages.
/// </summary>
public class TimesTests {
  [Theory]
  [InlineData(0, false)]
  [InlineData(1, true)]
  [InlineData(2, false)]
  public void Once_OnlyOneCallSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.Once().IsSatisfiedBy(actual));
  }

  [Theory]
  [InlineData(0, true)]
  [InlineData(1, false)]
  public void Never_OnlyZeroCallsSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.Never().IsSatisfiedBy(actual));
  }

  [Theory]
  [InlineData(2, false)]
  [InlineData(3, true)]
  [InlineData(4, false)]
  public void Exactly_OnlyTheExactCountSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.Exactly(3).IsSatisfiedBy(actual));
  }

  [Theory]
  [InlineData(1, false)]
  [InlineData(2, true)]
  [InlineData(1000, true)]
  public void AtLeast_CountOrMoreSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.AtLeast(2).IsSatisfiedBy(actual));
  }

  [Theory]
  [InlineData(0, false)]
  [InlineData(1, true)]
  [InlineData(5, true)]
  public void AtLeastOnce_OneOrMoreSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.AtLeastOnce().IsSatisfiedBy(actual));
  }

  [Theory]
  [InlineData(0, true)]
  [InlineData(2, true)]
  [InlineData(3, false)]
  public void AtMost_UpToCountSatisfies(int actual, bool expected) {
    Assert.Equal(expected, Times.AtMost(2).IsSatisfiedBy(actual));
  }

  [Fact]
  public void FactoryMethods_NegativeCounts_Throw() {
    Assert.Throws<ArgumentOutOfRangeException>(() => Times.Exactly(-1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Times.AtLeast(-1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Times.AtMost(-1));
  }

  [Fact]
  public void ToString_DescribesTheExpectation() {
    Assert.Equal("exactly once", Times.Once().ToString());
    Assert.Equal("never", Times.Never().ToString());
    Assert.Equal("exactly 3 time(s)", Times.Exactly(3).ToString());
    Assert.Equal("at least 2 time(s)", Times.AtLeast(2).ToString());
    Assert.Equal("at least once", Times.AtLeastOnce().ToString());
    Assert.Equal("at most 2 time(s)", Times.AtMost(2).ToString());
  }

  [Fact]
  public void DefaultValue_BehavesAsNever() {
    Times defaultTimes = default;

    Assert.True(defaultTimes.IsSatisfiedBy(0));
    Assert.False(defaultTimes.IsSatisfiedBy(1));
    Assert.Equal("never", defaultTimes.ToString());
  }
}
