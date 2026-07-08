namespace xUnit_Nem_Mockery.TestSupport;

/// <summary>
/// A static async method, proving Task-returning members stub like any other.
/// </summary>
public static class AsyncFetcher {
  /// <summary>
  /// Really awaits and returns a marker string proving the real implementation ran.
  /// </summary>
  public static async Task<string> FetchAsync(string key) {
    await Task.Delay(1);
    return $"real:{key}";
  }
}
