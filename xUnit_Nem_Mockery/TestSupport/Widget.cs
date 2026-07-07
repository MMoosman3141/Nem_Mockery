namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A class whose constructor has an observable effect, for constructor mocking tests.
/// </summary>
/// <remarks>
/// Creates the widget; the real body marks the name so tests can tell whether it ran.
/// </remarks>
public sealed class Widget(string name) {

  /// <summary>
  /// The name assigned by the constructor; stays null when the body is skipped.
  /// </summary>
  public string? Name { get; } = $"real:{name}";
}
