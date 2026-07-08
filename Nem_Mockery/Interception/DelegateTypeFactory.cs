using System.Reflection;
using System.Reflection.Emit;

namespace Nem_Mockery.Interception;

/// <summary>
/// Builds delegate types for arbitrary method signatures. Needed because
/// <see cref="System.Linq.Expressions.Expression.GetDelegateType"/> cannot represent
/// <see langword="ref"/>/<see langword="out"/> parameters (including the by-ref
/// <c>this</c> of struct instance methods), which MonoMod trampolines require exactly.
/// </summary>
internal static class DelegateTypeFactory {
  private static readonly Lock _lock = new();
  private static readonly Dictionary<string, Type> _cache = [];
  private static readonly ModuleBuilder _module = AssemblyBuilder
    .DefineDynamicAssembly(new AssemblyName("Nem_Mockery.DynamicDelegates"), AssemblyBuilderAccess.Run)
    .DefineDynamicModule("Nem_Mockery.DynamicDelegates");
  private static int _nextTypeId;

  /// <summary>
  /// Returns a delegate type whose <c>Invoke</c> has exactly the given signature,
  /// creating and caching it on first use.
  /// </summary>
  /// <param name="returnType">The delegate return type (<see cref="Void"/> allowed).</param>
  /// <param name="parameterTypes">The delegate parameter types; by-ref types allowed.</param>
  /// <returns>A sealed <see cref="MulticastDelegate"/> subclass.</returns>
  internal static Type GetDelegateType(Type returnType, Type[] parameterTypes) {
    string key = BuildKey(returnType, parameterTypes);
    lock (_lock) {
      if (_cache.TryGetValue(key, out Type? cached)) {
        return cached;
      }
      Type created = Build(returnType, parameterTypes);
      _cache[key] = created;
      return created;
    }
  }

  private static Type Build(Type returnType, Type[] parameterTypes) {
    TypeBuilder builder = _module.DefineType(
      $"OrigDelegate{_nextTypeId++}",
      TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass,
      typeof(MulticastDelegate));

    ConstructorBuilder constructor = builder.DefineConstructor(
      MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
      CallingConventions.Standard,
      [typeof(object), typeof(nint)]);
    constructor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

    MethodBuilder invoke = builder.DefineMethod(
      "Invoke",
      MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
      returnType,
      parameterTypes);
    invoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

    return builder.CreateType();
  }

  private static string BuildKey(Type returnType, Type[] parameterTypes) {
    string[] names = new string[parameterTypes.Length + 1];
    names[0] = returnType.AssemblyQualifiedName ?? returnType.Name;
    for (int i = 0; i < parameterTypes.Length; i++) {
      names[i + 1] = parameterTypes[i].AssemblyQualifiedName ?? parameterTypes[i].Name;
    }
    return string.Join("|", names);
  }
}
