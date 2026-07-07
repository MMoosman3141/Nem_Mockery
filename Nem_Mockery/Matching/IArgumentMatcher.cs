namespace Nem_Mockery.Matching;

/// <summary>
/// Decides whether one actual argument satisfies one position of a stubbed or
/// verified call expression.
/// </summary>
internal interface IArgumentMatcher {
  /// <summary>
  /// Returns whether <paramref name="actual"/> satisfies this matcher.
  /// </summary>
  /// <param name="actual">The boxed runtime argument.</param>
  /// <returns><see langword="true"/> on a match.</returns>
  bool Matches(object? actual);
}
