using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Argument-guard contracts: the ArgumentNullException / ArgumentOutOfRangeException
/// conditions declared in the XML docs of Mock, the stub builders, and IInvocation.
/// Each test asserts the specific ParamName the design names.
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_NullExpression_ThrowsArgumentNullException() {
    ArgumentNullException valueForm = Assert.Throws<ArgumentNullException>(
      () => Mock.When((System.Linq.Expressions.Expression<Func<int>>)null!));
    ArgumentNullException voidForm = Assert.Throws<ArgumentNullException>(
      () => Mock.When((System.Linq.Expressions.Expression<Action>)null!));

    Assert.Equal("call", valueForm.ParamName);
    Assert.Equal("call", voidForm.ParamName);
  }

  [Fact]
  public void WhenSet_NullArguments_ThrowArgumentNullException() {
    ArgumentNullException nullProperty = Assert.Throws<ArgumentNullException>(
      () => Mock.WhenSet(null!, () => 5));
    ArgumentNullException nullValue = Assert.Throws<ArgumentNullException>(
      () => Mock.WhenSet(() => StaticCalculator.Value, null!));

    Assert.Equal("property", nullProperty.ParamName);
    Assert.Equal("value", nullValue.ParamName);
  }

  [Fact]
  public void WhenNew_NullExpression_ThrowsArgumentNullException() {
    ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
      () => Mock.WhenNew<Widget>(null!));

    Assert.Equal("construction", thrown.ParamName);
  }

  [Fact]
  public void Verify_NullExpression_ThrowsArgumentNullException() {
    ArgumentNullException valueForm = Assert.Throws<ArgumentNullException>(
      () => Mock.Verify((System.Linq.Expressions.Expression<Func<int>>)null!));
    ArgumentNullException voidForm = Assert.Throws<ArgumentNullException>(
      () => Mock.Verify((System.Linq.Expressions.Expression<Action>)null!, Times.Once()));

    Assert.Equal("call", valueForm.ParamName);
    Assert.Equal("call", voidForm.ParamName);
  }

  [Fact]
  public void ThenThrow_NullException_ThrowsArgumentNullException() {
    using MockContext context = new();
    IStubBuilder<int> builder = Mock.When(() => StaticCalculator.Add(1, 1));

    ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
      () => builder.ThenThrow(null!));

    Assert.Equal("exception", thrown.ParamName);
  }

  [Fact]
  public void ThenAnswer_NullCallback_ThrowsArgumentNullException() {
    using MockContext context = new();
    IStubBuilder<int> builder = Mock.When(() => StaticCalculator.Add(1, 1));

    ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
      () => builder.ThenAnswer(null!));

    Assert.Equal("answer", thrown.ParamName);
  }

  [Fact]
  public void ThenReturn_NullValuesArray_ThrowsArgumentNullException() {
    using MockContext context = new();
    IStubBuilder<int> builder = Mock.When(() => StaticCalculator.Add(1, 1));

    ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(
      () => builder.ThenReturn((int[])null!));

    Assert.Equal("values", thrown.ParamName);
  }

  [Fact]
  public void SetArgument_IndexOutOfRange_ThrowsArgumentOutOfRangeException() {
    using MockContext context = new();
    Exception? negative = null;
    Exception? tooLarge = null;

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenAnswer(invocation => {
      negative = Record.Exception(() => invocation.SetArgument(-1, 0));
      tooLarge = Record.Exception(() => invocation.SetArgument(2, 0));
      return 0;
    });
    StaticCalculator.Add(1, 1);

    Assert.IsType<ArgumentOutOfRangeException>(negative);
    Assert.IsType<ArgumentOutOfRangeException>(tooLarge);
  }

  [Fact]
  public void When_GuardFailure_ContextRemainsUsable() {
    using MockContext context = new();

    Assert.Throws<ArgumentNullException>(
      () => Mock.When((System.Linq.Expressions.Expression<Func<int>>)null!));

    // The failed call must not have poisoned the context or claimed anything.
    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(100);
    Assert.Equal(100, StaticCalculator.Add(1, 1));
  }
}
