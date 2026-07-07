using System.Reflection;
using System.Reflection.Emit;
using MonoMod.RuntimeDetour;
using Nem_Mockery.Matching;
using Nem_Mockery.Stubbing;

namespace Nem_Mockery.Interception;

/// <summary>
/// Everything the library knows about one mocked method: the MonoMod hook that
/// detours it, the stubs currently arranged on it, the invocations recorded while it
/// is mocked, and the ownership gate that serializes parallel tests wanting to mock
/// the same method.
/// </summary>
/// <remarks>
/// Detours are process-global, so a <see cref="MethodMock"/> is claimed by the first
/// <see cref="MockContext"/> that stubs its method. Contexts nested in the same
/// async flow share the claim; a context on an unrelated flow blocks in
/// <see cref="Claim"/> until the owning context disposes. The hook is applied while
/// at least one owner is alive and undone when the last owner releases.
/// </remarks>
internal sealed class MethodMock {
  private const int ClaimTimeoutMilliseconds = 30000;

  private readonly SemaphoreSlim _gate = new(1, 1);
  private readonly Lock _stateLock = new();
  private readonly List<MockContext> _owners = [];
  private readonly List<Stub> _stubs = [];
  private readonly List<RecordedInvocation> _invocations = [];
  private readonly Hook _hook;

  internal MethodMock(int id, MethodBase method) {
    ValidateMockable(method);
    Id = id;
    Method = method;
    HasThis = !method.IsStatic;
    ReturnType = (method is MethodInfo info) ? info.ReturnType : typeof(void);
    DynamicMethod handler = HandlerFactory.Create(method, id);
    try {
      _hook = new Hook(method, handler, applyByDefault: false);
    } catch (Exception ex) {
      throw new MockeryException(
        $"Could not create a detour for '{Describe(method)}'. The runtime may not support " +
        "detouring this method (very small methods can be inlined; intrinsics cannot be hooked).", ex);
    }
  }

  internal int Id { get; }

  internal MethodBase Method { get; }

  internal bool HasThis { get; }

  internal Type ReturnType { get; }

  /// <summary>
  /// Gives <paramref name="context"/> shared or exclusive ownership of this mock.
  /// Blocks when an unrelated context currently owns the method.
  /// </summary>
  /// <exception cref="MockeryException">The wait for ownership timed out.</exception>
  internal void Claim(MockContext context) {
    lock (_stateLock) {
      if (IsOwnedByChainOf(context)) {
        _owners.Add(context);
        return;
      }
    }
    if (!_gate.Wait(ClaimTimeoutMilliseconds)) {
      throw new MockeryException(
        $"Timed out waiting to mock '{Describe(Method)}': another MockContext still owns it. " +
        "A test probably failed to dispose its MockContext, or two parallel tests are mocking " +
        "overlapping methods in opposite orders.");
    }
    lock (_stateLock) {
      _owners.Add(context);
      _hook.Apply();
    }
  }

  /// <summary>
  /// Removes <paramref name="context"/>'s stubs and ownership; when the last owner
  /// releases, the detour is undone and recorded invocations are cleared.
  /// </summary>
  internal void Release(MockContext context) {
    bool last = false;
    lock (_stateLock) {
      _stubs.RemoveAll(stub => ReferenceEquals(stub.Owner, context));
      if (!_owners.Remove(context)) {
        return;
      }
      if (_owners.Count == 0) {
        last = true;
        _invocations.Clear();
        _hook.Undo();
      }
    }
    if (last) {
      _gate.Release();
    }
  }

  internal void AddStub(Stub stub) {
    lock (_stateLock) {
      _stubs.Add(stub);
    }
  }

  internal bool IsClaimedByChainOf(MockContext context) {
    lock (_stateLock) {
      return IsOwnedByChainOf(context);
    }
  }

