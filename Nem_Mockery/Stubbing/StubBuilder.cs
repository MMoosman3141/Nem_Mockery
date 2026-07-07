namespace Nem_Mockery.Stubbing;

/// <summary>
/// The concrete builder behind <see cref="IStubBuilder{TResult}"/>; every call appends
/// one behavior to the wrapped <see cref="Stub"/>.
/// </summary>
internal sealed class StubBuilder<TResult>(Stub stub) : IStubBuilder<TResult> {
  public IStubBuilder<TResult> ThenReturn(TResult value) {
    stub.AddBehavior(new ReturnBehavior(value));
    return this;
  }

  public IStubBuilder<TResult> ThenReturn(params TResult[] values) {
    ArgumentNullException.ThrowIfNull(values);
    if (values.Length == 0) {
      throw new ArgumentException("At least one return value is required.", nameof(values));
    }
    foreach (TResult value in values) {
      stub.AddBehavior(new ReturnBehavior(value));
    }
    return this;
  }

  public IStubBuilder<TResult> ThenThrow(Exception exception) {
    ArgumentNullException.ThrowIfNull(exception);
    stub.AddBehavior(new ThrowBehavior(() => exception));
    return this;
  }

  public IStubBuilder<TResult> ThenThrow<TException>() where TException : Exception, new() {
    stub.AddBehavior(new ThrowBehavior(static () => new TException()));
    return this;
  }

  public IStubBuilder<TResult> ThenAnswer(Func<IInvocation, TResult> answer) {
    ArgumentNullException.ThrowIfNull(answer);
    stub.AddBehavior(new AnswerBehavior(invocation => answer(invocation)));
    return this;
  }

  public IStubBuilder<TResult> ThenCallOriginal() {
    stub.AddBehavior(new CallOriginalBehavior());
    return this;
  }
}

/// <summary>
/// The concrete builder behind <see cref="IVoidStubBuilder"/>; every call appends one
/// behavior to the wrapped <see cref="Stub"/>.
/// </summary>
internal sealed class VoidStubBuilder(Stub stub) : IVoidStubBuilder {
  public IVoidStubBuilder ThenDoNothing() {
    stub.AddBehavior(new DoNothingBehavior());
    return this;
  }

  public IVoidStubBuilder ThenThrow(Exception exception) {
    ArgumentNullException.ThrowIfNull(exception);
    stub.AddBehavior(new ThrowBehavior(() => exception));
    return this;
  }

  public IVoidStubBuilder ThenThrow<TException>() where TException : Exception, new() {
    stub.AddBehavior(new ThrowBehavior(static () => new TException()));
    return this;
  }

  public IVoidStubBuilder ThenAnswer(Action<IInvocation> answer) {
    ArgumentNullException.ThrowIfNull(answer);
    stub.AddBehavior(new AnswerBehavior(invocation => {
      answer(invocation);
      return null;
    }));
    return this;
  }

  public IVoidStubBuilder ThenCallOriginal() {
    stub.AddBehavior(new CallOriginalBehavior());
    return this;
  }
}
