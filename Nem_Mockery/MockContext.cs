using Nem_Mockery.Interception;

namespace Nem_Mockery;

/// <summary>
/// The scope all stubbing and verification happens in. Create one at the start of a
/// test (typically with a <see langword="using"/> declaration); disposing it removes
/// every detour and stub the test arranged, restoring the real behavior of all
/// mocked methods.
/// </summary>
/// <remarks>
/// <para>
/// Detours are process-global, so while one context has a method mocked, every call
/// to that method anywhere in the process is intercepted. To keep parallel test runs
/// safe, the first context to stub a method owns it; a context in another test that
/// stubs the same method blocks until the owner disposes. Contexts nested in the same
/// async flow share ownership.
/// </para>
/// <para>
/// Always dispose the context — a leaked context keeps its methods detoured and keeps
/// other tests waiting. The <see langword="using"/> declaration form makes this
/// automatic:
/// </para>
/// <code>
/// using MockContext context = new();
/// Mock.When(() => FileReader.Read(Arg.Any&lt;string&gt;())).ThenReturn("data");
/// </code>
/// </remarks>
public sealed class MockContext : IDisposable {
  private static readonly AsyncLocal<MockContext?> s_current = new();
  private readonly List<MethodMock> _claimedMocks = [];

  /// <summary>
  /// Opens a new mocking scope and makes it the ambient context for the current
  /// async flow. Nested contexts are allowed; disposing the inner one restores
  /// the outer.
  /// </summary>
  public MockContext() {
    Parent = s_current.Value;
    s_current.Value = this;
  }

  /// <summary>
  /// Whether this context has been disposed. Stubs owned by a disposed context
  /// never match.
  /// </summary>
  public bool IsDisposed { get; private set; }

  internal static MockContext? Current => s_current.Value;

  internal MockContext? Parent { get; }

  internal static MockContext RequireCurrent() {
    MockContext? current = s_current.Value;
    if (current is null || current.IsDisposed) {
      throw new MockeryException(
        "No active MockContext. Wrap the test body in 'using MockContext context = new();' " +
        "before calling Mock.When or Mock.Verify.");
    }
    return current;
  }

  internal void Claim(MethodMock mock) {
    ObjectDisposedException.ThrowIf(IsDisposed, this);
    if (_claimedMocks.Contains(mock)) {
      return;
    }
    mock.Claim(this);
    _claimedMocks.Add(mock);
  }

  /// <summary>
  /// Removes every stub this context arranged, releases ownership of the mocked
  /// methods (undoing their detours when this was the last owner), and restores the
  /// previous ambient context.
  /// </summary>
  public void Dispose() {
    if (IsDisposed) {
      return;
    }
    IsDisposed = true;
    foreach (MethodMock mock in _claimedMocks) {
      mock.Release(this);
    }
    _claimedMocks.Clear();
    if (ReferenceEquals(s_current.Value, this)) {
      s_current.Value = Parent;
    }
  }
}
