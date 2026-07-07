using Nem_Mockery;

namespace xUnit_Nem_Mockery;

/// <summary>
/// The Arg markers' documented out-of-expression behavior: they are inert and just
/// return default.
/// </summary>
public class ArgTests {
  [Fact]
  public void Any_OutsideExpression_ReturnsDefault() {
    Assert.Equal(0, Arg.Any<int>());
    Assert.Null(Arg.Any<string>());
  }

  [Fact]
  public void Is_OutsideExpression_ReturnsDefault() {
    Assert.Equal(0, Arg.Is<int>(n => n > 5));
    Assert.Null(Arg.Is<string>(s => s.Length > 0));
  }
}
