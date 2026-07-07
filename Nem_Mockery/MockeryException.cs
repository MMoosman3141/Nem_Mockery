namespace Nem_Mockery;

/// <summary>
/// The exception thrown when Nem_Mockery cannot set up, match, or tear down a mock —
/// for example when no <see cref="MockContext"/> is active, when an expression cannot
/// be translated into a mockable method, or when a method cannot be detoured.
/// </summary>
public class MockeryException : Exception {
  /// <summary>
  /// Creates the exception with a message describing what went wrong.
  /// </summary>
  /// <param name="message">A description of the setup or teardown failure.</param>
  public MockeryException(string message) : base(message) {
  }

  /// <summary>
  /// Creates the exception with a message and the underlying cause.
  /// </summary>
  /// <param name="message">A description of the setup or teardown failure.</param>
  /// <param name="innerException">The underlying failure.</param>
  public MockeryException(string message, Exception innerException) : base(message, innerException) {
  }
}
