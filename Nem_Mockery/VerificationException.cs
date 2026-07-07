namespace Nem_Mockery;

/// <summary>
/// The exception thrown when a <see cref="Mock.Verify{TResult}(System.Linq.Expressions.Expression{System.Func{TResult}})"/>
/// call (or any other verification) fails: the mocked method was not called the expected
/// number of times, or unverified calls remain when
/// <see cref="Mock.VerifyNoOtherCalls"/> runs.
/// </summary>
/// <remarks>
/// Creates the exception with a message describing the expected and actual calls.
/// </remarks>
/// <param name="message">A description of the verification failure.</param>
public sealed class VerificationException(string message) : MockeryException(message) {
}
