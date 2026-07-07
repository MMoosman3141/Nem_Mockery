namespace Nem_Mockery.Stubbing;

/// <summary>
/// One step in a stub's response sequence: what a mocked call does when this step
/// is reached.
/// </summary>
internal interface IBehavior {
  /// <summary>
  /// Produces the result of the mocked call (boxed), or <see langword="null"/> for
  /// <see langword="void"/> methods and constructors.
  /// </summary>
  object? Execute(IInvocation invocation);
}

/// <summary>
/// Returns a fixed, pre-boxed value.
/// </summary>
internal sealed class ReturnBehavior(object? value) : IBehavior {
  public object? Execute(IInvocation invocation) {
    return value;
  }
}

/// <summary>
/// Throws an exception produced by the factory (a fresh instance per call for the
/// generic <c>ThenThrow&lt;TException&gt;()</c> form).
/// </summary>
internal sealed class ThrowBehavior(Func<Exception> exceptionFactory) : IBehavior {
  public object? Execute(IInvocation invocation) {
    throw exceptionFactory();
  }
}

/// <summary>
/// Delegates the response to a user callback that receives the live invocation.
/// </summary>
internal sealed class AnswerBehavior(Func<IInvocation, object?> answer) : IBehavior {
  public object? Execute(IInvocation invocation) {
    return answer(invocation);
  }
}

/// <summary>
/// Forwards to the real implementation.
/// </summary>
internal sealed class CallOriginalBehavior : IBehavior {
  public object? Execute(IInvocation invocation) {
    return invocation.CallOriginal();
  }
}

/// <summary>
/// Does nothing and returns <see langword="null"/> — used by <c>ThenDoNothing</c> on
/// void methods and to skip constructor bodies.
/// </summary>
internal sealed class DoNothingBehavior : IBehavior {
  public object? Execute(IInvocation invocation) {
    return null;
  }
}
