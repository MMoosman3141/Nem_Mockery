using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// The README's headline scenarios pinned against real framework types: BCL statics,
/// async methods, extension methods, and the documented generic-method boundary
/// (README "Usage" and "Limitations").
/// </summary>
public partial class MockTests {
  [Fact]
  public void When_BclTryParse_OutParameterStubbed() {
    using MockContext context = new();
    int ignored = 0;

    Mock.When(() => int.TryParse(Arg.Any<string>(), out ignored)).ThenAnswer(invocation => {
      invocation.SetArgument(1, 42);
      return true;
    });

    bool parsed = int.TryParse("not a number", out int number);

    Assert.True(parsed);
    Assert.Equal(42, number);
  }

  [Fact]
  public void When_DateTimeNow_ReturnsCannedTimeUntilDisposed() {
    DateTime canned = new(2001, 2, 3, 4, 5, 6);
    MockContext context = new();

    Mock.When(() => DateTime.Now).ThenReturn(canned);
    DateTime mocked = DateTime.Now;
    context.Dispose();

    Assert.Equal(canned, mocked);
    Assert.NotEqual(canned, DateTime.Now);
  }

  [Fact]
  public void When_FileReadAllText_ReturnsCannedWithoutTouchingDisk() {
    using MockContext context = new();

    Mock.When(() => File.ReadAllText(Arg.Any<string>())).ThenReturn("canned");

    Assert.Equal("canned", File.ReadAllText(@"Z:\does\not\exist.txt"));
  }

  [Fact]
  public void When_GuidNewGuid_ReturnsCannedValue() {
    using MockContext context = new();
    Guid canned = new("11111111-1111-1111-1111-111111111111");

    Mock.When(() => Guid.NewGuid()).ThenReturn(canned);

    Assert.Equal(canned, Guid.NewGuid());
  }

  [Fact]
  public async Task When_AsyncMethod_ReturnsCannedTask() {
    using MockContext context = new();

    Mock.When(() => AsyncFetcher.FetchAsync(Arg.Any<string>()))
      .ThenReturn(Task.FromResult("canned"));

    Assert.Equal("canned", await AsyncFetcher.FetchAsync("x"));
  }

  [Fact]
  public void When_ExtensionMethod_StubBindsToReceiverInstance() {
    using MockContext context = new();
    List<int> mocked = [1, 2, 3];
    List<int> real = [1, 2, 3, 4];

    Mock.When(() => mocked.Summarize()).ThenReturn("stubbed");

    Assert.Equal("stubbed", mocked.Summarize());
    Assert.Equal("real:4", real.Summarize());
  }

  [Fact]
  public void When_GenericMethod_ThrowsDescriptiveMockeryException() {
    using MockContext context = new();

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.When(() => GenericEcho.Echo(5)));

    Assert.Contains("generic", thrown.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void When_MethodOnGenericType_ThrowsDescriptiveMockeryException() {
    using MockContext context = new();
    List<string> list = ["a"];

    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.When(() => list.Contains("a")));

    Assert.Contains("generic", thrown.Message, StringComparison.OrdinalIgnoreCase);
  }
}
