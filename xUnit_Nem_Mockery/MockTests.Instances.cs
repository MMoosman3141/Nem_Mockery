using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Stubbing instance methods on sealed classes, records, and structs — the receivers
/// proxy-based mocking libraries cannot touch. Per README "Semantics worth knowing":
/// class instances match by reference, struct receivers by value.
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_SealedClassMethod_StubAppliesToThatInstance() {
    using MockContext context = new();
    SealedGreeter greeter = new("Ada");

    Mock.When(() => greeter.Greet(Arg.Any<string>())).ThenReturn("stubbed");

    Assert.Equal("stubbed", greeter.Greet("world"));
  }

  [Fact]
  public void When_SealedClassMethod_OtherInstanceRunsRealCode() {
    using MockContext context = new();
    SealedGreeter mocked = new("Ada");
    SealedGreeter real = new("Grace");

    Mock.When(() => mocked.Greet(Arg.Any<string>())).ThenReturn("stubbed");

    Assert.Equal("Hello world, I am Grace.", real.Greet("world"));
  }

  [Fact]
  public void When_SealedRecordMethod_ReturnsStub() {
    using MockContext context = new();
    Order order = new("SKU-1", 3);

    Mock.When(() => order.Total(Arg.Any<decimal>())).ThenReturn(999m);

    Assert.Equal(999m, order.Total(5m));
  }

  [Fact]
  public void When_SealedRecordMethod_EqualButDistinctInstanceRunsRealCode() {
    using MockContext context = new();
    Order mocked = new("SKU-1", 3);
    Order equalTwin = new("SKU-1", 3);

    Mock.When(() => mocked.Total(Arg.Any<decimal>())).ThenReturn(999m);

    // Class instances (records included) match by identity, not equality.
    Assert.Equal(15m, equalTwin.Total(5m));
  }

  [Fact]
  public void When_StructMethod_StubSkipsMutation() {
    using MockContext context = new();
    TickCounter counter = new() { Count = 5 };

    Mock.When(() => counter.Next()).ThenReturn(99);
    int result = counter.Next();

    Assert.Equal(99, result);
    Assert.Equal(5, counter.Count);
  }

  [Fact]
  public void When_StructMethodMiss_RealMutationWritesBackToCaller() {
    using MockContext context = new();
    TickCounter mocked = new() { Count = 5 };
    TickCounter other = new() { Count = 10 };

    Mock.When(() => mocked.Next()).ThenReturn(99);
    int result = other.Next();

    // Struct receivers match by value; Count=10 misses, so the real body runs and
    // its mutation of 'this' must flow back through the by-ref receiver.
    Assert.Equal(11, result);
    Assert.Equal(11, other.Count);
  }
}
