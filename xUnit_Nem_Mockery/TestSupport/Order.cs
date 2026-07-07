namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A sealed record — positional, immutable, and closed to inheritance.
/// </summary>
public sealed record Order(string Sku, int Quantity) {
  /// <summary>
  /// Returns the real line total for a unit price.
  /// </summary>
  public decimal Total(decimal unitPrice) {
    return Quantity * unitPrice;
  }
}
