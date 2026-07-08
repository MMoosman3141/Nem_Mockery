using System.Linq.Expressions;
using System.Text;
using Nem_Mockery.Interception;
using Nem_Mockery.Parsing;
using Nem_Mockery.Stubbing;

namespace Nem_Mockery;

/// <summary>
/// The Mockito-flavored entry point for stubbing and verification. Works on any
/// concrete method — static, sealed, non-virtual, on classes, records, or structs —
/// by detouring the compiled method at runtime, so no interfaces or virtual members
/// are required. All calls need an active <see cref="MockContext"/>.
/// </summary>
/// <example>
/// <code>
/// using static Nem_Mockery.Mock;
///
/// using MockContext context = new();
///
/// When(() => FileReader.Read(Arg.Any&lt;string&gt;())).ThenReturn("data");
/// string result = FileReader.Read("a.txt");   // "data"
/// FileReader.Delete("other.txt");             // unstubbed member: real code runs
///
/// Verify(() => FileReader.Read("a.txt"), Times.Once());
/// </code>
/// </example>
public static class Mock {
  /// <summary>
  /// Starts stubbing a value-returning method or property read. Only calls whose
  /// receiver and arguments match the expression are stubbed; non-matching calls run
  /// the real implementation.
  /// </summary>
  /// <typeparam name="TResult">The stubbed member's return type.</typeparam>
  /// <param name="call">
  /// A lambda performing the call to stub, e.g. <c>() => Parser.Parse("x")</c> or
  /// <c>() => Config.Timeout</c>. Use <see cref="Arg"/> matchers for flexible arguments.
  /// </param>
  /// <returns>A builder to attach <c>Then*</c> behaviors to.</returns>
  /// <exception cref="MockeryException">
  /// No <see cref="MockContext"/> is active, the expression is not a mockable call, or
  /// the method cannot be detoured.
  /// </exception>
  public static IStubBuilder<TResult> When<TResult>(Expression<Func<TResult>> call) {
    ArgumentNullException.ThrowIfNull(call);
    return new StubBuilder<TResult>(RegisterStub(CallExpressionParser.ParseCall(call)));
  }

  /// <summary>
  /// Starts stubbing a <see langword="void"/> method. Only calls whose receiver and
  /// arguments match the expression are stubbed; non-matching calls run the real
  /// implementation.
  /// </summary>
  /// <param name="call">A lambda performing the call to stub.</param>
  /// <returns>A builder to attach <c>Then*</c> behaviors to.</returns>
  /// <exception cref="MockeryException">
  /// No <see cref="MockContext"/> is active, the expression is not a mockable call, or
  /// the method cannot be detoured.
  /// </exception>
  public static IVoidStubBuilder When(Expression<Action> call) {
    ArgumentNullException.ThrowIfNull(call);
    return new VoidStubBuilder(RegisterStub(CallExpressionParser.ParseCall(call)));
  }

  /// <summary>
  /// Starts stubbing a property setter. The first lambda names the property; the
  /// second produces the value to match, and may use <see cref="Arg"/> matchers.
  /// </summary>
  /// <typeparam name="TProperty">The property type.</typeparam>
  /// <param name="property">A lambda reading the property, e.g. <c>() => Config.Timeout</c>.</param>
  /// <param name="value">A lambda producing the value matcher, e.g. <c>() => Arg.Any&lt;int&gt;()</c>.</param>
  /// <returns>A builder to attach <c>Then*</c> behaviors to.</returns>
  /// <exception cref="MockeryException">
  /// No <see cref="MockContext"/> is active or the property has no setter.
  /// </exception>
  public static IVoidStubBuilder WhenSet<TProperty>(
      Expression<Func<TProperty>> property, Expression<Func<TProperty>> value) {
    ArgumentNullException.ThrowIfNull(property);
    ArgumentNullException.ThrowIfNull(value);
    return new VoidStubBuilder(RegisterStub(CallExpressionParser.ParseSetter(property, value)));
  }

  /// <summary>
  /// Starts stubbing a constructor. A matching <c>new</c> expression still allocates
  /// the object, but the stub decides whether the constructor body runs:
  /// <c>ThenDoNothing()</c> skips it (fields stay at their defaults), and
  /// <c>ThenAnswer</c> can initialize the instance via
  /// <see cref="IInvocation.Instance"/>.
  /// </summary>
  /// <typeparam name="T">The constructed type.</typeparam>
  /// <param name="construction">
  /// A lambda with a single <c>new</c> expression, e.g.
  /// <c>() => new Widget(Arg.Any&lt;string&gt;())</c>.
  /// </param>
  /// <returns>A builder to attach <c>Then*</c> behaviors to.</returns>
  /// <exception cref="MockeryException">
  /// No <see cref="MockContext"/> is active or the lambda is not a constructor call.
  /// </exception>
  public static IVoidStubBuilder WhenNew<T>(Expression<Func<T>> construction) {
    ArgumentNullException.ThrowIfNull(construction);
    return new VoidStubBuilder(RegisterStub(CallExpressionParser.ParseConstruction(construction)));
  }

