using System.Reflection;

namespace Nem_Mockery.Interception;

/// <summary>
/// The single entry point every generated handler calls into. Generated IL cannot
/// conveniently embed object references, so handlers pass the integer id of their
/// <see cref="MethodMock"/> and this class routes to it.
/// </summary>
internal static class MockDispatcher {
  /// <summary>
  /// The reflection handle the IL generator emits a <c>call</c> to.
  /// </summary>
  internal static MethodInfo DispatchMethod { get; } = typeof(MockDispatcher).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!;

  /// <summary>
  /// Handles one intercepted call: looks up the mock and lets it answer.
  /// </summary>
  /// <param name="mockId">The registry id embedded in the generated handler.</param>
  /// <param name="original">The MonoMod trampoline to the real implementation.</param>
  /// <param name="argumentsWithSelf">
  /// All boxed call values: the receiver first for instance methods, then the
  /// arguments in order. Element updates flow back to by-ref parameters.
  /// </param>
  /// <returns>The boxed return value, or <see langword="null"/> for void.</returns>
  private static object? Dispatch(int mockId, Delegate original, object?[] argumentsWithSelf) {
    return MockRegistry.GetById(mockId).Dispatch(original, argumentsWithSelf);
  }
}
