using System.Reflection;

namespace Nem_Mockery.Interception;

/// <summary>
/// The process-wide table of every method that has ever been mocked. Entries are kept
/// alive for the process lifetime — MonoMod undoes a hook when its
/// <see cref="MonoMod.RuntimeDetour.Hook"/> object is collected, so the registry's
/// strong references are what keep applied detours stable.
/// </summary>
internal static class MockRegistry {
  private static readonly Lock s_lock = new();
  private static readonly Dictionary<MethodBase, MethodMock> s_byMethod = [];
  private static readonly List<MethodMock> s_byId = [];

  /// <summary>
  /// Returns the existing mock for <paramref name="method"/> or creates one (building
  /// its handler and hook) on first use.
  /// </summary>
  internal static MethodMock GetOrCreate(MethodBase method) {
    lock (s_lock) {
      if (s_byMethod.TryGetValue(method, out MethodMock? existing)) {
        return existing;
      }
      MethodMock created = new(s_byId.Count, method);
      s_byId.Add(created);
      s_byMethod[method] = created;
      return created;
    }
  }

  /// <summary>
  /// Returns the mock for <paramref name="method"/>, or <see langword="null"/> if the
  /// method has never been mocked.
  /// </summary>
  internal static MethodMock? Find(MethodBase method) {
    lock (s_lock) {
      return s_byMethod.GetValueOrDefault(method);
    }
  }

  /// <summary>
  /// Resolves the id a generated handler embedded back to its mock.
  /// </summary>
  internal static MethodMock GetById(int id) {
    lock (s_lock) {
      return s_byId[id];
    }
  }

  /// <summary>
  /// Returns every registered mock (used by <c>VerifyNoOtherCalls</c>).
  /// </summary>
  internal static IReadOnlyList<MethodMock> GetAll() {
    lock (s_lock) {
      return [.. s_byId];
    }
  }
}
