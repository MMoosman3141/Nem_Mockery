using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Stubbing property getters (via When) and setters (via WhenSet), static and
/// instance, per the README "Properties, constructors, out parameters" section.
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_StaticPropertyGetter_ReturnsStub() {
    using MockContext context = new();
    StaticCalculator.Value = 1;

    Mock.When(() => StaticCalculator.Value).ThenReturn(42);

    Assert.Equal(42, StaticCalculator.Value);
  }

  [Fact]
  public void WhenSet_StaticPropertySetter_ThenDoNothingSwallowsWrite() {
    using MockContext context = new();
    StaticCalculator.Value = 1;

    Mock.WhenSet(() => StaticCalculator.Value, () => Arg.Any<int>()).ThenDoNothing();
    StaticCalculator.Value = 99;

    // The getter is not stubbed, so this reads the real backing field: the
    // swallowed write never stored 99.
    Assert.Equal(1, StaticCalculator.Value);
  }

  [Fact]
  public void WhenSet_ValueDoesNotMatch_RealSetterRuns() {
    using MockContext context = new();
    StaticCalculator.Value = 1;

    Mock.WhenSet(() => StaticCalculator.Value, () => 5).ThenDoNothing();
    StaticCalculator.Value = 99;

    Assert.Equal(99, StaticCalculator.Value);
  }

  [Fact]
  public void When_InstancePropertyGetter_StubAppliesToThatInstance() {
    using MockContext context = new();
    SealedGreeter greeter = new("Ada");
    SealedGreeter other = new("Grace");

    Mock.When(() => greeter.Name).ThenReturn("stubbed");

    Assert.Equal("stubbed", greeter.Name);
    Assert.Equal("Grace", other.Name);
  }

  [Fact]
  public void WhenSet_GetterOnlyProperty_ThrowsMockeryException() {
    using MockContext context = new();
    Widget widget = new("w");

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.WhenSet(() => widget.Name, () => Arg.Any<string?>()));

    Assert.Contains("no setter", thrown.Message);
  }

  [Fact]
  public void WhenSet_NonPropertyLambda_ThrowsMockeryException() {
    using MockContext context = new();

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.WhenSet(() => StaticCalculator.Add(1, 1), () => Arg.Any<int>()));

    Assert.Contains("property", thrown.Message, StringComparison.OrdinalIgnoreCase);
  }
}
