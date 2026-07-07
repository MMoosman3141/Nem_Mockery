using System.Diagnostics.CodeAnalysis;

namespace Nem_Mockery;

/// <summary>
/// Argument matchers for use inside the lambda passed to <c>Mock.When</c> and
/// <c>Mock.Verify</c>. A matcher call marks that argument position as flexible instead
/// of requiring an exact value.
/// </summary>
/// <remarks>
/// These methods are markers: they are recognized syntactically while the stubbing
/// expression is parsed and are never really executed as part of a mocked call.
/// Calling them outside a <c>Mock.When</c>/<c>Mock.Verify</c> expression just returns
/// <see langword="default"/> and matches nothing.
/// </remarks>
/// <example>
/// <code>
/// Mock.When(() => FileReader.Read(Arg.Any&lt;string&gt;())).ThenReturn("data");
/// Mock.Verify(() => FileReader.Read(Arg.Is&lt;string&gt;(p => p.EndsWith(".txt"))), Times.Once());
/// </code>
/// </example>
public static class Arg {
  /// <summary>
  /// Matches any value of <typeparamref name="T"/>, including <see langword="null"/>.
  /// </summary>
  /// <typeparam name="T">The declared type of the parameter being matched.</typeparam>
  /// <returns><see langword="default"/>; the value is never used.</returns>
  public static T Any<T>() {
    return default!;
  }

  /// <summary>
  /// Matches any value of <typeparamref name="T"/> for which <paramref name="predicate"/>
  /// returns <see langword="true"/>. A <see langword="null"/> argument never matches and
  /// is never passed to the predicate; match null with an exact <see langword="null"/>
  /// argument instead.
  /// </summary>
  /// <typeparam name="T">The declared type of the parameter being matched.</typeparam>
  /// <param name="predicate">The condition an actual argument must satisfy.</param>
  /// <returns><see langword="default"/>; the value is never used.</returns>
  [SuppressMessage("Style", "IDE0060:Remove unused parameter",
    Justification = "The predicate is consumed by CallExpressionParser, which extracts and compiles it " +
      "from the When/Verify expression tree; this body only runs outside an expression, where there is " +
      "no call to match.")]
  public static T Is<T>(Func<T, bool> predicate) {
    return default!;
  }
}
