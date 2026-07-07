namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A static class with no seams at all — the classic "unmockable" dependency that
/// Nem_Mockery exists to mock.
/// </summary>
public static class StaticCalculator {
  /// <summary>
  /// A static property with a real getter and setter.
  /// </summary>
  public static int Value { get; set; }

  /// <summary>
  /// Returns the real sum; stubs replace this with canned values.
  /// </summary>
  public static int Add(int left, int right) {
    return left + right;
  }

  /// <summary>
  /// Returns a marker string proving the real implementation ran.
  /// </summary>
  public static string Describe(string input) {
    return $"real:{input}";
  }

  /// <summary>
  /// A void method whose real body has an observable side effect on <see cref="Value"/>.
  /// </summary>
  public static void Increment() {
    Value++;
  }

  /// <summary>
  /// A method with an out parameter: really parses, returning false for non-positive.
  /// </summary>
  public static bool TryParsePositive(string text, out int number) {
    bool parsed = int.TryParse(text, out number);
    return parsed && (number > 0);
  }

  /// <summary>
  /// Returns the parsed number, or null when the text is not a number — a nullable
  /// value-type return.
  /// </summary>
  public static int? FindOrNull(string text) {
    return int.TryParse(text, out int number) ? number : null;
  }

  /// <summary>
  /// A method with enough parameters to push argument marshaling past the short-form
  /// IL opcodes.
  /// </summary>
  public static string Concat(
      string first, string second, string third, string fourth, string fifth,
      string sixth, string seventh, string eighth, string ninth, string tenth) {
    return string.Join("+", first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth);
  }
}
