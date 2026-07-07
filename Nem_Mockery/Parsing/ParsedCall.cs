using System.Reflection;
using Nem_Mockery.Matching;

namespace Nem_Mockery.Parsing;

/// <summary>
/// The result of parsing a stubbing or verification lambda: which method it names,
/// which receiver it was written against (<see langword="null"/> for statics and
/// constructors), and one matcher per parameter. <paramref name="IgnoreInstance"/>
/// is set for constructors, whose receiver is always a freshly allocated object that
/// no expression could have named in advance.
/// </summary>
internal sealed record ParsedCall(
  MethodBase Method,
  object? Instance,
  IReadOnlyList<IArgumentMatcher> Matchers,
  bool IgnoreInstance = false);
