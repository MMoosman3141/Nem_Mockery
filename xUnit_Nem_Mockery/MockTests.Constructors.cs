using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Constructor mocking via WhenNew: the object is still allocated, but the stub
/// controls whether the constructor body runs (README "Properties, constructors,
/// out parameters").
/// </summary>
public partial class MockTests {
  [Fact]
  public void WhenNew_ThenDoNothing_SkipsConstructorBody() {
    using MockContext context = new();

    Mock.WhenNew(() => new Widget(Arg.Any<string>())).ThenDoNothing();
    Widget widget = new("anything");

    Assert.Null(widget.Name);
  }

  [Fact]
  public void WhenNew_ArgumentsDoNotMatch_RealConstructorRuns() {
    using MockContext context = new();

    Mock.WhenNew(() => new Widget("only-this")).ThenDoNothing();
    Widget widget = new("other");

    Assert.Equal("real:other", widget.Name);
  }

  [Fact]
  public void WhenNew_ThenAnswer_ReceivesAllocatedInstance() {
    using MockContext context = new();
    object? seenInstance = null;

    Mock.WhenNew(() => new Widget(Arg.Any<string>())).ThenAnswer(invocation => {
      seenInstance = invocation.Instance;
    });
    Widget widget = new("x");

    Assert.Same(widget, seenInstance);
  }

  [Fact]
  public void WhenNew_ThenThrow_FailsConstruction() {
    using MockContext context = new();

    Mock.WhenNew(() => new Widget(Arg.Any<string>())).ThenThrow<InvalidOperationException>();

    Assert.Throws<InvalidOperationException>(() => new Widget("x"));
  }

  [Fact]
  public void VerifyNew_CountsMatchingConstructions() {
    using MockContext context = new();

    Mock.WhenNew(() => new Widget(Arg.Any<string>())).ThenCallOriginal();
    Widget first = new("a");
    Widget second = new("b");

    Assert.Equal("real:a", first.Name);
    Assert.Equal("real:b", second.Name);
    Mock.VerifyNew(() => new Widget(Arg.Any<string>()), Times.Exactly(2));
    Mock.VerifyNew(() => new Widget("a"), Times.Once());
    Mock.VerifyNew(() => new Widget("zzz"), Times.Never());
  }

  [Fact]
  public void WhenNew_NonNewLambda_ThrowsMockeryException() {
    using MockContext context = new();

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.WhenNew(() => StaticCalculator.Describe("x")));

    Assert.Contains("new", thrown.Message);
  }
}