  /// <summary>
  /// Asserts the described call happened exactly once while its method was mocked in
  /// the current context.
  /// </summary>
  /// <typeparam name="TResult">The verified member's return type.</typeparam>
  /// <param name="call">A lambda describing the call, with values or <see cref="Arg"/> matchers.</param>
  /// <exception cref="VerificationException">The call count is not exactly one.</exception>
  /// <exception cref="MockeryException">The method was never mocked in this context.</exception>
  public static void Verify<TResult>(Expression<Func<TResult>> call) {
    Verify(call, Times.Once());
  }

  /// <summary>
  /// Asserts the described call happened the expected number of times while its
  /// method was mocked in the current context. All calls to a mocked method are
  /// counted, including ones that fell through to the real implementation.
  /// </summary>
  /// <typeparam name="TResult">The verified member's return type.</typeparam>
  /// <param name="call">A lambda describing the call, with values or <see cref="Arg"/> matchers.</param>
  /// <param name="times">The expected call count.</param>
  /// <exception cref="VerificationException">The call count does not satisfy <paramref name="times"/>.</exception>
  /// <exception cref="MockeryException">The method was never mocked in this context.</exception>
  public static void Verify<TResult>(Expression<Func<TResult>> call, Times times) {
    ArgumentNullException.ThrowIfNull(call);
    VerifyCore(CallExpressionParser.ParseCall(call), times);
  }

  /// <summary>
  /// Asserts the described <see langword="void"/> call happened exactly once while its
  /// method was mocked in the current context.
  /// </summary>
  /// <param name="call">A lambda describing the call, with values or <see cref="Arg"/> matchers.</param>
  /// <exception cref="VerificationException">The call count is not exactly one.</exception>
  /// <exception cref="MockeryException">The method was never mocked in this context.</exception>
  public static void Verify(Expression<Action> call) {
    Verify(call, Times.Once());
  }

  /// <summary>
  /// Asserts the described <see langword="void"/> call happened the expected number of
  /// times while its method was mocked in the current context.
  /// </summary>
  /// <param name="call">A lambda describing the call, with values or <see cref="Arg"/> matchers.</param>
  /// <param name="times">The expected call count.</param>
  /// <exception cref="VerificationException">The call count does not satisfy <paramref name="times"/>.</exception>
  /// <exception cref="MockeryException">The method was never mocked in this context.</exception>
  public static void Verify(Expression<Action> call, Times times) {
    ArgumentNullException.ThrowIfNull(call);
    VerifyCore(CallExpressionParser.ParseCall(call), times);
  }

  /// <summary>
  /// Asserts the property was assigned a matching value exactly once while its setter
  /// was mocked in the current context.
  /// </summary>
  /// <typeparam name="TProperty">The property type.</typeparam>
  /// <param name="property">A lambda reading the property.</param>
  /// <param name="value">A lambda producing the value matcher.</param>
  /// <exception cref="VerificationException">The call count is not exactly one.</exception>
  /// <exception cref="MockeryException">The setter was never mocked in this context.</exception>
  public static void VerifySet<TProperty>(
      Expression<Func<TProperty>> property, Expression<Func<TProperty>> value) {
    VerifySet(property, value, Times.Once());
  }

  /// <summary>
  /// Asserts the property was assigned a matching value the expected number of times
  /// while its setter was mocked in the current context.
  /// </summary>
  /// <typeparam name="TProperty">The property type.</typeparam>
  /// <param name="property">A lambda reading the property.</param>
  /// <param name="value">A lambda producing the value matcher.</param>
  /// <param name="times">The expected call count.</param>
  /// <exception cref="VerificationException">The call count does not satisfy <paramref name="times"/>.</exception>
  /// <exception cref="MockeryException">The setter was never mocked in this context.</exception>
  public static void VerifySet<TProperty>(
      Expression<Func<TProperty>> property, Expression<Func<TProperty>> value, Times times) {
    ArgumentNullException.ThrowIfNull(property);
    ArgumentNullException.ThrowIfNull(value);
    VerifyCore(CallExpressionParser.ParseSetter(property, value), times);
  }