  /// <summary>
  /// Answers one intercepted call: records it, finds the newest matching stub, and
  /// either runs its next behavior or falls through to the real implementation.
  /// </summary>
  internal object? Dispatch(Delegate original, object?[] argumentsWithSelf) {
    RecordedInvocation invocation = new(Method, argumentsWithSelf, HasThis ? 1 : 0, original);
    Stub[] stubs;
    lock (_stateLock) {
      _invocations.Add(invocation);
      stubs = [.. _stubs];
    }

    // Newest stub wins, matching Mockito's "later stubbing overrides earlier" rule.
    // Matching runs outside the lock because Arg.Is predicates are user code.
    Stub? match = null;
    for (int i = stubs.Length - 1; i >= 0; i--) {
      if (stubs[i].Matches(invocation.Instance, invocation.Arguments)) {
        match = stubs[i];
        break;
      }
    }

    object? result = (match is null) ? invocation.CallOriginal() : match.Execute(invocation);
    return NormalizeResult(result);
  }

  /// <summary>
  /// Counts recorded invocations matching a verification expression, optionally
  /// marking them consumed for <c>VerifyNoOtherCalls</c>.
  /// </summary>
  internal int CountMatchingInvocations(
      object? instance, IReadOnlyList<IArgumentMatcher> matchers, bool markVerified, bool ignoreInstance) {
    RecordedInvocation[] snapshot;
    lock (_stateLock) {
      snapshot = [.. _invocations];
    }
    int count = 0;
    foreach (RecordedInvocation invocation in snapshot) {
      if ((ignoreInstance || InvocationMatcher.InstanceMatches(instance, invocation.Instance))
          && InvocationMatcher.ArgumentsMatch(matchers, invocation.Arguments)) {
        count++;
        if (markVerified) {
          invocation.Verified = true;
        }
      }
    }
    return count;
  }

  /// <summary>
  /// Returns recorded invocations not yet consumed by a Verify call.
  /// </summary>
  internal IReadOnlyList<RecordedInvocation> GetUnverifiedInvocations() {
    lock (_stateLock) {
      return [.. _invocations.Where(invocation => !invocation.Verified)];
    }
  }

  /// <summary>
  /// Returns all recorded invocations, for verification failure messages.
  /// </summary>
  internal IReadOnlyList<RecordedInvocation> GetInvocations() {
    lock (_stateLock) {
      return [.. _invocations];
    }
  }

  internal static string Describe(MethodBase method) {
    string typeName = method.DeclaringType?.Name ?? "?";
    string methodName = method.IsConstructor ? $"new {typeName}" : $"{typeName}.{method.Name}";
    string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
    return $"{methodName}({parameters})";
  }

  private bool IsOwnedByChainOf(MockContext context) {
    for (MockContext? candidate = context; candidate is not null; candidate = candidate.Parent) {
      if (_owners.Contains(candidate)) {
        return true;
      }
    }
    return false;
  }

  private object? NormalizeResult(object? result) {
    // A stub can legitimately produce null for a value-type return (e.g. an Answer
    // callback returning default); unboxing null would crash in the handler, so
    // substitute a boxed default. Nullable<T> keeps null, which unboxes correctly.
    if (result is null
        && ReturnType.IsValueType
        && (ReturnType != typeof(void))
        && (Nullable.GetUnderlyingType(ReturnType) is null)) {
      return Activator.CreateInstance(ReturnType);
    }
    return result;
  }

  private static void ValidateMockable(MethodBase method) {
    if (method.IsAbstract) {
      throw new MockeryException(
        $"'{Describe(method)}' is abstract; there is no method body to detour. Mock a concrete implementation.");
    }
    if (method.ContainsGenericParameters) {
      throw new MockeryException(
        $"'{Describe(method)}' has open generic parameters; mock a closed instantiation instead.");
    }
    if (method is MethodInfo { ReturnType.IsByRef: true }) {
      throw new MockeryException(
        $"'{Describe(method)}' returns by reference; ref-returning methods are not supported.");
    }
  }
}
