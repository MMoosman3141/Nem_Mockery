namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A generic method, for pinning which instantiations can be detoured: value-type
/// instantiations get their own compiled code; reference-type ones share it.
/// </summary>
public static class GenericEcho {
  /// <summary>
  /// Really returns the value unchanged.
  /// </summary>
  public static T Echo<T>(T value) {
    return value;
  }
}
