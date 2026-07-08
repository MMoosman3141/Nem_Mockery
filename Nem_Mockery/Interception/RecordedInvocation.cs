using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Nem_Mockery.Interception;

/// <summary>
/// One intercepted call: the live argument buffer shared with the generated handler
/// (so <see cref="SetArgument"/> updates flow back to by-ref parameters), plus the
/// trampoline to the real implementation. Also serves as the record that
/// <c>Mock.Verify</c> counts.
/// </summary>
internal sealed class RecordedInvocation(
    MethodBase method, object?[] argumentsWithSelf, int argumentOffset, Delegate original) : IInvocation {
  public MethodBase Method { get; } = method;

  public object? Instance => (argumentOffset == 1) ? argumentsWithSelf[0] : null;

  public IReadOnlyList<object?> Arguments =>
    new ArraySegment<object?>(argumentsWithSelf, argumentOffset, argumentsWithSelf.Length - argumentOffset);

  /// <summary>
  /// Whether a Verify call has already matched this invocation (consumed by
  /// <c>VerifyNoOtherCalls</c>).
  /// </summary>
  internal bool Verified { get; set; }

  public void SetArgument(int index, object? value) {
    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, argumentsWithSelf.Length - argumentOffset);
    argumentsWithSelf[index + argumentOffset] = value;
  }

  public object? CallOriginal() {
    try {
      // DynamicInvoke copies by-ref parameter updates back into the array, which the
      // generated handler then writes back to the caller's variables.
      return original.DynamicInvoke(argumentsWithSelf);
    } catch (TargetInvocationException wrapped) when (wrapped.InnerException is not null) {
      ExceptionDispatchInfo.Capture(wrapped.InnerException).Throw();
      // Unreachable; satisfies definite return analysis.
      throw;
    }
  }
}
