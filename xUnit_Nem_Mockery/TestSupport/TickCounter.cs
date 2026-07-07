namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A mutable struct: instance methods take <c>this</c> by reference, exercising the
/// hardest interception path (by-ref receiver with write-back).
/// </summary>
public struct TickCounter {
  /// <summary>
  /// The current count; mutated by <see cref="Next"/>.
  /// </summary>
  public int Count;

  /// <summary>
  /// Really increments the count and returns the new value.
  /// </summary>
  public int Next() {
    Count++;
    return Count;
  }
}
