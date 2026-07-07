namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A sealed class with non-virtual members — unmockable by proxy-based libraries.
/// </summary>
public sealed class SealedGreeter(string name) {
  /// <summary>
  /// An instance property with a real getter and setter.
  /// </summary>
  public string Name { get; set; } = name;

  /// <summary>
  /// Returns a real greeting; stubs replace this per instance.
  /// </summary>
  public string Greet(string whom) {
    return $"Hello {whom}, I am {Name}.";
  }
}
