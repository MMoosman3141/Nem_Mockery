using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Core stubbing behavior on static methods: canned returns, matchers, sequences,
/// throws, answers, and the fall-through-to-original miss policy documented in
/// README "Semantics worth knowing".
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_StaticMethodExactArguments_ReturnsStub() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(100);

    Assert.Equal(100, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void When_ArgumentsDoNotMatch_RealImplementationRuns() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(100);

    // Miss policy: unmatched arguments fall through to real code.
    Assert.Equal(7, StaticCalculator.Add(3, 4));
  }

  [Fact]
  public void When_ArgAny_MatchesEveryValue() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>())).ThenReturn(-1);

    Assert.Equal(-1, StaticCalculator.Add(0, 0));
    Assert.Equal(-1, StaticCalculator.Add(int.MaxValue, int.MinValue));
  }

  [Fact]
  public void When_ArgIs_MatchesOnlyPredicatePasses() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Is<int>(n => n > 10), Arg.Any<int>())).ThenReturn(0);

    Assert.Equal(0, StaticCalculator.Add(11, 5));
    Assert.Equal(6, StaticCalculator.Add(1, 5));
  }

  [Fact]
  public void ThenReturn_SequencedValues_ReturnedInOrderThenLastRepeats() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(10, 20, 30);

    Assert.Equal(10, StaticCalculator.Add(1, 1));
    Assert.Equal(20, StaticCalculator.Add(1, 1));
    Assert.Equal(30, StaticCalculator.Add(1, 1));
    Assert.Equal(30, StaticCalculator.Add(1, 1));
  }

  [Fact]
  public void ThenReturn_ChainedWithThenThrow_StepsThroughSequence() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1))
      .ThenReturn(10)
      .ThenThrow(new InvalidOperationException("sequence end"));

    Assert.Equal(10, StaticCalculator.Add(1, 1));
    InvalidOperationException thrown =
      Assert.Throws<InvalidOperationException>(() => StaticCalculator.Add(1, 1));
    Assert.Equal("sequence end", thrown.Message);
  }

  [Fact]
  public void ThenReturn_EmptyValues_ThrowsArgumentException() {
    using MockContext context = new();

    IStubBuilder<int> builder = Mock.When(() => StaticCalculator.Add(1, 1));

    Assert.Throws<ArgumentException>(() => builder.ThenReturn([]));
  }

  [Fact]
  public void ThenThrow_ExceptionInstance_ThrownByMockedCall() {
    using MockContext context = new();
    InvalidOperationException planted = new("planted");

    Mock.When(() => StaticCalculator.Describe(Arg.Any<string>())).ThenThrow(planted);

    InvalidOperationException thrown =
      Assert.Throws<InvalidOperationException>(() => StaticCalculator.Describe("x"));
    Assert.Same(planted, thrown);
  }

  [Fact]
  public void ThenThrow_Generic_ThrowsFreshInstancePerCall() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Describe("boom")).ThenThrow<InvalidOperationException>();

    InvalidOperationException first =
      Assert.Throws<InvalidOperationException>(() => StaticCalculator.Describe("boom"));
    InvalidOperationException second =
      Assert.Throws<InvalidOperationException>(() => StaticCalculator.Describe("boom"));
    Assert.NotSame(first, second);
  }

  [Fact]
  public void ThenAnswer_ReceivesInvocationArguments() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>()))
      .ThenAnswer(invocation => ((int)invocation.Arguments[0]!) * ((int)invocation.Arguments[1]!));

    Assert.Equal(12, StaticCalculator.Add(3, 4));
  }

  [Fact]
  public void ThenAnswer_CallOriginal_ForwardsToRealCode() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>()))
      .ThenAnswer(invocation => ((int)invocation.CallOriginal()!) + 1000);

    Assert.Equal(1003, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void ThenCallOriginal_RunsRealImplementation() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(99).ThenCallOriginal();

    Assert.Equal(99, StaticCalculator.Add(1, 1));
    Assert.Equal(2, StaticCalculator.Add(1, 1));
  }

  [Fact]
  public void When_VoidMethod_ThenDoNothingSuppressesSideEffect() {
    using MockContext context = new();
    StaticCalculator.Value = 5;

    Mock.When(() => StaticCalculator.Increment()).ThenDoNothing();
    StaticCalculator.Increment();

    Assert.Equal(5, StaticCalculator.Value);
  }

  [Fact]
  public void When_VoidMethod_ThenThrowThrows() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Increment()).ThenThrow<InvalidOperationException>();

    Assert.Throws<InvalidOperationException>(StaticCalculator.Increment);
  }

  [Fact]
  public void When_VoidMethodNoBehaviors_RealCodeStillRuns() {
    using MockContext context = new();
    StaticCalculator.Value = 5;

    // A When(...) with no Then* is inert: it records calls but changes nothing.
    Mock.When(() => StaticCalculator.Increment());
    StaticCalculator.Increment();

    Assert.Equal(6, StaticCalculator.Value);
    Mock.Verify(() => StaticCalculator.Increment(), Times.Once());
  }

  [Fact]
  public void When_OutParameter_SetArgumentWritesBackToCaller() {
    using MockContext context = new();
    int ignored = 0;

    Mock.When(() => StaticCalculator.TryParsePositive(Arg.Any<string>(), out ignored))
      .ThenAnswer(invocation => {
        invocation.SetArgument(1, 42);
        return true;
      });

    bool parsed = StaticCalculator.TryParsePositive("not a number", out int number);

    Assert.True(parsed);
    Assert.Equal(42, number);
  }

  [Fact]
  public void When_OutParameterMiss_RealParsingRunsAndWritesBack() {
    using MockContext context = new();
    int ignored = 0;

    Mock.When(() => StaticCalculator.TryParsePositive("stubbed", out ignored)).ThenAnswer(invocation => {
      invocation.SetArgument(1, -1);
      return false;
    });

    bool parsed = StaticCalculator.TryParsePositive("17", out int number);

    Assert.True(parsed);
    Assert.Equal(17, number);
  }

  [Fact]
  public void When_TwoStubsMatchSameCall_NewestWins() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(10);
    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(20);

    Assert.Equal(20, StaticCalculator.Add(1, 1));
  }

  [Fact]
  public void When_FieldExpression_ThrowsMockeryException() {
    using MockContext context = new();
    TickCounter counter = new();

    MockeryException thrown = Assert.Throws<MockeryException>(() => Mock.When(() => counter.Count));

    Assert.Contains("field", thrown.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void When_AbstractMethod_ThrowsMockeryException() {
    using MockContext context = new();
    Stream stream = new MemoryStream();

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.When(() => stream.Read(Arg.Any<byte[]>(), 0, 0)));

    Assert.Contains("abstract", thrown.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void When_TenParameterMethod_StubAndMissBothWork() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Concat(
      "a", "b", "c", "d", "e", Arg.Any<string>(), "g", "h", "i", "j")).ThenReturn("stubbed");

    Assert.Equal("stubbed", StaticCalculator.Concat("a", "b", "c", "d", "e", "X", "g", "h", "i", "j"));
    Assert.Equal(
      "a+b+c+d+e+f+g+h+i+X",
      StaticCalculator.Concat("a", "b", "c", "d", "e", "f", "g", "h", "i", "X"));
  }

  [Fact]
  public void ThenReturn_NullForNullableValueType_ReturnsNull() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.FindOrNull("42")).ThenReturn((int?)null);

    Assert.Null(StaticCalculator.FindOrNull("42"));
    Assert.Equal(7, StaticCalculator.FindOrNull("7"));
  }

  [Fact]
  public void When_NullArgument_MatchesByEquality() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Describe(null!)).ThenReturn("null-stub");

    Assert.Equal("null-stub", StaticCalculator.Describe(null!));
    Assert.Equal("real:x", StaticCalculator.Describe("x"));
  }

  [Fact]
  public void When_ArgIsPredicate_NullActualDoesNotMatch() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Describe(Arg.Is<string>(s => s.Length > 0))).ThenReturn("stubbed");

    // Null cannot satisfy the predicate (it is never invoked), so the real code runs.
    Assert.Equal("real:", StaticCalculator.Describe(null!));
    Assert.Equal("stubbed", StaticCalculator.Describe("x"));
  }

  [Fact]
  public void When_ArgIsPredicateFromVariable_Matches() {
    using MockContext context = new();
    Func<int, bool> isLarge = n => n > 100;

    Mock.When(() => StaticCalculator.Add(Arg.Is(isLarge), Arg.Any<int>())).ThenReturn(-5);

    Assert.Equal(-5, StaticCalculator.Add(101, 1));
    Assert.Equal(3, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void When_NewExpression_DirectsToWhenNew() {
    using MockContext context = new();

    MockeryException thrown = Assert.Throws<MockeryException>(() => Mock.When(() => new Widget("x")));

    Assert.Contains("WhenNew", thrown.Message);
  }
}
