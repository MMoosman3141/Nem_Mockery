using Nem_Mockery;
using xUnit_Nem_Mockery.TestSupport;

namespace xUnit_Nem_Mockery;

/// <summary>
/// MockContext lifecycle: the ambient-context requirement, full revert on dispose,
/// nested contexts, and the per-method ownership gate that serializes parallel
/// tests (README "Isolation and parallelism").
/// </summary>
public class MockContextTests {
  [Fact]
  public void When_NoActiveContext_ThrowsMockeryException() {
    MockeryException thrown = Assert.Throws<MockeryException>(
      () => Mock.When(() => StaticCalculator.Add(1, 1)));

    Assert.Contains("MockContext", thrown.Message);
  }

  [Fact]
  public void Dispose_RestoresRealBehavior() {
    MockContext context = new();
    Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(100);
    Assert.Equal(100, StaticCalculator.Add(1, 2));

    context.Dispose();

    Assert.True(context.IsDisposed);
    Assert.Equal(3, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void Dispose_CalledTwice_IsSafe() {
    MockContext context = new();
    Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(100);

    context.Dispose();
    context.Dispose();

    Assert.Equal(3, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void NestedContexts_InnerStubWinsThenRevertsToOuter() {
    using MockContext outer = new();
    Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(10);

    using (MockContext inner = new()) {
      Mock.When(() => StaticCalculator.Add(1, 2)).ThenReturn(20);
      Assert.Equal(20, StaticCalculator.Add(1, 2));
    }

    Assert.Equal(10, StaticCalculator.Add(1, 2));
  }

  [Fact]
  public void When_OnDisposedContext_ThrowsMockeryException() {
    MockContext context = new();
    context.Dispose();

    Assert.Throws<MockeryException>(() => Mock.When(() => StaticCalculator.Add(1, 1)));
  }

  [Fact]
  public void Claim_UnrelatedContexts_SerializeOnTheSameMethod() {
    List<string> events = [];
    Lock eventsLock = new();
    ManualResetEventSlim firstHasStub = new(false);
    ManualResetEventSlim releaseFirst = new(false);
    int firstObserved = 0;
    int secondObserved = 0;

    Thread first = new(() => {
      using MockContext context = new();
      Mock.When(() => StaticCalculator.Add(2, 2)).ThenReturn(111);
      firstHasStub.Set();
      releaseFirst.Wait();
      firstObserved = StaticCalculator.Add(2, 2);
      lock (eventsLock) {
        events.Add("first-disposing");
      }
    });
    Thread second = new(() => {
      firstHasStub.Wait();
      using MockContext context = new();
      // This claim must block until the first context disposes.
      Mock.When(() => StaticCalculator.Add(2, 2)).ThenReturn(222);
      lock (eventsLock) {
        events.Add("second-claimed");
      }
      secondObserved = StaticCalculator.Add(2, 2);
    });

    first.Start();
    second.Start();
    // Give the second thread time to reach the gate, then let the first finish.
    Thread.Sleep(300);
    releaseFirst.Set();
    Assert.True(first.Join(TimeSpan.FromSeconds(30)));
    Assert.True(second.Join(TimeSpan.FromSeconds(30)));

    Assert.Equal(111, firstObserved);
    Assert.Equal(222, secondObserved);
    Assert.Equal(["first-disposing", "second-claimed"], events);
  }
}
