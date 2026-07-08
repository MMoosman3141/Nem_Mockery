using System.Reflection;

namespace Nem_Mockery.Interception;

/// <summary>
/// Renders methods and calls for diagnostics. Property accessors are shown in
/// property syntax (<c>Config.Timeout</c>, <c>Config.Timeout = 5</c>, indexers as
/// <c>Cache[key]</c>) instead of their compiler names (<c>get_Timeout</c>,
/// <c>set_Timeout</c>), and constructors as <c>new Widget(...)</c>.
/// </summary>
internal static class CallFormatter {
  /// <summary>
  /// Formats a method with the given argument renderings — matcher descriptions when
  /// formatting an expectation, actual values when formatting a recorded call.
  /// </summary>
  internal static string Format(MethodBase method, IReadOnlyList<string> arguments) {
    string typeName = method.DeclaringType?.Name ?? "?";
    if (method.IsConstructor) {
      return $"new {typeName}({string.Join(", ", arguments)})";
    }
    if (method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal)) {
      string propertyName = method.Name[4..];
      if (arguments.Count == 0) {
        return $"{typeName}.{propertyName}";
      }
      if (propertyName == "Item") {
        return $"{typeName}[{string.Join(", ", arguments)}]";
      }
    }
    if (method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal)) {
      string propertyName = method.Name[4..];
      if (arguments.Count == 1) {
        return $"{typeName}.{propertyName} = {arguments[0]}";
      }
      if (propertyName == "Item") {
        return $"{typeName}[{string.Join(", ", arguments.Take(arguments.Count - 1))}] = {arguments[^1]}";
      }
    }
    return $"{typeName}.{method.Name}({string.Join(", ", arguments)})";
  }

  /// <summary>
  /// Formats a method with its parameter types, for messages about the method itself
  /// rather than a particular call.
  /// </summary>
  internal static string FormatSignature(MethodBase method) {
    string[] parameterTypes = [.. method.GetParameters().Select(p => p.ParameterType.Name)];
    return Format(method, parameterTypes);
  }
}
