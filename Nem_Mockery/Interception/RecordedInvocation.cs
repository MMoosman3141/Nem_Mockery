using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Nem_Mockery.Interception;

/// <summary>
/// One intercepted call: the live argument buffer shared with the generated handler
/// (so <see cref="SetArgument"/> updates flow back to by-ref parameters), plus the
/// trampoline to the real implementation. Also serves as the record that
/// <c>Mock.Verify</c> counts.
/// </summary>
internal sealed class RecordedInvocation : IInvocation {
  private readonly object?[] _argumentsWithSelf;
  private readonly int _argumentOffset;
  private readonly Delegate _original;

  internal RecordedInvocation(MethodBase method, object?[] argumentsWithSelf, int argumentOffset, Delegate original) {
    Method = method;
    _argumentsWithSelf = argumentsWithSelf;
    _argumentOffset = argumentOffset;
    _original = original;
  }

  public MethodBase Method { get; }

  public object? Instance => (_argumentOffset == 1) ? _argumentsWithSelf[0] : null;

  public IReadOnlyList<object?> Arguments =>
    new ArraySegment<object?>(_argumentsWithSelf, _argumentOffset, _argumentsWithSelf.Length - _argumentOffset);

  /// <summary>
  /// Whether a Verify call has already matched this invocation (consumed by
  /// <c>VerifyNoOtherCalls</c>).
  /// </summary>
  internal bool Verified { get; set; }

  public void SetArgument(int index, object? value) {
    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _argumentsWithSelf.Length - _argumentOffset);
    _argumentsWithSelf[index + _argumentOffset] = value;
  }

  public object? CallOriginal() {
    try {
      // DynamicInvoke copies by-ref parameter updates back into the array, which the
      // generated handler then writes back to the caller's variables.
      return _original.DynamicInvoke(_argumentsWithSelf);
    } catch (TargetInvocationException wrapped) when (wrapped.InnerException is not null) {
      ExceptionDispatchInfo.Capture(wrapped.InnerException).Throw();
      throw; // Unreachable; satisfies definite return analysis.
    }
  }
}
