namespace Nem_Mockery;

/// <summary>
/// Fluent continuation of <c>Mock.When</c> for a value-returning method: defines what
/// matching calls do. Each <c>Then*</c> call appends one step to a response sequence;
/// consecutive matching calls step through the sequence and the last step repeats.
/// </summary>
/// <typeparam name="TResult">The return type of the stubbed method.</typeparam>
public interface IStubBuilder<TResult> {
  /// <summary>
  /// Makes matching calls return <paramref name="value"/>.
  /// </summary>
  /// <param name="value">The canned return value.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  IStubBuilder<TResult> ThenReturn(TResult value);

  /// <summary>
  /// Makes consecutive matching calls return the given values in order; after the last
  /// value is reached it repeats.
  /// </summary>
  /// <param name="values">The values to return in sequence; must not be empty.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
  IStubBuilder<TResult> ThenReturn(params TResult[] values);

  /// <summary>
  /// Makes matching calls throw <paramref name="exception"/> (the same instance each time).
  /// </summary>
  /// <param name="exception">The exception to throw.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  IStubBuilder<TResult> ThenThrow(Exception exception);

  /// <summary>
  /// Makes matching calls throw a new <typeparamref name="TException"/> per call.
  /// </summary>
  /// <typeparam name="TException">The exception type to construct and throw.</typeparam>
  /// <returns>The same builder, for chaining further steps.</returns>
  IStubBuilder<TResult> ThenThrow<TException>() where TException : Exception, new();

  /// <summary>
  /// Makes matching calls run <paramref name="answer"/> and return its result. The
  /// callback receives the live <see cref="IInvocation"/> and may set
  /// <see langword="ref"/>/<see langword="out"/> arguments or forward to the original.
  /// </summary>
  /// <param name="answer">The callback that computes the result.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  IStubBuilder<TResult> ThenAnswer(Func<IInvocation, TResult> answer);

  /// <summary>
  /// Makes matching calls run the real implementation.
  /// </summary>
  /// <returns>The same builder, for chaining further steps.</returns>
  IStubBuilder<TResult> ThenCallOriginal();
}

/// <summary>
/// Fluent continuation of <c>Mock.When</c> for a <see langword="void"/> method,
/// property setter, or constructor: defines what matching calls do. Each <c>Then*</c>
/// call appends one step to a response sequence; consecutive matching calls step
/// through the sequence and the last step repeats.
/// </summary>
public interface IVoidStubBuilder {
  /// <summary>
  /// Makes matching calls do nothing. For a stubbed constructor this skips the real
  /// constructor body, leaving fields at their defaults.
  /// </summary>
  /// <returns>The same builder, for chaining further steps.</returns>
  IVoidStubBuilder ThenDoNothing();

  /// <summary>
  /// Makes matching calls throw <paramref name="exception"/> (the same instance each time).
  /// </summary>
  /// <param name="exception">The exception to throw.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  IVoidStubBuilder ThenThrow(Exception exception);

  /// <summary>
  /// Makes matching calls throw a new <typeparamref name="TException"/> per call.
  /// </summary>
  /// <typeparam name="TException">The exception type to construct and throw.</typeparam>
  /// <returns>The same builder, for chaining further steps.</returns>
  IVoidStubBuilder ThenThrow<TException>() where TException : Exception, new();

  /// <summary>
  /// Makes matching calls run <paramref name="answer"/>. The callback receives the live
  /// <see cref="IInvocation"/> and may set <see langword="ref"/>/<see langword="out"/>
  /// arguments or forward to the original.
  /// </summary>
  /// <param name="answer">The callback to run.</param>
  /// <returns>The same builder, for chaining further steps.</returns>
  IVoidStubBuilder ThenAnswer(Action<IInvocation> answer);

  /// <summary>
  /// Makes matching calls run the real implementation.
  /// </summary>
  /// <returns>The same builder, for chaining further steps.</returns>
  IVoidStubBuilder ThenCallOriginal();
}
