using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// Verification: Times expectations, matcher-based counting (including calls that
/// fell through to real code), failure diagnostics, and VerifyNoOtherCalls.
/// </summary>
public partial class MockTests {
  [Fact]
  public void Verify_DefaultTimes_PassesOnExactlyOneCall() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(0);
    StaticCalculator.Add(1, 1);

    Mock.Verify(() => StaticCalculator.Add(1, 1));
  }

  [Fact]
  public void Verify_DefaultTimes_FailsOnZeroCalls() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(0);

    Assert.Throws<VerificationException>(() => Mock.Verify(() => StaticCalculator.Add(1, 1)));
  }

  [Fact]
  public void Verify_WithMatchers_CountsAcrossArgumentValues() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>())).ThenReturn(0);
    StaticCalculator.Add(1, 1);
    StaticCalculator.Add(2, 2);
    StaticCalculator.Add(3, 3);

    Mock.Verify(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>()), Times.Exactly(3));
    Mock.Verify(() => StaticCalculator.Add(2, 2), Times.Once());
    Mock.Verify(() => StaticCalculator.Add(9, 9), Times.Never());
    Mock.Verify(() => StaticCalculator.Add(Arg.Is<int>(n => n >= 2), Arg.Any<int>()), Times.AtLeast(2));
  }

  [Fact]
  public void Verify_CountsCallsThatFellThroughToRealCode() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(0);
    int real = StaticCalculator.Add(5, 5);

    Assert.Equal(10, real);
    Mock.Verify(() => StaticCalculator.Add(5, 5), Times.Once());
  }

  [Fact]
  public void Verify_TooManyCalls_ThrowsWithDiagnostics() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(0);
    StaticCalculator.Add(1, 1);
    StaticCalculator.Add(1, 1);

    VerificationException thrown = Assert.Throws<VerificationException>(
      () => Mock.Verify(() => StaticCalculator.Add(1, 1), Times.Once()));

    Assert.Contains("exactly once", thrown.Message);
    Assert.Contains("2 time(s)", thrown.Message);
    Assert.Contains("Add(1, 1)", thrown.Message);
  }

  [Fact]
  public void Verify_NeverMockedMethod_ThrowsMockeryException() {
    using MockContext context = new();

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.Verify(() => StaticCalculator.Describe("x"), Times.Never()));

    Assert.Contains("not mocked", thrown.Message);
  }

  [Fact]
  public void Verify_NoRecordedCalls_MessageSaysSo() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(1, 1)).ThenReturn(0);

    VerificationException thrown = Assert.Throws<VerificationException>(
      () => Mock.Verify(() => StaticCalculator.Add(1, 1), Times.AtLeastOnce()));

    Assert.Contains("No calls were recorded", thrown.Message);
  }

  [Fact]
  public void VerifyNoOtherCalls_AllCallsVerified_Passes() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>())).ThenReturn(0);
    StaticCalculator.Add(1, 1);
    StaticCalculator.Add(2, 2);

    Mock.Verify(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>()), Times.Exactly(2));
    Mock.VerifyNoOtherCalls();
  }

  [Fact]
  public void VerifyNoOtherCalls_UnverifiedCallRemains_Throws() {
    using MockContext context = new();

    Mock.When(() => StaticCalculator.Add(Arg.Any<int>(), Arg.Any<int>())).ThenReturn(0);
    StaticCalculator.Add(1, 1);
    StaticCalculator.Add(2, 2);

    Mock.Verify(() => StaticCalculator.Add(1, 1), Times.Once());
    VerificationException thrown = Assert.Throws<VerificationException>(Mock.VerifyNoOtherCalls);

    Assert.Contains("Add(2, 2)", thrown.Message);
  }

  [Fact]
  public void VerifySet_Failure_UsesPropertyAssignmentSyntax() {
    using MockContext context = new();
    StaticCalculator.Value = 0;

    Mock.WhenSet(() => StaticCalculator.Value, () => Arg.Any<int>()).ThenCallOriginal();
    StaticCalculator.Value = 3;

    VerificationException thrown = Assert.Throws<VerificationException>(
      () => Mock.VerifySet(() => StaticCalculator.Value, () => 4));

    // Property accessors render as assignments, not compiler names like set_Value.
    Assert.Contains("StaticCalculator.Value = 4", thrown.Message);
    Assert.Contains("StaticCalculator.Value = 3", thrown.Message);
    Assert.DoesNotContain("set_Value", thrown.Message);
  }

  [Fact]
  public void VerifySet_CountsMatchingWrites() {
    using MockContext context = new();
    StaticCalculator.Value = 0;

    Mock.WhenSet(() => StaticCalculator.Value, () => Arg.Any<int>()).ThenCallOriginal();
    StaticCalculator.Value = 7;
    StaticCalculator.Value = 8;

    Mock.VerifySet(() => StaticCalculator.Value, () => 7);
    Mock.VerifySet(() => StaticCalculator.Value, () => Arg.Any<int>(), Times.Exactly(2));
    Mock.VerifySet(() => StaticCalculator.Value, () => 9, Times.Never());
    Assert.Equal(8, StaticCalculator.Value);
  }
}
