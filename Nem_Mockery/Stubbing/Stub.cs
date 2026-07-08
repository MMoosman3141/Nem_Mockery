using Nem_Mockery.Matching;

namespace Nem_Mockery.Stubbing;

/// <summary>
/// One registered <c>Mock.When(...)</c> arrangement: the receiver and argument shape
/// it applies to, plus the sequence of behaviors consecutive matching calls step
/// through. A stub with no behaviors yet (a <c>When</c> without any <c>Then*</c>)
/// never matches, so it cannot change program behavior.
/// </summary>
internal sealed class Stub(
    MockContext owner, object? instance, IReadOnlyList<IArgumentMatcher> matchers, bool ignoreInstance) {
  private readonly List<IBehavior> _behaviors = [];
  private int _executionCount = -1;

  /// <summary>
  /// The context the stub was created in; the stub dies with it.
  /// </summary>
  internal MockContext Owner { get; } = owner;

  /// <summary>
  /// The receiver the stub was written against, or <see langword="null"/> for statics.
  /// </summary>
  internal object? Instance { get; } = instance;

  /// <summary>
  /// One matcher per parameter of the stubbed method.
  /// </summary>
  internal IReadOnlyList<IArgumentMatcher> Matchers { get; } = matchers;

  /// <summary>
  /// Whether receiver matching is skipped (true for constructor stubs, whose
  /// receiver is always a fresh allocation).
  /// </summary>
  internal bool IgnoreInstance { get; } = ignoreInstance;

  internal void AddBehavior(IBehavior behavior) {
    _behaviors.Add(behavior);
  }

  /// <summary>
  /// Returns whether a call with this receiver and these arguments should be answered
  /// by this stub.
  /// </summary>
  internal bool Matches(object? instance, IReadOnlyList<object?> arguments) {
    if (Owner.IsDisposed || (_behaviors.Count == 0)) {
      return false;
    }
    if (!IgnoreInstance && !InvocationMatcher.InstanceMatches(Instance, instance)) {
      return false;
    }
    return InvocationMatcher.ArgumentsMatch(Matchers, arguments);
  }

  /// <summary>
  /// Runs the next behavior in the sequence; once the sequence is exhausted the last
  /// behavior repeats (Mockito's <c>thenReturn(a, b)</c> semantics).
  /// </summary>
  internal object? Execute(IInvocation invocation) {
    int index = Interlocked.Increment(ref _executionCount);
    IBehavior behavior = _behaviors[Math.Min(index, _behaviors.Count - 1)];
    return behavior.Execute(invocation);
  }
}
