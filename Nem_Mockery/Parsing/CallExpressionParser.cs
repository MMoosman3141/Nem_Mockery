using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Nem_Mockery.Matching;

namespace Nem_Mockery.Parsing;

/// <summary>
/// Translates the lambdas passed to <c>Mock.When</c>/<c>Mock.Verify</c> into a
/// <see cref="ParsedCall"/>. The lambda body is inspected, never executed: only the
/// receiver expression and non-matcher argument expressions are evaluated, the target
/// method itself is not invoked.
/// </summary>
internal static class CallExpressionParser {
  /// <summary>
  /// Parses a method call or property read, e.g. <c>() => Foo.Bar(Arg.Any&lt;int&gt;())</c>
  /// or <c>() => Config.Timeout</c>.
  /// </summary>
  /// <exception cref="MockeryException">The lambda body is not a mockable call.</exception>
  internal static ParsedCall ParseCall(LambdaExpression expression) {
    Expression body = StripConversions(expression.Body);
    switch (body) {
      case MethodCallExpression call: {
          (object? instance, bool ignoreInstance) = ResolveReceiver(call.Object, call.Method);
          IArgumentMatcher[] matchers = BuildMatchers(call.Method, call.Arguments);
          return new ParsedCall(call.Method, instance, matchers, ignoreInstance);
        }
      case MemberExpression { Member: PropertyInfo property } member: {
          MethodInfo getter = property.GetMethod
            ?? throw new MockeryException($"Property '{property.Name}' has no getter to mock.");
          (object? instance, bool ignoreInstance) = ResolveReceiver(member.Expression, getter);
          return new ParsedCall(getter, instance, [], ignoreInstance);
        }
      case MemberExpression { Member: FieldInfo field }:
        throw new MockeryException(
          $"'{field.Name}' is a field. Fields are plain memory reads and cannot be mocked; " +
          "only methods, properties, and constructors can.");
      case NewExpression:
        throw new MockeryException(
          "Constructor calls cannot be stubbed with Mock.When; use Mock.WhenNew(() => new ...) instead.");
      default:
        throw new MockeryException(
          $"Cannot mock expression '{expression}'. The lambda body must be a single method call, " +
          "property read, or (via Mock.WhenNew) constructor call.");
    }
  }

  /// <summary>
  /// Parses a constructor call, e.g. <c>() => new Widget(Arg.Any&lt;string&gt;())</c>.
  /// </summary>
  /// <exception cref="MockeryException">The lambda body is not a constructor call.</exception>
  internal static ParsedCall ParseConstruction(LambdaExpression expression) {
    Expression body = StripConversions(expression.Body);
    if (body is not NewExpression construction) {
      throw new MockeryException(
        $"Mock.WhenNew expects a lambda whose body is a 'new' expression, got '{expression}'.");
    }
    if (construction.Constructor is null) {
      throw new MockeryException(
        $"'{construction.Type.Name}' has no invocable constructor to mock (parameterless struct " +
        "initialization does not run a constructor).");
    }
    IArgumentMatcher[] matchers = BuildMatchers(construction.Constructor, construction.Arguments);
    return new ParsedCall(construction.Constructor, null, matchers, IgnoreInstance: true);
  }

  /// <summary>
  /// Parses a property-setter arrangement: the property lambda names the property and
  /// the value lambda supplies the value matcher, e.g.
  /// <c>Mock.WhenSet(() => Config.Timeout, () => Arg.Any&lt;int&gt;())</c>.
  /// </summary>
  /// <exception cref="MockeryException">The property lambda is not a settable property.</exception>
  internal static ParsedCall ParseSetter(LambdaExpression property, LambdaExpression value) {
    Expression body = StripConversions(property.Body);
    if (body is not MemberExpression { Member: PropertyInfo propertyInfo } member) {
      throw new MockeryException(
        $"Mock.WhenSet expects a lambda whose body is a property read (naming the property), got '{property}'.");
    }
    MethodInfo setter = propertyInfo.SetMethod
      ?? throw new MockeryException($"Property '{propertyInfo.Name}' has no setter to mock.");
    (object? instance, bool ignoreInstance) = ResolveReceiver(member.Expression, setter);
    IArgumentMatcher valueMatcher = ToMatcher(StripConversions(value.Body), propertyInfo.PropertyType, isOut: false);
    return new ParsedCall(setter, instance, [valueMatcher], ignoreInstance);
  }

  private static (object? Instance, bool IgnoreInstance) ResolveReceiver(Expression? receiver, MethodBase method) {
    if (receiver is null) {
      return (null, false);
    }
    // Arg.AnyInstance<T>() in receiver position means "this arrangement applies to
    // every instance"; it is a marker, never evaluated.
    if (StripConversions(receiver) is MethodCallExpression { Method: { } marker }
        && (marker.DeclaringType == typeof(Arg))
        && (marker.Name == nameof(Arg.AnyInstance))) {
      return (null, true);
    }
    object? instance = Evaluate(receiver) ?? throw new MockeryException(
        $"The receiver of '{method.Name}' evaluated to null; a stub needs a live instance to match " +
        "against. To match every instance, use Arg.AnyInstance<T>() as the receiver.");
    return (instance, false);
  }

  private static IArgumentMatcher[] BuildMatchers(
      MethodBase method, ReadOnlyCollection<Expression> arguments) {
    ParameterInfo[] parameters = method.GetParameters();
    IArgumentMatcher[] matchers = new IArgumentMatcher[arguments.Count];
    for (int i = 0; i < arguments.Count; i++) {
      Type parameterType = parameters[i].ParameterType.IsByRef
        ? parameters[i].ParameterType.GetElementType()!
        : parameters[i].ParameterType;
      matchers[i] = ToMatcher(StripConversions(arguments[i]), parameterType, parameters[i].IsOut);
    }
    return matchers;
  }

  private static IArgumentMatcher ToMatcher(Expression argument, Type parameterType, bool isOut) {
    // The incoming value of an out parameter is meaningless, so it always matches.
    if (isOut) {
      return new AnyMatcher(parameterType);
    }
    if (argument is MethodCallExpression { Method.DeclaringType: not null } call
        && (call.Method.DeclaringType == typeof(Arg))) {
      Type matchedType = call.Method.GetGenericArguments()[0];
      if (call.Method.Name == nameof(Arg.AnyInstance)) {
        throw new MockeryException(
          "Arg.AnyInstance marks the receiver of a call (Arg.AnyInstance<T>().Method(...)); it " +
          "cannot be used as an argument. Use Arg.Any<T>() to match any argument value.");
      }
      if (call.Method.Name == nameof(Arg.Any)) {
        return new AnyMatcher(matchedType);
      }
      if (call.Method.Name == nameof(Arg.Is)) {
        Expression predicateExpression = StripConversions(call.Arguments[0]);
        Delegate predicate = predicateExpression is LambdaExpression lambda
          ? lambda.Compile()
          : (Delegate)Evaluate(predicateExpression)!;
        return new PredicateMatcher(matchedType, predicate);
      }
    }
    return new ValueMatcher(Evaluate(argument));
  }

  private static object? Evaluate(Expression expression) {
    if (expression is ConstantExpression constant) {
      return constant.Value;
    }
    // General case: compile the sub-expression and run it. Slow, but stubbing happens
    // once per test, not per mocked call.
    return Expression.Lambda(expression).Compile().DynamicInvoke();
  }

  private static Expression StripConversions(Expression expression) {
    Expression current = expression;
    while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary) {
      current = unary.Operand;
    }
    return current;
  }
}
