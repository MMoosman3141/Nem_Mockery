namespace Nem_Mockery;

/// <summary>
/// Describes how many times a mocked call is expected to have happened, for use with
/// the <c>Mock.Verify*</c> methods. Create instances through the static factory
/// methods; the default value of the struct is equivalent to <see cref="Never"/>.
/// </summary>
public readonly struct Times {
  private readonly int _minimum;
  private readonly int _maximum;
  private readonly string? _description;

  private Times(int minimum, int maximum, string description) {
    _minimum = minimum;
    _maximum = maximum;
    _description = description;
  }

  /// <summary>
  /// The call must have happened exactly one time. This is the default used by the
  /// <c>Verify</c> overloads that take no <see cref="Times"/> argument.
  /// </summary>
  /// <returns>A <see cref="Times"/> requiring exactly one call.</returns>
  public static Times Once() {
    return new Times(1, 1, "exactly once");
  }

  /// <summary>
  /// The call must never have happened.
  /// </summary>
  /// <returns>A <see cref="Times"/> requiring zero calls.</returns>
  public static Times Never() {
    return new Times(0, 0, "never");
  }

  /// <summary>
  /// The call must have happened exactly <paramref name="count"/> times.
  /// </summary>
  /// <param name="count">The exact number of expected calls; must not be negative.</param>
  /// <returns>A <see cref="Times"/> requiring exactly <paramref name="count"/> calls.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
  public static Times Exactly(int count) {
    ArgumentOutOfRangeException.ThrowIfNegative(count);
    return new Times(count, count, $"exactly {count} time(s)");
  }

  /// <summary>
  /// The call must have happened <paramref name="count"/> or more times.
  /// </summary>
  /// <param name="count">The minimum number of expected calls; must not be negative.</param>
  /// <returns>A <see cref="Times"/> requiring at least <paramref name="count"/> calls.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
  public static Times AtLeast(int count) {
    ArgumentOutOfRangeException.ThrowIfNegative(count);
    return new Times(count, int.MaxValue, $"at least {count} time(s)");
  }

  /// <summary>
  /// The call must have happened one or more times.
  /// </summary>
  /// <returns>A <see cref="Times"/> requiring at least one call.</returns>
  public static Times AtLeastOnce() {
    return new Times(1, int.MaxValue, "at least once");
  }

  /// <summary>
  /// The call must have happened <paramref name="count"/> or fewer times (including zero).
  /// </summary>
  /// <param name="count">The maximum number of allowed calls; must not be negative.</param>
  /// <returns>A <see cref="Times"/> allowing at most <paramref name="count"/> calls.</returns>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
  public static Times AtMost(int count) {
    ArgumentOutOfRangeException.ThrowIfNegative(count);
    return new Times(0, count, $"at most {count} time(s)");
  }

  /// <summary>
  /// Returns whether an actual call count satisfies this expectation.
  /// </summary>
  /// <param name="actualCount">The number of matching calls that were recorded.</param>
  /// <returns><see langword="true"/> when the count is within range.</returns>
  public bool IsSatisfiedBy(int actualCount) {
    return (actualCount >= _minimum) && (actualCount <= _maximum);
  }

  /// <summary>
  /// Returns a human-readable description such as "exactly once" or "at least 3 time(s)".
  /// </summary>
  /// <returns>The description used in verification failure messages.</returns>
  public override string ToString() {
    return _description ?? "never";
  }
}
