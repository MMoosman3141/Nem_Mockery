namespace Nem_Mockery.Matching;

/// <summary>
/// Shared matching rules used both when a stub is selected during dispatch and when
/// recorded invocations are counted during verification.
/// </summary>
internal static class InvocationMatcher {
  /// <summary>
  /// Returns whether the actual receiver of a call matches the receiver a stub or
  /// verification was written against. Reference types match by identity; boxed value
  /// types (struct receivers) match by value equality, because every box is a copy.
  /// </summary>
  internal static bool InstanceMatches(object? expected, object? actual) {
    if (expected is null && actual is null) {
      return true;
    }
    if (expected is null || actual is null) {
      return false;
    }
    if (expected is ValueType) {
      return expected.Equals(actual);
    }
    return ReferenceEquals(expected, actual);
  }

  /// <summary>
  /// Returns whether every actual argument satisfies the matcher at its position.
  /// </summary>
  internal static bool ArgumentsMatch(IReadOnlyList<IArgumentMatcher> matchers, IReadOnlyList<object?> arguments) {
    if (matchers.Count != arguments.Count) {
      return false;
    }
    for (int i = 0; i < matchers.Count; i++) {
      if (!matchers[i].Matches(arguments[i])) {
        return false;
      }
    }
    return true;
  }
}