  /// <summary>
  /// Asserts a matching constructor call happened exactly once while the constructor
  /// was mocked in the current context.
  /// </summary>
  /// <typeparam name="T">The constructed type.</typeparam>
  /// <param name="construction">A lambda with a single <c>new</c> expression.</param>
  /// <exception cref="VerificationException">The call count is not exactly one.</exception>
  /// <exception cref="MockeryException">The constructor was never mocked in this context.</exception>
  public static void VerifyNew<T>(Expression<Func<T>> construction) {
    VerifyNew(construction, Times.Once());
  }

  /// <summary>
  /// Asserts a matching constructor call happened the expected number of times while
  /// the constructor was mocked in the current context.
  /// </summary>
  /// <typeparam name="T">The constructed type.</typeparam>
  /// <param name="construction">A lambda with a single <c>new</c> expression.</param>
  /// <param name="times">The expected call count.</param>
  /// <exception cref="VerificationException">The call count does not satisfy <paramref name="times"/>.</exception>
  /// <exception cref="MockeryException">The constructor was never mocked in this context.</exception>
  public static void VerifyNew<T>(Expression<Func<T>> construction, Times times) {
    ArgumentNullException.ThrowIfNull(construction);
    VerifyCore(CallExpressionParser.ParseConstruction(construction), times);
  }

  /// <summary>
  /// Asserts every call recorded on the current context's mocked methods has been
  /// consumed by an earlier <c>Verify*</c> call, mirroring Mockito's
  /// <c>verifyNoMoreInteractions</c>.
  /// </summary>
  /// <exception cref="VerificationException">Unverified calls remain.</exception>
  /// <exception cref="MockeryException">No <see cref="MockContext"/> is active.</exception>
  public static void VerifyNoOtherCalls() {
    MockContext context = MockContext.RequireCurrent();
    List<string> unverified = [];
    foreach (MethodMock mock in MockRegistry.GetAll()) {
      if (!mock.IsClaimedByChainOf(context)) {
        continue;
      }
      foreach (RecordedInvocation invocation in mock.GetUnverifiedInvocations()) {
        unverified.Add(DescribeInvocation(invocation));
      }
    }
    if (unverified.Count > 0) {
      StringBuilder message = new();
      message.AppendLine("Expected no unverified calls, but found:");
      foreach (string call in unverified) {
        message.AppendLine($"  {call}");
      }
      throw new VerificationException(message.ToString());
    }
  }

  private static Stub RegisterStub(ParsedCall parsed) {
    MockContext context = MockContext.RequireCurrent();
    MethodMock mock = MockRegistry.GetOrCreate(parsed.Method);
    context.Claim(mock);
    Stub stub = new(context, parsed.Instance, parsed.Matchers, parsed.IgnoreInstance);
    mock.AddStub(stub);
    return stub;
  }

  private static void VerifyCore(ParsedCall parsed, Times times) {
    MockContext context = MockContext.RequireCurrent();
    MethodMock? mock = MockRegistry.Find(parsed.Method);
    if (mock is null || !mock.IsClaimedByChainOf(context)) {
      throw new MockeryException(
        $"Cannot verify '{MethodMock.Describe(parsed.Method)}': it is not mocked in the current " +
        "MockContext. Calls are only recorded while a method is mocked, so stub it first " +
        "(a bare When(...) with no Then* behaviors records calls without changing behavior).");
    }
    int actual = mock.CountMatchingInvocations(
      parsed.Instance, parsed.Matchers, markVerified: true, parsed.IgnoreInstance);
    if (!times.IsSatisfiedBy(actual)) {
      string[] matcherTexts = [.. parsed.Matchers.Select(matcher => matcher.ToString() ?? "?")];
      StringBuilder message = new();
      message.AppendLine($"Verification failed for {CallFormatter.Format(parsed.Method, matcherTexts)}.");
      message.AppendLine($"Expected {times}, but was called {actual} time(s) with matching arguments.");
      IReadOnlyList<RecordedInvocation> invocations = mock.GetInvocations();
      if (invocations.Count == 0) {
        message.AppendLine("No calls were recorded for this method.");
      } else {
        message.AppendLine("All recorded calls of this method:");
        foreach (RecordedInvocation invocation in invocations) {
          message.AppendLine($"  {DescribeInvocation(invocation)}");
        }
      }
      throw new VerificationException(message.ToString());
    }
  }

  private static string DescribeInvocation(RecordedInvocation invocation) {
    string[] argumentTexts = [.. invocation.Arguments.Select(a => a?.ToString() ?? "null")];
    return CallFormatter.Format(invocation.Method, argumentTexts);
  }
}
