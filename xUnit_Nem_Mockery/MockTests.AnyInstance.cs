using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// The Arg.AnyInstance receiver marker: one arrangement applies to every instance of
/// a type — methods, properties, setters, structs, and verification (README
/// "Semantics worth knowing").
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_AnyInstanceMethod_StubAppliesToEveryInstance() {
    using MockContext context = new();
    SealedGreeter first = new("Ada");
    SealedGreeter second = new("Grace");

    Mock.When(() => Arg.AnyInstance<SealedGreeter>().Greet(Arg.Any<string>())).ThenReturn("stubbed");

    Assert.Equal("stubbed", first.Greet("world"));
    Assert.Equal("stubbed", second.Greet("world"));
  }

  [Fact]
  public void When_AnyInstanceMethod_ArgumentMatchersStillFilter() {
    using MockContext context = new();
    SealedGreeter greeter = new("Ada");

    Mock.When(() => Arg.AnyInstance<SealedGreeter>().Greet("stub-me")).ThenReturn("stubbed");

    Assert.Equal("stubbed", greeter.Greet("stub-me"));
    Assert.Equal("Hello world, I am Ada.", greeter.Greet("world"));
  }

  [Fact]
  public void When_AnyInstanceProperty_StubAppliesToEveryInstance() {
    using MockContext context = new();
    SealedGreeter first = new("Ada");
    SealedGreeter second = new("Grace");

    Mock.When(() => Arg.AnyInstance<SealedGreeter>().Name).ThenReturn("everyone");

    Assert.Equal("everyone", first.Name);
    Assert.Equal("everyone", second.Name);
  }

  [Fact]
  public void WhenSet_AnyInstanceSetter_SwallowsWritesOnEveryInstance() {
    using MockContext context = new();
    SealedGreeter greeter = new("Ada");

    Mock.WhenSet(() => Arg.AnyInstance<SealedGreeter>().Name, () => Arg.Any<string>()).ThenDoNothing();
    greeter.Name = "changed";

    Assert.Equal("Ada", greeter.Name);
  }

  [Fact]
  public void When_AnyInstanceStructMethod_StubAppliesToEveryValue() {
    using MockContext context = new();
    TickCounter first = new() { Count = 5 };
    TickCounter second = new() { Count = 10 };

    Mock.When(() => Arg.AnyInstance<TickCounter>().Next()).ThenReturn(99);

    Assert.Equal(99, first.Next());
    Assert.Equal(99, second.Next());
    Assert.Equal(5, first.Count);
    Assert.Equal(10, second.Count);
  }

  [Fact]
  public void When_ExactInstanceStubbedAfterAnyInstance_NewestWinsForThatInstance() {
    using MockContext context = new();
    SealedGreeter special = new("Ada");
    SealedGreeter ordinary = new("Grace");

    Mock.When(() => Arg.AnyInstance<SealedGreeter>().Greet(Arg.Any<string>())).ThenReturn("general");
    Mock.When(() => special.Greet(Arg.Any<string>())).ThenReturn("special");

    Assert.Equal("special", special.Greet("world"));
    Assert.Equal("general", ordinary.Greet("world"));
  }

  [Fact]
  public void Verify_AnyInstance_CountsCallsAcrossAllInstances() {
    using MockContext context = new();
    SealedGreeter first = new("Ada");
    SealedGreeter second = new("Grace");

    Mock.When(() => Arg.AnyInstance<SealedGreeter>().Greet(Arg.Any<string>())).ThenReturn("stubbed");
    first.Greet("a");
    second.Greet("b");
    second.Greet("c");

    Mock.Verify(() => Arg.AnyInstance<SealedGreeter>().Greet(Arg.Any<string>()), Times.Exactly(3));
    Mock.Verify(() => second.Greet(Arg.Any<string>()), Times.Exactly(2));
    Mock.Verify(() => Arg.AnyInstance<SealedGreeter>().Greet("a"), Times.Once());
  }

  [Fact]
  public void When_AnyInstanceUsedAsArgument_ThrowsMockeryException() {
    using MockContext context = new();
    SealedGreeter greeter = new("Ada");

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.When(() => greeter.Greet(Arg.AnyInstance<string>())));

    Assert.Contains("receiver", thrown.Message);
  }

  [Fact]
  public void AnyInstance_OutsideExpression_ReturnsDefault() {
    Assert.Null(Arg.AnyInstance<string>());
    Assert.Equal(0, Arg.AnyInstance<int>());
  }
}
