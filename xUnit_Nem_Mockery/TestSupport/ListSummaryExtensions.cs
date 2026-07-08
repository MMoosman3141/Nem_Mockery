namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// An extension method — a static method whose receiver arrives as the first
/// argument, which the parser must treat as an exact-instance matcher.
/// </summary>
public static class ListSummaryExtensions {
  /// <summary>
  /// Returns a marker string proving the real implementation ran.
  /// </summary>
  public static string Summarize(this List<int> source) {
    return $"real:{source.Count}";
  }
}
