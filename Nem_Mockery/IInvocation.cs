using System.Reflection;

namespace Nem_Mockery;

/// <summary>
/// A single call to a mocked method, as seen by a <c>ThenAnswer</c> callback.
/// Exposes the runtime arguments, allows setting <see langword="ref"/>/<see langword="out"/>
/// parameter values, and can forward to the real implementation.
/// </summary>
public interface IInvocation {
  /// <summary>
  /// The method (or constructor) that was called.
  /// </summary>
  MethodBase Method { get; }

  /// <summary>
  /// The instance the method was called on, or <see langword="null"/> for static methods.
  /// For methods on structs this is a boxed copy of the caller's value.
  /// </summary>
  object? Instance { get; }

  /// <summary>
  /// The arguments the caller passed, in declaration order. Value types are boxed.
  /// </summary>
  IReadOnlyList<object?> Arguments { get; }

  /// <summary>
  /// Overwrites the argument at <paramref name="index"/>. For <see langword="ref"/> and
  /// <see langword="out"/> parameters the new value is written back to the caller's
  /// variable when the mocked call returns.
  /// </summary>
  /// <param name="index">The zero-based parameter position.</param>
  /// <param name="value">The value to store (boxed for value types).</param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
  void SetArgument(int index, object? value);

  /// <summary>
  /// Invokes the real (un-mocked) implementation with the current argument values and
  /// returns its result, or <see langword="null"/> for <see langword="void"/> methods.
  /// </summary>
  /// <returns>The original method's return value.</returns>
  object? CallOriginal();
}
