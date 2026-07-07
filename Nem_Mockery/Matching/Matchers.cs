namespace Nem_Mockery.Matching;

/// <summary>
/// Matches any argument value, including <see langword="null"/>. Produced by
/// <see cref="Arg.Any{T}"/> and by <see langword="out"/> parameters (whose incoming
/// value is meaningless).
/// </summary>
internal sealed class AnyMatcher(Type parameterType) : IArgumentMatcher {
  public bool Matches(object? actual) {
    return true;
  }

  public override string ToString() {
    return $"any {parameterType.Name}";
  }
}

/// <summary>
/// Matches an argument by running the user's predicate. Produced by <see cref="Arg.Is{T}"/>.
/// Null arguments never match and never reach the predicate, so predicates only need
/// to handle real values; match null with an exact null argument instead.
/// </summary>
internal sealed class PredicateMatcher(Type parameterType, Delegate predicate) : IArgumentMatcher {
  public bool Matches(object? actual) {
    if (actual is null || !parameterType.IsInstanceOfType(actual)) {
      return false;
    }
    return (bool)predicate.DynamicInvoke(actual)!;
  }

  public override string ToString() {
    return $"{parameterType.Name} matching predicate";
  }
}

/// <summary>
/// Matches an argument by equality with a fixed expected value (the default when a
/// stubbing expression uses a plain value instead of an <see cref="Arg"/> matcher).
/// </summary>
internal sealed class ValueMatcher(object? expected) : IArgumentMatcher {
  public bool Matches(object? actual) {
    return Equals(expected, actual);
  }

  public override string ToString() {
    return expected?.ToString() ?? "null";
  }
}
