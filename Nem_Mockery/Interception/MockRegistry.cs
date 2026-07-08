using System.Reflection;

namespace Nem_Mockery.Interception;

/// <summary>
/// The process-wide table of every method that has ever been mocked. Entries are kept
/// alive for the process lifetime — MonoMod undoes a hook when its
/// <see cref="MonoMod.RuntimeDetour.Hook"/> object is collected, so the registry's
/// strong references are what keep applied detours stable.
/// </summary>
internal static class MockRegistry {
  private static readonly Lock _lock = new();
  private static readonly Dictionary<MethodBase, MethodMock> _byMethod = [];
  private static readonly List<MethodMock> _byId = [];

  /// <summary>
  /// Returns the existing mock for <paramref name="method"/> or creates one (building
  /// its handler and hook) on first use.
  /// </summary>
  internal static MethodMock GetOrCreate(MethodBase method) {
    lock (_lock) {
      if (_byMethod.TryGetValue(method, out MethodMock? existing)) {
        return existing;
      }
      MethodMock created = new(_byId.Count, method);
      _byId.Add(created);
      _byMethod[method] = created;
      return created;
    }
  }

  /// <summary>
  /// Returns the mock for <paramref name="method"/>, or <see langword="null"/> if the
  /// method has never been mocked.
  /// </summary>
  internal static MethodMock? Find(MethodBase method) {
    lock (_lock) {
      return _byMethod.GetValueOrDefault(method);
    }
  }

  /// <summary>
  /// Resolves the id a generated handler embedded back to its mock.
  /// </summary>
  internal static MethodMock GetById(int id) {
    lock (_lock) {
      return _byId[id];
    }
  }

  /// <summary>
  /// Returns every registered mock (used by <c>VerifyNoOtherCalls</c>).
  /// </summary>
  internal static IReadOnlyList<MethodMock> GetAll() {
    lock (_lock) {
      return [.. _byId];
    }
  }
}
